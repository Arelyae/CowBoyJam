using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;
using System.Collections.Generic;

public class GameProgressionManager : MonoBehaviour
{
    public static GameProgressionManager Instance;

    [Header("--- The Roster ---")]
    public List<DuelEnemyProfile> enemyRoster;

    [Header("--- References ---")]
    public EnemyDuelAI enemyAI;
    public EndManager endManager;
    public ScoreManager scoreManager;

    [Header("--- Input Actions ---")]
    public InputActionReference continueInput;
    public InputActionReference retryInput;
    public InputActionReference restartInput;

    [Header("--- Transition Settings ---")]
    [Tooltip("Time to wait after selecting an option before the game actually resets.")]
    public float selectionDelay = 0.6f;

    private int _currentIndex = 0;
    private bool _isTransitioning = false;

    private void Awake()
    {
        if (Instance == null) { Instance = this; }
        else { Destroy(gameObject); }
    }

    private void Start()
    {
        // Debug Log for Initial Load
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
        if (scoreManager == null || !scoreManager.AreInputsActive || _isTransitioning) return;

        if (CheckInput(continueInput))
        {
            StartCoroutine(SequenceContinue());
        }
        else if (CheckInput(retryInput))
        {
            StartCoroutine(SequenceRetry());
        }
        else if (CheckInput(restartInput))
        {
            StartCoroutine(SequenceRestart());
        }
    }

    private bool CheckInput(InputActionReference refAction)
    {
        return (refAction != null && refAction.action.WasPressedThisFrame());
    }

    // --- COROUTINE SEQUENCES ---

    // 1. RETRY
    IEnumerator SequenceRetry()
    {
        _isTransitioning = true;

        // --- DEBUG LOG ---
        Debug.Log($"<color=cyan>[INPUT] Player selected: RETRY</color> (Replaying Enemy #{_currentIndex}: {enemyRoster[_currentIndex].name})");

        scoreManager.HighlightSelection(NavigationAction.Retry);
        yield return new WaitForSecondsRealtime(selectionDelay);

        endManager.RestartGame(resetTotalScore: false);
        _isTransitioning = false;
    }

    // 2. CONTINUE
    IEnumerator SequenceContinue()
    {
        _isTransitioning = true;

        // --- DEBUG LOG ---
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
        _isTransitioning = false;
    }

    // 3. RESTART
    IEnumerator SequenceRestart()
    {
        _isTransitioning = true;

        // --- DEBUG LOG ---
        Debug.Log($"<color=red>[INPUT] Player selected: FULL RESTART</color>");

        scoreManager.HighlightSelection(NavigationAction.Restart);
        yield return new WaitForSecondsRealtime(selectionDelay);

        _currentIndex = 0;
        LoadEnemyAtIndex(0);
        endManager.RestartGame(resetTotalScore: true);
        _isTransitioning = false;
    }

    private void LoadEnemyAtIndex(int index)
    {
        if (enemyRoster == null || enemyRoster.Count == 0) return;
        if (index < 0 || index >= enemyRoster.Count) return;

        DuelEnemyProfile targetProfile = enemyRoster[index];

        // --- DEBUG LOG ---
        Debug.Log($"<color=yellow>[PROGRESSION] Loading Enemy Index {index}: {targetProfile.name}</color>");

        if (enemyAI != null) enemyAI.UpdateProfile(targetProfile);
    }
}