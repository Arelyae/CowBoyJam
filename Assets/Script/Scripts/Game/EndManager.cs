using UnityEngine;
using UnityEngine.InputSystem;
using DG.Tweening;
using System.Collections;
using Unity.Cinemachine;
using UnityEngine.SceneManagement;

public class EndManager : MonoBehaviour
{
    [Header("--- Input ---")]
    public InputActionReference reloadAction;

    [Header("--- UI Managers ---")]
    public ScoreManager scoreManager;
    public FailManager failManager;
    public FinalScoreManager finalScoreManager;

    [Header("--- NEW: Gameplay HUD ---")]
    public CanvasGroup gameplayHUDGroup;

    [Header("--- MODE: TUTORIAL ---")]
    public TutorialManager tutorialManager;

    [Header("--- MODE: DUEL ---")]
    public DuelCinematographer cinematographer;
    public DuelAudioDirector audioDirector;
    public EnemyDuelAI enemyAI;
    public CameraDirector cameraDirector;

    [Header("--- Shared References ---")]
    public DuelController playerController;

    [Header("--- Victory Settings ---")]
    public float delayBeforeSlowMo = 0.1f;
    public float targetTimeScale = 0.1f;
    public float slowMoDuration = 1.5f;
    public Ease slowMoEase = Ease.OutExpo;

    [Header("--- Defeat Settings ---")]
    public float defeatDelay = 0.8f;

    [Header("--- FAIL SCREEN STRINGS ---")]
    public string deathTitle = "YOU DIED";
    [TextArea] public string deathReason = "Shot through the heart.";
    public string dishonorTitle = "DISHONORABLE";
    [TextArea] public string dishonorReason = "You fired before the draw.";
    public string fumbleTitle = "FUMBLE";
    [TextArea] public string fumbleReasonOverride = "";

    // Internal State
    private bool gameIsOver = false;
    public bool PlayerWonThisRound { get; private set; } = false;

    private void OnEnable() { if (reloadAction != null) reloadAction.action.Enable(); }
    private void OnDisable() { if (reloadAction != null) reloadAction.action.Disable(); }

    void Start()
    {
        Time.timeScale = 1f;
        Time.fixedDeltaTime = 0.02f;
        gameIsOver = false;
        DOTween.KillAll();

        if (gameplayHUDGroup) gameplayHUDGroup.alpha = 1f;
    }

    void Update()
    {
        if (!gameIsOver) return;

        // 1. Block input if Score Manager is animating (Round End)
        if (scoreManager != null && scoreManager.AreInputsActive) return;
        if (GameProgressionManager.Instance != null && GameProgressionManager.Instance.IsTransitioning) return;

        // 2. NEW: Block input if Player is Typing in Leaderboard
        if (finalScoreManager != null &&
            finalScoreManager.leaderboardManager != null &&
            finalScoreManager.leaderboardManager.IsTyping)
        {
            return; // IGNORE ALL RESET INPUTS
        }

        bool pressedStandard = false;
        bool pressedWest = false;

        // 3. Check Standard Inputs (R, Y, A)
        if (reloadAction != null && reloadAction.action.WasPressedThisFrame()) pressedStandard = true;
        if (Input.GetKeyDown(KeyCode.R)) pressedStandard = true;

        if (Gamepad.current != null)
        {
            if (Gamepad.current.buttonNorth.wasPressedThisFrame) pressedStandard = true;
            if (Gamepad.current.buttonSouth.wasPressedThisFrame) pressedStandard = true;

            // 4. Check West Button Separately (X / Square)
            if (Gamepad.current.buttonWest.wasPressedThisFrame) pressedWest = true;
        }

        // Logic Branch
        if (pressedStandard)
        {
            // Standard reset (Retry / Skip Animation)
            HandleResetInput();
        }
        else if (pressedWest)
        {
            // Hard Reset Logic: Only allowed at Final Score
            if (finalScoreManager != null && finalScoreManager.finalScorePanel.activeSelf)
            {
                RestartGame(resetTotalScore: true, fullSceneReload: true);
            }
        }
    }

    public void DisableGameplayForFinale()
    {
        gameIsOver = true;
        if (playerController) playerController.enabled = false;
        if (enemyAI) enemyAI.StopCombat();
        if (cinematographer) cinematographer.StopCinematics();
        Debug.Log("<color=red>[END MANAGER] Gameplay Disabled for Finale.</color>");
    }

    private void HandleResetInput()
    {
        if (failManager != null && failManager.IsAnimating) { failManager.SkipAnimation(); return; }
        if (scoreManager != null && scoreManager.IsAnimating) { scoreManager.SkipAnimation(); return; }

        if (finalScoreManager != null && finalScoreManager.finalScorePanel.activeSelf)
        {
            if (finalScoreManager.IsAnimating)
            {
                finalScoreManager.SkipAnimation();
                return;
            }
            if (finalScoreManager.IsSequenceFinished)
            {
                // Soft Full Restart (Go to Enemy 0, but INSTANTLY)
                RestartGame(resetTotalScore: true, fullSceneReload: false);
                return;
            }
            return;
        }

        // Default Round Restart (Retry)
        RestartGame(resetTotalScore: false, fullSceneReload: false);
    }

