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
    [Tooltip("Leave empty to use dynamic reason.")]
    [TextArea] public string fumbleReasonOverride = "";

    // Internal State
    private bool gameIsOver = false;

    private void OnEnable()
    {
        if (reloadAction != null) reloadAction.action.Enable();
    }

    private void OnDisable()
    {
        if (reloadAction != null) reloadAction.action.Disable();
    }

    void Start()
    {
        Time.timeScale = 1f;
        Time.fixedDeltaTime = 0.02f;
        gameIsOver = false;
        DOTween.KillAll();
    }

    void Update()
    {
        if (!gameIsOver) return;

        // We ALWAYS listen for input here to allow skipping animations.
        bool pressedRestart = false;

        if (reloadAction != null && reloadAction.action.WasPressedThisFrame()) pressedRestart = true;
        if (Input.GetKeyDown(KeyCode.R)) pressedRestart = true;
        if (Gamepad.current != null && Gamepad.current.buttonNorth.wasPressedThisFrame) pressedRestart = true;

        // Also check South Button (A/X) specifically for skipping score text, 
        // as players often mash 'A' to skip.
        if (Gamepad.current != null && Gamepad.current.buttonSouth.wasPressedThisFrame) pressedRestart = true;

        if (pressedRestart)
        {
            HandleResetInput();
        }
    }

    private void HandleResetInput()
    {
        // PRIORITY 1: Skip Fail Animation
        if (failManager != null && failManager.IsAnimating)
        {
            failManager.SkipAnimation();
            return; // Stop here
        }

        // PRIORITY 2: Skip Score/Victory Animation
        if (scoreManager != null && scoreManager.IsAnimating)
        {
            scoreManager.SkipAnimation();
            return; // Stop here
        }

        // PRIORITY 3: BLOCKER - Game Progression Logic
        // If the Score Screen is finished animating and waiting for input (Prompts are visible),
        // we DO NOT trigger a generic restart here. 
        // We yield control to the GameProgressionManager which listens for specific keys.
        if (scoreManager != null && scoreManager.AreInputsActive)
        {
            return;
        }

        // PRIORITY 4: Standard Restart (Fail Screen or Fallback)
        // Only runs if we are NOT on the Victory Input screen.
        RestartGame(resetTotalScore: false);
    }

    // --- VICTORY LOGIC ---
    public void TriggerVictory(string message)
    {
        if (gameIsOver) return;
        gameIsOver = true;

        if (cameraDirector != null) cameraDirector.TriggerKillCam();
        if (failManager) failManager.Hide();
        if (scoreManager) scoreManager.DisplayScore();

        StartCoroutine(SlowMotionSequence());
    }

    IEnumerator SlowMotionSequence()
    {
        if (delayBeforeSlowMo > 0) yield return new WaitForSeconds(delayBeforeSlowMo);

        DOTween.To(() => Time.timeScale, x => Time.timeScale = x, targetTimeScale, slowMoDuration)
            .SetUpdate(true)
            .SetEase(slowMoEase);

        Time.fixedDeltaTime = 0.02f * targetTimeScale;
    }

    // --- DEFEAT LOGIC ---
    public void TriggerDefeat(string rawReason)
    {
        if (gameIsOver) return;
        gameIsOver = true;

        if (enemyAI != null) enemyAI.StopCombat();

        if (scoreManager) scoreManager.ResetScore();

        DOTween.To(() => Time.timeScale, x => Time.timeScale = x, 0.2f, 0.2f)
            .SetDelay(defeatDelay)
            .SetUpdate(true)
            .SetEase(Ease.OutQuart)
            .OnStart(() => { Time.fixedDeltaTime = 0.02f * 0.2f; });

        if (failManager)
        {
            string finalTitle = deathTitle;
            string finalReason = deathReason;
            bool showRedOverlay = true;

            if (rawReason.Contains("Dishonor") || rawReason.Contains("Premature"))
            {
                finalTitle = dishonorTitle;
                finalReason = dishonorReason;
                showRedOverlay = false;
            }
            else if (rawReason.Contains("Jammed") || rawReason.Contains("Misfire") || rawReason.Contains("Hesitated"))
            {
                finalTitle = fumbleTitle;
                finalReason = !string.IsNullOrEmpty(fumbleReasonOverride) ? fumbleReasonOverride : rawReason;
                showRedOverlay = false;
            }

            failManager.TriggerFailSequence(finalTitle, finalReason, showRedOverlay);
        }
    }

    // --- RESET LOGIC ---
    public void RestartGame(bool resetTotalScore = true)
    {
        Debug.Log($"--- RESETTING GAME (Wipe Score: {resetTotalScore}) ---");

        DOTween.KillAll();
        Time.timeScale = 1f;
        Time.fixedDeltaTime = 0.02f;
        gameIsOver = false;

        if (failManager) failManager.Hide();

        if (scoreManager)
        {
            scoreManager.ResetScore();
            // Note: If you stored persistent data in ScoreManager, clear it here if resetTotalScore is true
        }

        if (tutorialManager != null)
        {
            tutorialManager.ForceReset();
        }
        else
        {
            if (cinematographer != null) cinematographer.StopCinematics();
            if (audioDirector != null)
            {
                audioDirector.StopMusic();
                audioDirector.StartMusic();
            }
            if (cameraDirector) cameraDirector.ResetCamera();
            if (playerController) playerController.ResetPlayer();
            if (enemyAI) enemyAI.ResetEnemy();
        }
    }
}