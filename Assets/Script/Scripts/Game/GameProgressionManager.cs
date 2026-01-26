using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using FMODUnity;

[System.Serializable]
public struct DuelScoreData
{
    public float reflexTime;
    public float drawSpeed;
    public string enemyName;
}

public class GameProgressionManager : MonoBehaviour
{
    public static GameProgressionManager Instance;

    [Header("--- The Roster ---")]
    [Tooltip("List of Enemy Scriptable Objects in order of appearance.")]
    public List<DuelEnemyProfile> enemyRoster;

    [Header("--- UI References ---")]
    [Tooltip("The UI Text that displays the current enemy's name during gameplay.")]
    public TextMeshProUGUI enemyNameText;

    [Header("--- Animation & Audio ---")]
    public float nameTypingDuration = 0.8f;
    public EventReference typingSound;

    [Header("--- References ---")]
    public EnemyDuelAI enemyAI;
    public EndManager endManager;
    public ScoreManager scoreManager;
    public DuelAudioDirector audioDirector;

    [Header("--- END GAME ---")]
    public FinalScoreManager finalScoreManager;

    [Header("--- Input Actions ---")]
    public InputActionReference continueInput;
    public InputActionReference retryInput;
    public InputActionReference restartInput;

    [Header("--- Transition Settings ---")]
    [Tooltip("Time to wait after selecting an option before the game actually resets.")]
    public float selectionDelay = 0.6f;

    // --- SCORE TRACKING ---
    private List<DuelScoreData> _scoreHistory = new List<DuelScoreData>();

    private int _currentIndex = 0;
    private Coroutine _typingCoroutine;
    public bool IsTransitioning { get; private set; } = false;

    private void Awake()
    {
        if (Instance == null) { Instance = this; }
        else { Destroy(gameObject); }
    }

    private void Start()
    {
        Debug.Log("<color=green>[PROGRESSION] Game Started. Loading first enemy...</color>");
        _scoreHistory.Clear();
        LoadEnemyAtIndex(0);

        if (continueInput != null) continueInput.action.Enable();
        if (retryInput != null) retryInput.action.Enable();
        if (restartInput != null) restartInput.action.Enable();
    }

    private void OnDestroy()
    {
        if (continueInput != null) continueInput.action.Disable();
        if (retryInput != null) retryInput.action.Disable();
        if (restartInput != null) restartInput.action.Disable();
    }

    private void Update()
    {
        // Block inputs during transitions or if the Score Manager isn't ready
        if (scoreManager == null || !scoreManager.AreInputsActive || IsTransitioning) return;

        if (CheckInput(continueInput)) StartCoroutine(SequenceContinue());
        else if (CheckInput(retryInput)) StartCoroutine(SequenceRetry());
        else if (CheckInput(restartInput)) StartCoroutine(SequenceRestart());
    }

    private bool CheckInput(InputActionReference refAction)
    {
        return (refAction != null && refAction.action.WasPressedThisFrame());
    }

    // --- HELPER: RESET ROSTER (Called by EndManager for Full Restart) ---
    public void ManualFullReset()
    {
        // 1. STOP OLD ROUTINES FIRST (Fixes the "One Letter" bug)
        // We stop everything here so we don't kill the new typewriter coroutine we are about to start.
        StopAllCoroutines();
        IsTransitioning = false;

        Debug.Log("<color=red>[PROGRESSION] Manual Full Reset Triggered. Index reset to 0.</color>");

        // 2. Wipe History
        _scoreHistory.Clear();

        // 3. Reset Audio
        if (audioDirector != null) audioDirector.ResetIntensity();

        // 4. CRITICAL: Reset Index to 0
        _currentIndex = 0;

        // 5. Load First Enemy (Starts the NEW Typewriter coroutine safely)
        LoadEnemyAtIndex(0);
    }
    // --------------------------------------------------------------------

    // 1. RETRY
    IEnumerator SequenceRetry()
    {
        IsTransitioning = true;
        Debug.Log($"<color=cyan>[INPUT] Player selected: RETRY</color>");
        scoreManager.HighlightSelection(NavigationAction.Retry);
        yield return new WaitForSecondsRealtime(selectionDelay);

        // If player WON, decrease music intensity back to previous level
        if (endManager != null && endManager.PlayerWonThisRound)
        {
            if (audioDirector != null && enemyRoster.Count > _currentIndex)
            {
                audioDirector.DecreaseIntensity(enemyRoster[_currentIndex].musicIntensityStep);
            }
        }

        LoadEnemyAtIndex(_currentIndex);
        endManager.RestartGame(resetTotalScore: false);
        IsTransitioning = false;
    }

