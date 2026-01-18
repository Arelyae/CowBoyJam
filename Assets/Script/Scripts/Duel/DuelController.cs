using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;
using System; // Required for Events

public class DuelController : MonoBehaviour
{
    // --- AUDIO EVENTS ---
    public event Action OnDraw;
    public event Action OnLoad;
    public event Action OnFire;
    public event Action OnFeint;
    public event Action OnFumble;
    public event Action OnDeath;

    [Header("--- Components ---")]
    public Animator animator;
    [Tooltip("The Arbiter judging the match (Honor check)")]
    public DuelArbiter arbiter;

    [Header("--- External Links (Game Loop) ---")]
    [Tooltip("The Enemy AI we are shooting at")]
    public AIDeathHandler targetEnemy;
    [Tooltip("The Manager handling Win/Loss logic")]
    public EndManager endManager;

    [Header("--- Input System ---")]
    public InputActionReference aimAction;   // Left Trigger
    public InputActionReference loadAction;  // Right Trigger
    public InputActionReference fireAction;  // Button South (A/X)
    public InputActionReference feintAction; // Button West (X/Square)

    [Header("--- Duel State ---")]
    public DuelState currentState = DuelState.Idle;

    // Internal Timers
    private float lastStateChangeTime; // Tracks time between steps
    private float duelStartTime;       // Tracks total reaction time
    private bool hasFumbled = false;   // Safety flag to stop inputs

    [Header("--- Difficulty (Speed Limits) ---")]
    [Tooltip("If you Load faster than this after Aiming -> FUMBLE")]
    public float minDrawDuration = 0.3f;
    [Tooltip("If you Fire faster than this after Loading -> FUMBLE")]
    public float minLoadDuration = 0.2f;

    [Header("--- Difficulty (Stress/Anti-Camping) ---")]
    [Tooltip("Maximum time you can hold the hammer cocked before panicking.")]
    public float maxCockedDuration = 1.0f;

    [Header("--- Settings ---")]
    public float feintCooldown = 0.5f;
    [Range(0.01f, 1f)] public float triggerThreshold = 0.5f;

    // Animation IDs (Optimization)
    private int animID_IsAiming;
    private int animID_IsCocked;
    private int animID_Feint;
    private int animID_Fire;
    private int animID_Die;

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
        // Stop all logic if the duel is over
        if (currentState == DuelState.Dead || currentState == DuelState.Fired || hasFumbled) return;

        UpdateAnimationStates();
        HandleInput();

        // --- STRESS MECHANIC (Anti-Camping) ---
        // If the player holds the hammer cocked too long, they panic.
        if (currentState == DuelState.Cocked)
        {
            float timeHeld = Time.time - lastStateChangeTime;

            if (timeHeld > maxCockedDuration)
            {
                StartCoroutine(Fumble("Hesitated too long! (Hand shaking)"));
            }
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

        // --- 1. AIM (Start) ---
        if (inputAim && currentState == DuelState.Idle)
        {
            duelStartTime = Time.time;
            ChangeState(DuelState.Drawing);
            OnDraw?.Invoke();
        }
        // --- 1. AIM (Cancel/Holster) ---
        else if (!inputAim && (currentState == DuelState.Drawing || currentState == DuelState.Cocked))
        {
            ChangeState(DuelState.Idle);
        }

        // --- 2. LOAD (Cock Hammer) ---
        if (loadAction.action.WasPressedThisFrame() && currentState == DuelState.Drawing)
        {
            // FUMBLE CHECK: Too Fast?
            if (Time.time - lastStateChangeTime < minDrawDuration)
            {
                StartCoroutine(Fumble("Jammed in holster! (Too fast)"));
                return;
            }
            ChangeState(DuelState.Cocked);
            OnLoad?.Invoke();
        }

        // --- 3. FIRE ---
        if (fireAction.action.WasPressedThisFrame() && currentState == DuelState.Cocked)
        {
            // FUMBLE CHECK: Too Fast?
            if (Time.time - lastStateChangeTime < minLoadDuration)
            {
                StartCoroutine(Fumble("Misfire! (Mechanism jammed)"));
                return;
            }
            Fire();
        }

        // --- 4. FEINT ---
        if (feintAction.action.WasPressedThisFrame() && currentState == DuelState.Idle)
        {
            StartCoroutine(PerformFeint());
        }
    }

    void Fire()
    {
        currentState = DuelState.Fired;
        animator.SetTrigger(animID_Fire);
        OnFire?.Invoke();

        float totalReactionTime = Time.time - duelStartTime;
        Debug.Log($"<color=green>SUCCESS: Shot fired in {totalReactionTime:F3} seconds!</color>");

        if (targetEnemy != null)
        {
            // CHECK HONOR: Did the enemy start their action?
            bool isHonorable = true;
            if (arbiter != null) isHonorable = arbiter.enemyHasStartedAction;

            if (isHonorable)
            {
                // 1. Calculate Bullet Trajectory
                Vector3 shootOrigin = transform.position + (Vector3.up * 1.5f);

                // FIXED: We now check 'ragdollHeadRigidbody' because we switched to the Model Swap script.
                // If the ragdoll is inactive, we fallback to the transform position approximation.
                Vector3 targetPosition = targetEnemy.ragdollHeadRigidbody != null
                    ? targetEnemy.ragdollHeadRigidbody.position
                    : targetEnemy.transform.position + Vector3.up * 1.6f;

                Vector3 shootDirection = targetPosition - shootOrigin;

                // 2. Kill the Enemy (Triggers Victory in EndManager via the Enemy Script)
                targetEnemy.TriggerHeadshotDeath(shootDirection);
            }
            else
            {
                // DISHONORABLE: Enemy ignores the shot.
                Debug.LogWarning("DISHONORABLE: You shot an unarmed man. He ignores it.");
            }
        }
    }

    // Punishment Logic
    IEnumerator Fumble(string reason)
    {
        hasFumbled = true;
        OnFumble?.Invoke(); // Play "Clunk" sound

        Debug.LogError($"FUMBLE: {reason}");

        // Trigger Defeat Screen
        if (endManager != null) endManager.TriggerDefeat(reason);

        // Die immediately
        Die();
        yield return null;
    }

    public void Die()
    {
        if (currentState == DuelState.Dead) return;

        currentState = DuelState.Dead;
        animator.SetTrigger(animID_Die);
        OnDeath?.Invoke();

        // Trigger standard defeat (if not already triggered by Fumble)
        if (endManager != null && !hasFumbled)
        {
            endManager.TriggerDefeat("You were shot dead.");
        }
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
}