    // --- VICTORY & DEFEAT ---
    public void TriggerVictory(string message)
    {
        if (gameIsOver) return;
        gameIsOver = true;
        PlayerWonThisRound = true;

        if (gameplayHUDGroup) gameplayHUDGroup.DOFade(0f, 0.3f).SetUpdate(true);
        if (cameraDirector != null) cameraDirector.TriggerKillCam();
        if (failManager) failManager.Hide();
        if (scoreManager) scoreManager.DisplayScore();

        // --- UPDATED AUDIO LOGIC ---
        if (audioDirector != null && enemyAI != null && enemyAI.difficultyProfile != null)
        {
            // 1. Play the Stinger
            audioDirector.PlayVictoryStinger(-1);

            // 2. AND Increase the Intensity (so the music gets more intense alongside the stinger)
            float step = enemyAI.difficultyProfile.musicIntensityStep;
            audioDirector.IncreaseIntensity(step);
        }
        // ---------------------------

        StartCoroutine(SlowMotionSequence());
    }

    public void TriggerDefeat(string rawReason)
    {
        if (gameIsOver) return;
        gameIsOver = true;
        PlayerWonThisRound = false;

        if (gameplayHUDGroup) gameplayHUDGroup.DOFade(0f, 0.3f).SetUpdate(true);
        if (enemyAI != null) enemyAI.StopCombat();
        if (scoreManager) scoreManager.ResetScore();

        DOTween.To(() => Time.timeScale, x => Time.timeScale = x, 0.2f, 0.2f)
            .SetDelay(defeatDelay).SetUpdate(true).SetEase(Ease.OutQuart)
            .OnStart(() => { Time.fixedDeltaTime = 0.02f * 0.2f; });

        if (failManager)
        {
            string finalTitle = deathTitle;
            string finalReason = deathReason;
            bool showRedOverlay = true;

            if (rawReason.Contains("Dishonor") || rawReason.Contains("Premature")) { finalTitle = dishonorTitle; finalReason = dishonorReason; showRedOverlay = false; }
            else if (rawReason.Contains("Jammed") || rawReason.Contains("Misfire")) { finalTitle = fumbleTitle; finalReason = !string.IsNullOrEmpty(fumbleReasonOverride) ? fumbleReasonOverride : rawReason; showRedOverlay = false; }

            failManager.TriggerFailSequence(finalTitle, finalReason, showRedOverlay);
        }
    }

    IEnumerator SlowMotionSequence()
    {
        if (delayBeforeSlowMo > 0) yield return new WaitForSeconds(delayBeforeSlowMo);
        DOTween.To(() => Time.timeScale, x => Time.timeScale = x, targetTimeScale, slowMoDuration).SetUpdate(true).SetEase(slowMoEase);
        Time.fixedDeltaTime = 0.02f * targetTimeScale;
    }

    // --- RESET LOGIC ---
    public void RestartGame(bool resetTotalScore = true, bool fullSceneReload = false)
    {
        Debug.Log($"--- RESETTING GAME (Score: {resetTotalScore} | Scene: {fullSceneReload}) ---");

        // 1. HARD RESET (SCENE RELOAD)
        if (fullSceneReload)
        {
            DOTween.KillAll();
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
            return;
        }

        // 2. SOFT RESET (INSTANT)
        DOTween.KillAll();
        Time.timeScale = 1f;
        Time.fixedDeltaTime = 0.02f;
        gameIsOver = false;
        PlayerWonThisRound = false;

        if (gameplayHUDGroup) gameplayHUDGroup.alpha = 1f;

        if (failManager) failManager.Hide();
        if (scoreManager) scoreManager.ResetScore();

        if (finalScoreManager)
        {
            if (finalScoreManager.finalScorePanel) finalScoreManager.finalScorePanel.SetActive(false);
            if (finalScoreManager.finalCamera)
            {
                if (finalScoreManager.finalCamera.TryGetComponent<CinemachineCamera>(out var vcam)) vcam.Priority = 0;
                finalScoreManager.finalCamera.gameObject.SetActive(false);
            }
        }

        // SYNC PROGRESSION
        if (resetTotalScore && GameProgressionManager.Instance != null)
        {
            GameProgressionManager.Instance.ManualFullReset();
        }

        // RESET GAMEPLAY
        if (tutorialManager != null)
        {
            tutorialManager.ForceReset();
        }
        else
        {
            if (cinematographer != null) cinematographer.StopCinematics();
            if (cameraDirector) cameraDirector.ResetCamera();
            if (playerController)
            {
                playerController.enabled = true;
                playerController.ResetPlayer();
            }
            if (enemyAI) enemyAI.ResetEnemy();
        }
    }
}