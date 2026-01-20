using UnityEngine;
using UnityEngine.InputSystem;
using DG.Tweening;
using System.Collections;

public class EndManager : MonoBehaviour
{
    [Header("--- Input ---")]
    public InputActionReference reloadAction; // Kept your Input System reference

    [Header("--- UI Managers ---")]
    public ScoreManager scoreManager;
    public FailManager failManager;

    [Header("--- Gameplay References (Soft Reset) ---")]
    public DuelController playerController;
    public EnemyDuelAI enemyAI;
    public CameraDirector cameraDirector;

    [Header("--- Victory Settings (Slow Motion) ---")]
    public float delayBeforeSlowMo = 0.1f;
    public float targetTimeScale = 0.1f;
    public float slowMoDuration = 1.5f;
    public Ease slowMoEase = Ease.OutExpo;

    [Header("--- Defeat Settings ---")]
    [Tooltip("Wait time in seconds before freezing (Allows hearing the 'Click' or seeing the death)")]
    public float defeatDelay = 0.8f; // RESTORED: Vital for game feel

    [Header("--- FAIL SCREEN STRINGS ---")]
    [Header("1. Death")]
    public string deathTitle = "YOU DIED";
    [TextArea] public string deathReason = "Shot through the heart.";

    [Header("2. Dishonor (Premature Shot)")]
    public string dishonorTitle = "DISHONORABLE";
    [TextArea] public string dishonorReason = "You fired before the draw.";

    [Header("3. Fumble (Misfire/Jam)")]
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
        // Ensure clean start state
        Time.timeScale = 1f;
        Time.fixedDeltaTime = 0.02f;
        gameIsOver = false;
        DOTween.KillAll();
    }

    void Update()
    {
        if (!gameIsOver) return;

        // --- INPUT DETECTION (Merged Logic) ---
        bool pressedRestart = false;

        // 1. Check your Input System Action
        if (reloadAction != null && reloadAction.action.WasPressedThisFrame())
        {
            pressedRestart = true;
        }
        // 2. Hard check for Keyboard 'R'
        if (Input.GetKeyDown(KeyCode.R))
        {
            pressedRestart = true;
        }
        // 3. Hard check for Gamepad North (Triangle/Y) as requested
        if (Gamepad.current != null && Gamepad.current.buttonNorth.wasPressedThisFrame)
        {
            pressedRestart = true;
        }

        if (pressedRestart)
        {
            HandleResetInput();
        }
    }

    // --- NEW: INPUT HANDLING PRIORITY ---
    private void HandleResetInput()
    {
        // PRIORITY 1: Fail Screen Animation Skip
        if (failManager != null && failManager.gameObject.activeInHierarchy)
        {
            if (failManager.IsAnimating)
            {
                failManager.SkipAnimation();
                return;
            }
        }

        // PRIORITY 2: Victory Screen Animation Skip
        if (scoreManager != null && scoreManager.gameObject.activeInHierarchy)
        {
            if (scoreManager.IsAnimating)
            {
                scoreManager.SkipAnimation();
                return;
            }
        }

        // PRIORITY 3: Actual Restart
        RestartGame();
    }

    // --- VICTORY LOGIC (Restored your SlowMo Coroutine) ---
    public void TriggerVictory(string message)
    {
        if (gameIsOver) return;
        gameIsOver = true;

        if (cameraDirector != null) cameraDirector.TriggerKillCam();

        // Ensure Fail UI is hidden
        if (failManager) failManager.Hide();

        // Show Score
        if (scoreManager) scoreManager.DisplayScore();

        // Kill Cam

        // Start your original juicy slow motion
        StartCoroutine(SlowMotionSequence());
    }

    IEnumerator SlowMotionSequence()
    {
        if (delayBeforeSlowMo > 0) yield return new WaitForSeconds(delayBeforeSlowMo);

        // Tween TimeScale smoothly
        DOTween.To(() => Time.timeScale, x => Time.timeScale = x, targetTimeScale, slowMoDuration)
            .SetUpdate(true)
            .SetEase(slowMoEase);

        Time.fixedDeltaTime = 0.02f * targetTimeScale;
    }

    // --- DEFEAT LOGIC (Restored your Delay + Added New Fail Strings) ---
    public void TriggerDefeat(string rawReason)
    {
        if (gameIsOver) return;
        gameIsOver = true;

        // --- 1. SILENCE THE ENEMY ---
        // Immediately stop the AI from shooting if the player fumbled/died
        if (enemyAI != null)
        {
            enemyAI.StopCombat();
        }

        Debug.Log($"DEFEAT ({rawReason}) - Waiting {defeatDelay}s before freeze...");

        // --- 2. SLOW MOTION SEQUENCE ---
        // Wait for the delay (e.g. for the gunshot sound or death anim), then slow down time
        DOTween.To(() => Time.timeScale, x => Time.timeScale = x, 0.2f, 0.2f)
            .SetDelay(defeatDelay)
            .SetUpdate(true)
            .SetEase(Ease.OutQuart)
            .OnStart(() => { Time.fixedDeltaTime = 0.02f * 0.2f; });

        // --- 3. TRIGGER FAIL UI ---
        if (failManager)
        {
            string finalTitle = "";
            string finalReason = "";
            bool showRedOverlay = false;

            if (rawReason.Contains("Dishonor") || rawReason.Contains("Premature"))
            {
                // DISHONOR
                finalTitle = dishonorTitle;
                finalReason = dishonorReason;
                showRedOverlay = false;
            }
            else if (rawReason.Contains("Jammed") || rawReason.Contains("Misfire") || rawReason.Contains("Hesitated"))
            {
                // FUMBLE
                finalTitle = fumbleTitle;
                finalReason = !string.IsNullOrEmpty(fumbleReasonOverride) ? fumbleReasonOverride : rawReason;
                showRedOverlay = false;
            }
            else
            {
                // DEATH (Default)
                finalTitle = deathTitle;
                finalReason = deathReason;
                showRedOverlay = true; // Only show blood overlay on actual death
            }

            failManager.TriggerFailSequence(finalTitle, finalReason, showRedOverlay);
        }
    }

    // --- SOFT RESET ---
    public void RestartGame()
    {
        Debug.Log("--- SOFT RESET ---");

        // 1. Global Cleanup
        DOTween.KillAll();

        // 2. Reset Time
        Time.timeScale = 1f;
        Time.fixedDeltaTime = 0.02f;
        gameIsOver = false;

        // 3. Reset Individual Components
        if (failManager) failManager.Hide();
        if (scoreManager) scoreManager.ResetScore();

        if (cameraDirector) cameraDirector.ResetCamera();
        if (playerController) playerController.ResetPlayer();
        if (enemyAI) enemyAI.ResetEnemy();

    }
}