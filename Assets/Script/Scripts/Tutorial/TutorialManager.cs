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

    [Header("--- Progression Settings ---")]
    public int hitsToUnlock = 3;
    private int _currentHits = 0;
    private bool _canStartGame = false;

    [Header("--- Start Game UI ---")]
    public CanvasGroup startGamePrompt;

    [Header("--- Input ---")]
    public InputActionReference startGameAction;

    [Header("--- Reset Settings ---")]
    public float autoResetDelay = 1.5f;

    void Start()
    {
        if (arbiter != null) arbiter.enemyHasStartedAction = true;

        if (player != null)
        {
            player.OnFire += HandleShotFired;
            player.OnFumble += HandleShotFired;
        }

        if (target != null)
        {
            target.OnHit += HandleTargetHit;
        }

        if (startGameAction != null) startGameAction.action.Enable();

        if (startGamePrompt != null)
        {
            startGamePrompt.alpha = 0f;
            startGamePrompt.interactable = false;
        }
    }

    void OnDestroy()
    {
        if (player != null)
        {
            player.OnFire -= HandleShotFired;
            player.OnFumble -= HandleShotFired;
        }
        if (target != null)
        {
            target.OnHit -= HandleTargetHit;
        }
        if (startGameAction != null) startGameAction.action.Disable();
    }

    void Update()
    {
        // 1. SAFETY BLOCK: FAIL SCREEN
        if (failManager != null && failManager.IsActive)
        {
            // NEW: If we failed, force the prompt to hide immediately
            if (startGamePrompt != null && startGamePrompt.alpha > 0)
            {
                startGamePrompt.DOKill(); // Stop any fade-in tweens
                startGamePrompt.alpha = 0f;
            }
            return;
        }

        // 2. CHECK INPUT
        if (_canStartGame)
        {
            if (startGameAction != null && startGameAction.action.WasPressedThisFrame())
            {
                TriggerGameStart();
            }
            else if (Gamepad.current != null && Gamepad.current.buttonWest.wasPressedThisFrame)
            {
                TriggerGameStart();
            }
            else if (Input.GetKeyDown(KeyCode.F))
            {
                TriggerGameStart();
            }
        }
    }

    // --- PROGRESSION LOGIC ---
    void HandleTargetHit()
    {
        // Don't count hits if dead
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
            // Simple Fade In
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
        // Add Scene Load logic here
        yield return new WaitForSeconds(0.5f);
    }

    // --- RESET LOGIC ---
    void HandleShotFired()
    {
        StartCoroutine(ResetRoutine());
    }

    IEnumerator ResetRoutine()
    {
        yield return new WaitForSeconds(autoResetDelay);

        // Do not auto-reset if Fail Screen is open
        if (failManager != null && failManager.IsActive)
        {
            yield break;
        }

        ForceReset();
    }

    public void ForceReset()
    {
        StopAllCoroutines();
        if (target != null) target.ResetTarget();
        if (player != null) player.ResetPlayer();
        if (arbiter != null) arbiter.enemyHasStartedAction = true;

        // NEW: Restore the prompt if it was previously unlocked
        // (This happens when pressing 'R' to restart)
        if (_canStartGame && startGamePrompt != null)
        {
            startGamePrompt.alpha = 1f;
        }
    }
}