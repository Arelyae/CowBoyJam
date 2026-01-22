using UnityEngine;
using UnityEngine.InputSystem;
using DG.Tweening;
using System.Collections;

public class TutorialManager : MonoBehaviour
{
    [Header("--- References ---")]
    public DuelController player;
    public DuelArbiter arbiter;
    public TutorialTarget target;
    public FailManager failManager;

    [Header("--- Title Screen Settings ---")]
    public CanvasGroup titleScreenUI; // Assign the Title Panel here
    public InputActionReference startTitleAction; // South Button (X/A)
    public float titleFadeDuration = 1.0f;
    public float delayBeforeTutorial = 0.5f;

    [Header("--- Progression Settings ---")]
    public int hitsToUnlock = 3;
    private int _currentHits = 0;
    private bool _canStartGame = false;
    private bool _isTitleScreen = true; // New Flag

    [Header("--- Start Game UI (End of Tutorial) ---")]
    public CanvasGroup startGamePrompt;
    public InputActionReference startGameAction; // West Button (Square/X)

    [Header("--- Reset Settings ---")]
    public float autoResetDelay = 1.5f;

    void Start()
    {
        // 1. SETUP TITLE STATE
        _isTitleScreen = true;

        if (titleScreenUI != null)
        {
            titleScreenUI.alpha = 1f;
            titleScreenUI.blocksRaycasts = true;
        }

        // 2. FREEZE PLAYER
        // Disabling the script prevents Update() from running, effectively freezing inputs.
        if (player != null) player.enabled = false;

        // 3. HIDE END PROMPT
        if (startGamePrompt != null)
        {
            startGamePrompt.alpha = 0f;
            startGamePrompt.interactable = false;
        }

        // Enable Inputs
        if (startTitleAction != null) startTitleAction.action.Enable();
        if (startGameAction != null) startGameAction.action.Enable();

        // Subscribe Events
        if (player != null)
        {
            player.OnFire += HandleShotFired;
            player.OnFumble += HandleShotFired;
        }
        if (target != null)
        {
            target.OnHit += HandleTargetHit;
        }
    }

    void OnDestroy()
    {
        if (player != null)
        {
            player.OnFire -= HandleShotFired;
            player.OnFumble -= HandleShotFired;
        }
        if (target != null) target.OnHit -= HandleTargetHit;
        if (startTitleAction != null) startTitleAction.action.Disable();
        if (startGameAction != null) startGameAction.action.Disable();
    }

    void Update()
    {
        // 1. FAIL SCREEN BLOCKER
        if (failManager != null && failManager.IsActive)
        {
            if (startGamePrompt != null) startGamePrompt.alpha = 0f;
            return;
        }

        // 2. TITLE SCREEN LOGIC (New)
        if (_isTitleScreen)
        {
            CheckTitleInput();
            return; // Stop here, don't run tutorial logic yet
        }

        // 3. TUTORIAL END LOGIC (Start Journey)
        if (_canStartGame)
        {
            CheckEndGameInput();
        }
    }

    // --- TITLE SCREEN INPUT ---
    void CheckTitleInput()
    {
        bool pressedStart = false;

        if (startTitleAction != null && startTitleAction.action.WasPressedThisFrame()) pressedStart = true;
        else if (Gamepad.current != null && Gamepad.current.buttonSouth.wasPressedThisFrame) pressedStart = true;
        else if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return)) pressedStart = true;

        if (pressedStart)
        {
            StartCoroutine(BeginTutorialSequence());
        }
    }

    IEnumerator BeginTutorialSequence()
    {
        _isTitleScreen = false; // Disable input checking immediately

        // 1. Fade Out Title
        if (titleScreenUI != null)
        {
            yield return titleScreenUI.DOFade(0f, titleFadeDuration).SetEase(Ease.InOutSine).WaitForCompletion();
            titleScreenUI.blocksRaycasts = false;
        }

        // 2. Small Wait
        yield return new WaitForSeconds(delayBeforeTutorial);

        // 3. Enable Game
        Debug.Log("TUTORIAL: Player Control Enabled");
        if (player != null) player.enabled = true; // Unfreeze controls
        if (arbiter != null) arbiter.enemyHasStartedAction = true; // Allow shooting

        // Note: Your TutorialUIManager handles showing the prompts automatically 
        // once the player script is enabled and State becomes Idle.
    }

    // --- TUTORIAL END INPUT ---
    void CheckEndGameInput()
    {
        bool pressedDepart = false;

        if (startGameAction != null && startGameAction.action.WasPressedThisFrame()) pressedDepart = true;
        else if (Gamepad.current != null && Gamepad.current.buttonWest.wasPressedThisFrame) pressedDepart = true;
        else if (Input.GetKeyDown(KeyCode.F)) pressedDepart = true;

        if (pressedDepart)
        {
            TriggerGameStart();
        }
    }

    // --- PROGRESSION LOGIC ---
    void HandleTargetHit()
    {
        if (_isTitleScreen) return; // Ignore hits if somehow triggered early
        if (failManager != null && failManager.IsActive) return;

        _currentHits++;

        if (_currentHits >= hitsToUnlock && !_canStartGame)
        {
            UnlockGameStart();
        }
    }

    void UnlockGameStart()
    {
        _canStartGame = true;
        if (startGamePrompt != null)
        {
            startGamePrompt.DOFade(1f, 1.0f).SetEase(Ease.OutSine);
        }
    }

    void TriggerGameStart()
    {
        _canStartGame = false;
        if (startGamePrompt != null) startGamePrompt.DOKill();

        Debug.Log("--- TRANSITION: STARTING MAIN GAME ---");
        StartCoroutine(TransitionSequence());
    }

    IEnumerator TransitionSequence()
    {
        // Trigger Scene Transition Singleton
        yield return new WaitForSeconds(0.1f);

        if (SceneTransitionManager.Instance != null)
        {
            SceneTransitionManager.Instance.LoadGameScene();
        }
        else
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene("DuelScene");
        }
    }

    // --- RESET LOGIC ---
    void HandleShotFired()
    {
        StartCoroutine(ResetRoutine());
    }

    IEnumerator ResetRoutine()
    {
        yield return new WaitForSeconds(autoResetDelay);

        if (failManager != null && failManager.IsActive) yield break;

        ForceReset();
    }

    public void ForceReset()
    {
        StopAllCoroutines();
        if (target != null) target.ResetTarget();
        if (player != null) player.ResetPlayer();
        if (arbiter != null) arbiter.enemyHasStartedAction = true;

        if (_canStartGame && startGamePrompt != null)
        {
            startGamePrompt.alpha = 1f;
        }
    }
}