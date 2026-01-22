using UnityEngine;
using UnityEngine.InputSystem;
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

    // Internal State
    private int _currentIndex = 0;

    private void Awake()
    {
        if (Instance == null) { Instance = this; }
        else { Destroy(gameObject); }
    }

    private void Start()
    {
        // Start at index 0
        _currentIndex = 0;
        LoadEnemyAtIndex(0);

        if (continueInput != null) continueInput.action.Enable();
        if (retryInput != null) retryInput.action.Enable();
        if (restartInput != null) restartInput.action.Enable();
    }

    private void Update()
    {
        // 1. SAFETY CHECK
        // If Score Manager is NOT active/showing prompts, do nothing.
        // This prevents us from overriding the Fail Screen logic.
        if (scoreManager == null || !scoreManager.AreInputsActive) return;

        // 2. DETECT INPUTS
        if (CheckInput(continueInput))
        {
            OnContinuePressed();
        }
        else if (CheckInput(retryInput))
        {
            OnRetryPressed();
        }
        else if (CheckInput(restartInput))
        {
            OnFullRestartPressed();
        }
    }

    private bool CheckInput(InputActionReference refAction)
    {
        if (refAction != null && refAction.action.WasPressedThisFrame()) return true;
        return false;
    }

    // --- ACTIONS ---

    // 1. RETRY (Same Enemy)
    public void OnRetryPressed()
    {
        Debug.Log($"PROGRESSION: Retry Enemy Index {_currentIndex}");
        // We do NOT change the index.
        // We perform a Soft Reset (keep total score, just reset round).
        endManager.RestartGame(resetTotalScore: false);
    }

    // 2. CONTINUE (Next Enemy)
    public void OnContinuePressed()
    {
        _currentIndex++;

        if (_currentIndex >= enemyRoster.Count)
        {
            Debug.Log("Campaign Complete! Looping to start...");
            _currentIndex = 0;
            // Optional: You could load a "Credits" scene here instead.
        }

        Debug.Log($"PROGRESSION: Continue to Index {_currentIndex} ({enemyRoster[_currentIndex].name})");

        // 1. Swap the Enemy Data
        LoadEnemyAtIndex(_currentIndex);

        // 2. Soft Reset the Scene (keeps score, resets positions)
        endManager.RestartGame(resetTotalScore: false);
    }

    // 3. RESTART (Index 0)
    public void OnFullRestartPressed()
    {
        Debug.Log("PROGRESSION: Full Restart (Index 0)");
        _currentIndex = 0;

        LoadEnemyAtIndex(0);

        // Hard Reset (Wipe score history)
        endManager.RestartGame(resetTotalScore: true);
    }

    private void LoadEnemyAtIndex(int index)
    {
        if (enemyRoster == null || enemyRoster.Count == 0) return;
        if (index < 0 || index >= enemyRoster.Count) return;

        DuelEnemyProfile targetProfile = enemyRoster[index];
        if (enemyAI != null)
        {
            enemyAI.UpdateProfile(targetProfile);
        }
    }
}