    // 2. CONTINUE
    IEnumerator SequenceContinue()
    {
        IsTransitioning = true;
        Debug.Log($"<color=cyan>[INPUT] Player selected: CONTINUE</color>");

        // Save Score for this round
        if (scoreManager != null)
        {
            DuelScoreData newData = new DuelScoreData
            {
                reflexTime = scoreManager.LastReflexTime,
                drawSpeed = scoreManager.LastDrawSpeed,
                enemyName = enemyRoster[_currentIndex].enemyName
            };
            _scoreHistory.Add(newData);
        }

        scoreManager.HighlightSelection(NavigationAction.Continue);
        yield return new WaitForSecondsRealtime(selectionDelay);

        _currentIndex++;

        // --- END GAME CHECK ---
        if (_currentIndex >= enemyRoster.Count)
        {
            Debug.Log("<color=orange>[PROGRESSION] All Enemies Defeated! Calculating Final Score...</color>");

            // 1. Completely lock gameplay inputs
            if (endManager != null) endManager.DisableGameplayForFinale();

            // 2. Hide existing HUDs
            if (endManager.gameplayHUDGroup) endManager.gameplayHUDGroup.alpha = 0f;
            if (scoreManager.scorePanel) scoreManager.scorePanel.SetActive(false);

            // 3. Trigger Final Score Sequence
            CalculateFinalAverage();

            // 4. Stop Coroutine (Do not restart game loop)
            IsTransitioning = false;
            yield break;
        }

        // Load Next Enemy
        LoadEnemyAtIndex(_currentIndex);
        endManager.RestartGame(resetTotalScore: false);
        IsTransitioning = false;
    }

    // 3. RESTART (Standard Input)
    IEnumerator SequenceRestart()
    {
        IsTransitioning = true;
        Debug.Log($"<color=red>[INPUT] Player selected: FULL RESTART</color>");
        scoreManager.HighlightSelection(NavigationAction.Restart);
        yield return new WaitForSecondsRealtime(selectionDelay);

        // DO NOT call ManualFullReset() here directly.
        // endManager.RestartGame(true) calls it internally.
        // Calling it twice would restart the typewriter then immediately kill it again.
        endManager.RestartGame(resetTotalScore: true);

        IsTransitioning = false;
    }

    private void CalculateFinalAverage()
    {
        if (_scoreHistory.Count == 0) return;

        float totalReflex = 0f;
        float totalDraw = 0f;
        int validReflexCount = 0;

        foreach (var data in _scoreHistory)
        {
            // Ignore Pre-Shots (0.0s) and Penalties (999s) for a fair average
            if (data.reflexTime > 0.001f && data.reflexTime < 900f)
            {
                totalReflex += data.reflexTime;
                validReflexCount++;
            }
            totalDraw += data.drawSpeed;
        }

        float avgReflex = validReflexCount > 0 ? totalReflex / validReflexCount : 0f;
        float avgDraw = totalDraw / _scoreHistory.Count;
        float finalScore = avgReflex + avgDraw;

        if (finalScoreManager != null)
        {
            finalScoreManager.TriggerEndingSequence(avgReflex, avgDraw, finalScore);
        }
        else
        {
            Debug.LogError("FinalScoreManager is not assigned in GameProgressionManager!");
        }
    }

    private void LoadEnemyAtIndex(int index)
    {
        if (enemyRoster == null || enemyRoster.Count == 0) return;
        if (index < 0 || index >= enemyRoster.Count) return;

        DuelEnemyProfile targetProfile = enemyRoster[index];

        if (enemyAI != null) enemyAI.UpdateProfile(targetProfile);

        // Update "Continue" Button Text (Last Enemy -> Final Score)
        bool isLastEnemy = (index == enemyRoster.Count - 1);
        if (scoreManager != null)
        {
            scoreManager.UpdateNavigationLabel(isLastEnemy);
        }

        // Animate Name Typing
        if (enemyNameText != null)
        {
            // Stop any existing typing routine before starting a new one
            if (_typingCoroutine != null) StopCoroutine(_typingCoroutine);
            _typingCoroutine = StartCoroutine(TypewriterRoutine(targetProfile.enemyName.ToUpper()));
        }
    }

    IEnumerator TypewriterRoutine(string finalName)
    {
        enemyNameText.text = "";
        if (string.IsNullOrEmpty(finalName)) yield break;

        float delayPerChar = nameTypingDuration / finalName.Length;

        for (int i = 0; i < finalName.Length; i++)
        {
            enemyNameText.text += finalName[i];
            PlayTypingSound();
            yield return new WaitForSecondsRealtime(delayPerChar);
        }
        _typingCoroutine = null;
    }

    private void PlayTypingSound()
    {
        if (!typingSound.IsNull) RuntimeManager.PlayOneShot(typingSound, Camera.main.transform.position);
    }
}