using UnityEngine;
using UnityEngine.InputSystem;
using DG.Tweening;
using System.Collections;

public class EndManager : MonoBehaviour
{
    [Header("--- Input ---")]
    public InputActionReference reloadAction;

    [Header("--- UI Managers ---")]
    public ScoreManager scoreManager;
    public FailManager failManager;

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

    // --- NEW FLAG ---
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

        // FIXED LOGIC (Respects Progression Transitions)
        if (scoreManager != null && scoreManager.AreInputsActive) return;
        if (GameProgressionManager.Instance != null && GameProgressionManager.Instance.IsTransitioning) return;

        bool pressedRestart = false;
        if (reloadAction != null && reloadAction.action.WasPressedThisFrame()) pressedRestart = true;
        if (Input.GetKeyDown(KeyCode.R)) pressedRestart = true;
        if (Gamepad.current != null && (Gamepad.current.buttonNorth.wasPressedThisFrame || Gamepad.current.buttonSouth.wasPressedThisFrame)) pressedRestart = true;

        if (pressedRestart) HandleResetInput();
    }

    private void HandleResetInput()
    {
        if (failManager != null && failManager.IsAnimating) { failManager.SkipAnimation(); return; }
        if (scoreManager != null && scoreManager.IsAnimating) { scoreManager.SkipAnimation(); return; }
        RestartGame(resetTotalScore: false);
    }

    // --- VICTORY LOGIC ---
    public void TriggerVictory(string message)
    {
        if (gameIsOver) return;
        gameIsOver = true;
        PlayerWonThisRound = true; // Mark as Won

        if (gameplayHUDGroup) gameplayHUDGroup.DOFade(0f, 0.3f).SetUpdate(true);
        if (cameraDirector != null) cameraDirector.TriggerKillCam();
        if (failManager) failManager.Hide();
        if (scoreManager) scoreManager.DisplayScore();

        // --- AUDIO: Increase Intensity ---
        if (audioDirector != null && enemyAI != null && enemyAI.difficultyProfile != null)
        {
            float step = enemyAI.difficultyProfile.musicIntensityStep;
            audioDirector.IncreaseIntensity(step);
        }

        StartCoroutine(SlowMotionSequence());
    }

    // --- DEFEAT LOGIC ---
    public void TriggerDefeat(string rawReason)
    {
        if (gameIsOver) return;
        gameIsOver = true;
        PlayerWonThisRound = false; // Mark as Lost

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
        DOTween.To(() => Time.timeScale, x => Time.timeScale = x, targetTimeScale, slowMoDuration)
            .SetUpdate(true).SetEase(slowMoEase);
        Time.fixedDeltaTime = 0.02f * targetTimeScale;
    }

    // --- RESET LOGIC ---
    public void RestartGame(bool resetTotalScore = true)
    {
        Debug.Log($"--- RESETTING GAME (Wipe Score: {resetTotalScore}) ---");

        DOTween.KillAll();
        Time.timeScale = 1f;
        Time.fixedDeltaTime = 0.02f;
        gameIsOver = false;
        PlayerWonThisRound = false; // Reset flag

        if (gameplayHUDGroup) gameplayHUDGroup.alpha = 1f;

        if (failManager) failManager.Hide();
        if (scoreManager) scoreManager.ResetScore();

        if (tutorialManager != null)
        {
            tutorialManager.ForceReset();
        }
        else
        {
            if (cinematographer != null) cinematographer.StopCinematics();
            // Note: Audio Director is handled via GameProgressionManager now (or persisted)
            if (cameraDirector) cameraDirector.ResetCamera();
            if (playerController) playerController.ResetPlayer();
            if (enemyAI) enemyAI.ResetEnemy();
        }
    }
}