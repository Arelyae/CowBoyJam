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
    public List<DuelEnemyProfile> enemyRoster;

    [Header("--- UI References ---")]
    public TextMeshProUGUI enemyNameText;

    [Header("--- Animation & Audio ---")]
    public float nameTypingDuration = 0.8f;
    public EventReference typingSound;

    [Header("--- References ---")]
    public EnemyDuelAI enemyAI;
    public EndManager endManager;
    public ScoreManager scoreManager;
    public DuelAudioDirector audioDirector;
    public DuelCinematographer cinematographer; // <--- MAKE SURE THIS IS ASSIGNED IN INSPECTOR

    [Header("--- END GAME ---")]
    public FinalScoreManager finalScoreManager;

    [Header("--- Input Actions ---")]
    public InputActionReference continueInput;
    public InputActionReference retryInput;
    public InputActionReference restartInput;

    [Header("--- Transition Settings ---")]
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
        if (scoreManager == null || !scoreManager.AreInputsActive || IsTransitioning) return;

        if (CheckInput(continueInput)) StartCoroutine(SequenceContinue());
        else if (CheckInput(retryInput)) StartCoroutine(SequenceRetry());
        else if (CheckInput(restartInput)) StartCoroutine(SequenceRestart());
    }

    private bool CheckInput(InputActionReference refAction)
    {
        return (refAction != null && refAction.action.WasPressedThisFrame());
    }

    public void ManualFullReset()
    {
        StopAllCoroutines();
        IsTransitioning = false;
        Debug.Log("<color=red>[PROGRESSION] Manual Full Reset Triggered. Index reset to 0.</color>");

        _scoreHistory.Clear();
        if (audioDirector != null) audioDirector.ResetIntensity();
        _currentIndex = 0;
        LoadEnemyAtIndex(0);
    }

    IEnumerator SequenceRetry()
    {
        IsTransitioning = true;
        scoreManager.HighlightSelection(NavigationAction.Retry);
        yield return new WaitForSecondsRealtime(selectionDelay);

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

    IEnumerator SequenceContinue()
    {
        IsTransitioning = true;
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

        if (_currentIndex >= enemyRoster.Count)
        {
            if (endManager != null) endManager.DisableGameplayForFinale();
            if (endManager.gameplayHUDGroup) endManager.gameplayHUDGroup.alpha = 0f;
            if (scoreManager.scorePanel) scoreManager.scorePanel.SetActive(false);
            CalculateFinalAverage();
            IsTransitioning = false;
            yield break;
        }

        LoadEnemyAtIndex(_currentIndex);
        endManager.RestartGame(resetTotalScore: false);
        IsTransitioning = false;
    }

    IEnumerator SequenceRestart()
    {
        IsTransitioning = true;
        scoreManager.HighlightSelection(NavigationAction.Restart);
        yield return new WaitForSecondsRealtime(selectionDelay);
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
    }

    private void LoadEnemyAtIndex(int index)
    {
        if (enemyRoster == null || enemyRoster.Count == 0) return;
        if (index < 0 || index >= enemyRoster.Count) return;

        DuelEnemyProfile targetProfile = enemyRoster[index];

        if (enemyAI != null) enemyAI.UpdateProfile(targetProfile);

        // --- CINEMATICS UPDATE ---
        if (cinematographer != null)
        {
            // 1. Load the specific shots for this enemy
            cinematographer.LoadProfileCinematics(targetProfile);

            // 2. IMPORTANT: Actually start the sequence!
            // This shows the first camera immediately and starts any durations.
            cinematographer.StartCinematicSequence();
        }
        // -------------------------

        bool isLastEnemy = (index == enemyRoster.Count - 1);
        if (scoreManager != null)
        {
            scoreManager.UpdateNavigationLabel(isLastEnemy);
        }

        if (enemyNameText != null)
        {
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