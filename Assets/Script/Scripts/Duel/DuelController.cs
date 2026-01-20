using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class DuelController : MonoBehaviour
{
    // --- EVENTS ---
    public event Action OnDraw, OnLoad, OnFire, OnDryFire, OnFeint, OnFumble, OnDeath;

    [Header("--- Components ---")]
    public Animator animator;
    public DuelArbiter arbiter;
    public AIDeathHandler targetEnemy;
    public EndManager endManager;
    public ScoreManager scoreManager; // <--- AJOUT

    [Header("--- VFX & Spawning ---")]
    public Transform firePoint;
    public GameObject muzzleFlashPrefab;
    public GameObject bulletPrefab;
    public float flashDuration = 0.05f;

    [Header("--- Input System ---")]
    public InputActionReference aimAction, loadAction, fireAction, feintAction;

    [Header("--- Duel State ---")]
    public DuelState currentState = DuelState.Idle;

    // Internal Timers & Logic
    private float lastStateChangeTime;
    private float duelStartTime;
    private bool hasFumbled = false;
    private bool currentShotIsHonorable = false;

    [Header("--- Difficulty ---")]
    public float minDrawDuration = 0.3f;
    public float minLoadDuration = 0.2f;
    public float maxCockedDuration = 1.0f;

    [Header("--- Settings ---")]
    public float feintCooldown = 0.5f;
    [Range(0.01f, 1f)] public float triggerThreshold = 0.5f;

    private int animID_IsAiming, animID_IsCocked, animID_Feint, animID_Fire, animID_Die;
    private GameObject lastFiredBullet;
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

        // AIM
        if (inputAim && currentState == DuelState.Idle)
        {
            duelStartTime = Time.time;
            ChangeState(DuelState.Drawing);
            OnDraw?.Invoke();
            if (scoreManager != null) scoreManager.playerDrawTimestamp = Time.time;
        }
        else if (!inputAim && (currentState == DuelState.Drawing || currentState == DuelState.Cocked))
        {
            ChangeState(DuelState.Idle);
        }

        // LOAD
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

        // FIRE
        if (fireAction.action.WasPressedThisFrame() && currentState == DuelState.Cocked)
        {
            if (Time.time - lastStateChangeTime < minLoadDuration)
            {
                StartCoroutine(Fumble("Misfire! (Mechanism jammed)"));
                return;
            }
            ProcessInputData();
        }

        // FEINT
        if (feintAction.action.WasPressedThisFrame() && currentState == DuelState.Idle)
        {
            StartCoroutine(PerformFeint());
        }
    }

    // --- PHASE 1: LOGIC & DECISION ---
    void ProcessInputData()
    {
        currentState = DuelState.Fired; // Bloque les inputs

        // 1. Check Honor
        currentShotIsHonorable = false;
        if (arbiter != null) currentShotIsHonorable = arbiter.enemyHasStartedAction;

        // 2. Result
        if (currentShotIsHonorable)
        {
            // --- SUCCÈS ---
            OnFire?.Invoke(); // Son "BOUM"
        }
        else
        {
            // --- ÉCHEC (Tir anticipé) ---
            OnDryFire?.Invoke(); // Son "CLIC"

            Debug.LogError("DÉFAITE : Tir Déshonorant (Trop tôt !)");

            // STOP DU JEU IMMÉDIAT VIA LE MANAGER
            if (endManager != null)
            {
                endManager.TriggerDefeat("Tir Prématuré ! (Déshonneur)");
            }
        }

        // 3. Animation (Pour le visuel du chien qui tombe)
        animator.SetTrigger(animID_Fire);

        if (scoreManager != null) scoreManager.playerFireTimestamp = Time.time;

    }

    // --- PHASE 2: VISUALS (Called by Animation Event) ---
    // --- CORRECTION 1 : Annuler le tir si on est mort au moment du Bang ---
    public void SpawnShotEffects()
    {
        // 1. Check de Mort (Déjà présent)
        if (currentState == DuelState.Dead) return;
        if (!currentShotIsHonorable) return;

        if (firePoint != null)
        {
            // Flash
            if (muzzleFlashPrefab != null)
            {
                GameObject flash = Instantiate(muzzleFlashPrefab, firePoint.position, firePoint.rotation, firePoint);
                Destroy(flash, flashDuration);
            }

            // Balle
            if (bulletPrefab != null && targetEnemy != null)
            {
                // ON STOCKE LA RÉFÉRENCE DANS 'lastFiredBullet'
                lastFiredBullet = Instantiate(bulletPrefab, firePoint.position, firePoint.rotation);

                DuelBullet bulletScript = lastFiredBullet.GetComponent<DuelBullet>();
                if (bulletScript != null)
                {
                    bulletScript.Initialize(targetEnemy.ragdollHeadRigidbody, targetEnemy, true);
                }
            }
        }
    }

    IEnumerator Fumble(string reason)
    {
        hasFumbled = true;
        OnFumble?.Invoke();
        if (endManager != null) endManager.TriggerDefeat(reason);
        Die();
        yield return null;
    }

    public void Die()
    {
        if (currentState == DuelState.Dead) return;
        currentState = DuelState.Dead;

        // --- CORRECTION CRUCIALE : NETTOYAGE DE LA BALLE ---
        // Si une balle est en train de voler vers l'ennemi, on la détruit immédiatement.
        if (lastFiredBullet != null)
        {
            Destroy(lastFiredBullet);
            lastFiredBullet = null;
        }
        // ---------------------------------------------------

        StopAllCoroutines();
        animator.SetTrigger(animID_Die);
        OnDeath?.Invoke();

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

    public void ResetPlayer()
    {
        // 1. Reset État
        currentState = DuelState.Idle; // ou Holstered selon votre logique
        currentShotIsHonorable = false;
        hasFumbled = false;

        // 2. Nettoyage balle en vol
        if (lastFiredBullet != null) Destroy(lastFiredBullet);

        // 3. Reset Animation
        animator.Rebind();      // Reset propre de l'animator
        animator.speed = 1f;    // Reset vitesse
        // animator.SetTrigger("Idle"); // Si nécessaire

        // 4. Reset Input
        // (Assurez-vous que vos booléens d'input comme 'inputAim' sont reset si besoin)
    }

}