using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class DuelController : MonoBehaviour
{
    // --- EVENTS ---
    public event Action OnDraw, OnLoad, OnFire, OnDryFire, OnFeint, OnFumble, OnDeath;

    [Header("--- Components ---")]
    public Animator animator;
    public DuelArbiter arbiter;
    public AIDeathHandler targetEnemy;
    public EndManager endManager;
    public ScoreManager scoreManager; // Reference for scoring

    [Header("--- VFX & Spawning ---")]
    public Transform firePoint;
    public GameObject muzzleFlashPrefab;
    public GameObject bulletPrefab;
    public float flashDuration = 0.05f;

    [Header("--- Training Mode ---")]
    public TutorialTarget practiceTarget; // Reference to the practice dummy

    [Header("--- Input System ---")]
    public InputActionReference aimAction, loadAction, fireAction, feintAction;

    [Header("--- Duel State ---")]
    public DuelState currentState = DuelState.Idle;

    // Internal Timers & Logic
    private float lastStateChangeTime;
    private bool hasFumbled = false;
    private bool currentShotIsHonorable = false;
    private GameObject lastFiredBullet;

    [Header("--- Difficulty ---")]
    public float minDrawDuration = 0.3f;
    public float minLoadDuration = 0.2f;
    public float maxCockedDuration = 1.0f;

    [Header("--- Settings ---")]
    public float feintCooldown = 0.5f;
    [Range(0.01f, 1f)] public float triggerThreshold = 0.5f;

    // Animation Hashes
    private int animID_IsAiming, animID_IsCocked, animID_Feint, animID_Fire, animID_Die;

    private void Awake()
    {
        animID_IsAiming = Animator.StringToHash("IsAiming");
        animID_IsCocked = Animator.StringToHash("IsCocked");
        animID_Feint = Animator.StringToHash("Feint");
        animID_Fire = Animator.StringToHash("Fire");
        animID_Die = Animator.StringToHash("Die");
    }

    private void OnEnable()
    {
        if (aimAction) aimAction.action.Enable();
        if (loadAction) loadAction.action.Enable();
        if (fireAction) fireAction.action.Enable();
        if (feintAction) feintAction.action.Enable();
    }

    private void OnDisable()
    {
        if (aimAction) aimAction.action.Disable();
        if (loadAction) loadAction.action.Disable();
        if (fireAction) fireAction.action.Disable();
        if (feintAction) feintAction.action.Disable();
    }

    void Update()
    {
        if (currentState == DuelState.Dead || currentState == DuelState.Fired || hasFumbled) return;

        UpdateAnimationStates();
        HandleInput();

        // Check for hesitation (holding the hammer too long)
        if (currentState == DuelState.Cocked)
        {
            float timeHeld = Time.time - lastStateChangeTime;
            if (timeHeld > maxCockedDuration) StartCoroutine(Fumble("Hesitated too long!"));
        }
    }

    void ChangeState(DuelState newState)
    {
        currentState = newState;
        lastStateChangeTime = Time.time;
    }

    void HandleInput()
    {
        float aimValue = aimAction.action.ReadValue<float>();
        bool inputAim = aimValue > triggerThreshold;

        // --- 1. AIM (DRAW) ---
        if (inputAim && currentState == DuelState.Idle)
        {
            ChangeState(DuelState.Drawing);
            OnDraw?.Invoke();

            // STOP REFLEX CLOCK HERE (Reaction Time)
            if (scoreManager != null) scoreManager.playerDrawTimestamp = Time.time;
        }
        else if (!inputAim && (currentState == DuelState.Drawing || currentState == DuelState.Cocked))
        {
            // Player let go of the controls -> Return to Idle
            ChangeState(DuelState.Idle);
        }

        // --- 2. LOAD (COCK HAMMER) ---
        if (loadAction.action.WasPressedThisFrame() && currentState == DuelState.Drawing)
        {
            if (Time.time - lastStateChangeTime < minDrawDuration)
            {
                StartCoroutine(Fumble("Jammed in holster! (Too fast)"));
                return;
            }
            ChangeState(DuelState.Cocked);
            OnLoad?.Invoke();
        }

        // --- 3. FIRE (SHOOT) ---
        if (fireAction.action.WasPressedThisFrame() && currentState == DuelState.Cocked)
        {
            if (Time.time - lastStateChangeTime < minLoadDuration)
            {
                StartCoroutine(Fumble("Misfire! (Mechanism jammed)"));
                return;
            }
            ProcessInputData();
        }
    }

    // --- PHASE 1: LOGIC & DECISION ---
    void ProcessInputData()
    {
        currentState = DuelState.Fired; // Block further inputs

        // Check Honor (Did the enemy move first?)
        currentShotIsHonorable = false;
        if (arbiter != null) currentShotIsHonorable = arbiter.enemyHasStartedAction;

        if (currentShotIsHonorable)
        {
            // SUCCESS
            OnFire?.Invoke();

            // STOP EXECUTION CLOCK HERE (Total Time)
            if (scoreManager != null) scoreManager.playerFireTimestamp = Time.time;
        }
        else
        {
            // FAIL (Dishonorable / Anticipation)
            OnDryFire?.Invoke();
            Debug.LogError("DEFEAT: Dishonorable Shot (Too early!)");

            if (endManager != null)
            {
                endManager.TriggerDefeat("Premature Shot! (Dishonor)");
            }
        }

        // Trigger visual animation (Hammer falls)
        animator.SetTrigger(animID_Fire);
    }

    // --- PHASE 2: VISUALS (Called by Animation Event "SpawnShotEffects") ---
    public void SpawnShotEffects()
    {
        // Safety check: If player died during the trigger pull, cancel the shot
        if (currentState == DuelState.Dead) return;
        if (!currentShotIsHonorable) return;

        if (firePoint != null)
        {
            // Muzzle Flash
            if (muzzleFlashPrefab != null)
            {
                GameObject flash = Instantiate(muzzleFlashPrefab, firePoint.position, firePoint.rotation, firePoint);
                Destroy(flash, flashDuration);
            }

            // Bullet Spawning
            // UPDATED: Allow firing if EITHER an Enemy OR a Practice Target exists
            if (bulletPrefab != null)
            {
                // We store the bullet reference to destroy it if we die while it travels
                lastFiredBullet = Instantiate(bulletPrefab, firePoint.position, firePoint.rotation);

                DuelBullet bulletScript = lastFiredBullet.GetComponent<DuelBullet>();
                if (bulletScript != null)
                {
                    if (targetEnemy != null)
                    {
                        // AI DUEL MODE
                        bulletScript.Initialize(targetEnemy.ragdollHeadRigidbody, targetEnemy, true);
                    }
                    else if (practiceTarget != null)
                    {
                        // TUTORIAL MODE
                        // Use the new InitializeTraining function you added to DuelBullet
                        bulletScript.InitializeTraining(practiceTarget.hitBox, practiceTarget);
                    }
                }
            }
        }
    }

    // --- STATES & UTILS ---

    IEnumerator Fumble(string reason)
    {
        hasFumbled = true;
        OnFumble?.Invoke();
        if (endManager != null) endManager.TriggerDefeat(reason);
        Die(); // Fail state acts like death
        yield return null;
    }

    public void Die()
    {
        if (currentState == DuelState.Dead) return;
        currentState = DuelState.Dead;

        // CLEANUP: Destroy bullet if it's currently flying
        if (lastFiredBullet != null)
        {
            Destroy(lastFiredBullet);
            lastFiredBullet = null;
        }

        StopAllCoroutines();
        animator.SetTrigger(animID_Die);
        OnDeath?.Invoke();

        // Only trigger defeat if it wasn't a self-fumble (EndManager handles victory/defeat logic)
        if (endManager != null && !hasFumbled) endManager.TriggerDefeat("You were shot dead.");
    }

    void UpdateAnimationStates()
    {
        bool isAiming = (currentState == DuelState.Drawing || currentState == DuelState.Cocked);
        animator.SetBool(animID_IsAiming, isAiming);

        bool isCocked = (currentState == DuelState.Cocked);
        animator.SetBool(animID_IsCocked, isCocked);
    }

    IEnumerator PerformFeint()
    {
        currentState = DuelState.Feinting;
        animator.SetTrigger(animID_Feint);
        OnFeint?.Invoke();
        yield return new WaitForSeconds(feintCooldown);
        if (currentState != DuelState.Dead) currentState = DuelState.Idle;
    }

    // --- SOFT RESET FUNCTION (Called by EndManager) ---
    public void ResetPlayer()
    {
        // 1. Reset State Logic
        currentState = DuelState.Idle;
        currentShotIsHonorable = false;
        hasFumbled = false;

        // 2. Cleanup flying objects
        if (lastFiredBullet != null) Destroy(lastFiredBullet);

        // 3. Reset Animation System
        animator.Rebind();
        animator.speed = 1f;

        // 4. Input Reset happens automatically via state check in Update
        // (currentState is now Idle, so inputs are valid again)
    }
}