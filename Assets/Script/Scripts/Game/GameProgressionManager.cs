using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using FMODUnity;

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

    [Header("--- Input Actions ---")]
    public InputActionReference continueInput;
    public InputActionReference retryInput;
    public InputActionReference restartInput;

    [Header("--- Transition Settings ---")]
    [Tooltip("Time to wait after selecting an option before the game actually resets.")]
    public float selectionDelay = 0.6f;

    private int _currentIndex = 0;
    private Coroutine _typingCoroutine; // Track the routine to stop it if needed
    public bool IsTransitioning { get; private set; } = false;

    private void Awake()
    {
        if (Instance == null) { Instance = this; }
        else { Destroy(gameObject); }
    }

    private void Start()
    {
        Debug.Log("<color=green>[PROGRESSION] Game Started. Loading first enemy...</color>");
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

    // --- COROUTINE SEQUENCES ---

    // 1. RETRY
    IEnumerator SequenceRetry()
    {
        IsTransitioning = true;

        Debug.Log($"<color=cyan>[INPUT] Player selected: RETRY</color>");
        scoreManager.HighlightSelection(NavigationAction.Retry);
        yield return new WaitForSecondsRealtime(selectionDelay);

        // Audio Logic: Decrease if we won previously
        if (endManager != null && endManager.PlayerWonThisRound)
        {
            if (audioDirector != null && enemyRoster.Count > _currentIndex)
            {
                float step = enemyRoster[_currentIndex].musicIntensityStep;
                audioDirector.DecreaseIntensity(step);
            }
        }

        // Reload the current enemy to trigger the Typewriter effect again
        LoadEnemyAtIndex(_currentIndex);

        endManager.RestartGame(resetTotalScore: false);
        IsTransitioning = false;
    }

    // 2. CONTINUE
    IEnumerator SequenceContinue()
    {
        IsTransitioning = true;

        Debug.Log($"<color=cyan>[INPUT] Player selected: CONTINUE</color>");
        scoreManager.HighlightSelection(NavigationAction.Continue);
        yield return new WaitForSecondsRealtime(selectionDelay);

        _currentIndex++;
        if (_currentIndex >= enemyRoster.Count)
        {
            Debug.Log("<color=orange>[PROGRESSION] Roster Complete! Looping back to start.</color>");
            _currentIndex = 0;
        }

        LoadEnemyAtIndex(_currentIndex);
        endManager.RestartGame(resetTotalScore: false);
        IsTransitioning = false;
    }

    // 3. RESTART (Full Reset)
    IEnumerator SequenceRestart()
    {
        IsTransitioning = true;

        Debug.Log($"<color=red>[INPUT] Player selected: FULL RESTART</color>");
        scoreManager.HighlightSelection(NavigationAction.Restart);
        yield return new WaitForSecondsRealtime(selectionDelay);

        if (audioDirector != null)
        {
            audioDirector.ResetIntensity();
        }

        _currentIndex = 0;
        LoadEnemyAtIndex(0);
        endManager.RestartGame(resetTotalScore: true);
        IsTransitioning = false;
    }

    private void LoadEnemyAtIndex(int index)
    {
        if (enemyRoster == null || enemyRoster.Count == 0) return;
        if (index < 0 || index >= enemyRoster.Count) return;

        DuelEnemyProfile targetProfile = enemyRoster[index];

        // 1. Update AI Stats
        if (enemyAI != null) enemyAI.UpdateProfile(targetProfile);

        // 2. Animate Name (Native Coroutine)
        if (enemyNameText != null)
        {
            // Stop any existing typing to prevent overlap/glitches
            if (_typingCoroutine != null) StopCoroutine(_typingCoroutine);

            _typingCoroutine = StartCoroutine(TypewriterRoutine(targetProfile.enemyName.ToUpper()));
        }
    }

    IEnumerator TypewriterRoutine(string finalName)
    {
        enemyNameText.text = ""; // Start empty

        // Safety check to avoid division by zero
        if (string.IsNullOrEmpty(finalName)) yield break;

        // Calculate time per character
        float delayPerChar = nameTypingDuration / finalName.Length;

        for (int i = 0; i < finalName.Length; i++)
        {
            enemyNameText.text += finalName[i];
            PlayTypingSound();

            // Wait unscaled so it works even if game is paused/slowed
            yield return new WaitForSecondsRealtime(delayPerChar);
        }

        _typingCoroutine = null;
    }

    private void PlayTypingSound()
    {
        if (!typingSound.IsNull)
        {
            RuntimeManager.PlayOneShot(typingSound, Camera.main.transform.position);
        }
    }
}