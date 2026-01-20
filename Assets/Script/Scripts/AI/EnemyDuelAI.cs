using System.Collections;
using UnityEngine;
using FMODUnity;

public class EnemyDuelAI : MonoBehaviour
{
    [Header("--- Configuration ---")]
    public DuelEnemyProfile difficultyProfile;

    [Header("--- Modules ---")]
    public AIDeathHandler deathHandler;

    [Header("--- References ---")]
    public DuelArbiter arbiter;
    public DuelController player;
    public ScoreManager scoreManager;
    public Animator aiAnimator;
    public Renderer aiRenderer;

    [Header("--- Target Reference (NEW) ---")]
    [Tooltip("Drag the Player's Camera or Head object here so the AI aims at the face.")]
    public Transform playerHeadTarget; // <--- ASSIGN THIS IN INSPECTOR (Main Camera)

    [Header("--- Combat VFX & Audio ---")]
    public Transform firePoint;
    public GameObject muzzleFlashPrefab;
    public GameObject bulletPrefab;
    public EventReference fireSound;

    private Vector3 startPosition;
    private Quaternion startRotation;
    private Coroutine duelRoutine;
    private bool isDead = false;
    private bool hasFired = false;

    void Start()
    {
        if (difficultyProfile == null)
        {
            Debug.LogError("ERROR: No Difficulty Profile assigned!");
            return;
        }

        if (aiRenderer != null && difficultyProfile.skinMaterial != null)
        {
            aiRenderer.material = difficultyProfile.skinMaterial;
        }

        if (aiAnimator) aiAnimator.speed = 1f;

        startPosition = transform.position;
        startRotation = transform.rotation;

        ResetEnemy();
    }

    public void RegisterDrawAction()
    {
        if (isDead) return;

        if (scoreManager != null) scoreManager.aiActionTimestamp = Time.time;
        if (arbiter != null) arbiter.enemyHasStartedAction = true;

        Debug.Log("AI: Movement detected (Reflex Clock Start).");
    }

    IEnumerator DuelRoutine()
    {
        float waitTime = Random.Range(difficultyProfile.minWaitTime, difficultyProfile.maxWaitTime);
        yield return new WaitForSeconds(waitTime);

        if (isDead) yield break;

        float chosenDuration = Random.Range(difficultyProfile.fastestDrawSpeed, difficultyProfile.slowestDrawSpeed);
        float animSpeedMultiplier = 1.0f / chosenDuration;

        if (aiAnimator)
        {
            aiAnimator.speed = animSpeedMultiplier;
            aiAnimator.SetTrigger("Fire");
        }

        yield return new WaitForSeconds(chosenDuration);

        if (isDead || !this.enabled) yield break;

        FireAtPlayer();
    }

    void FireAtPlayer()
    {
        if (isDead || hasFired) return;
        hasFired = true;

        // A. Visuals
        if (firePoint != null)
        {
            if (muzzleFlashPrefab)
            {
                GameObject flash = Instantiate(muzzleFlashPrefab, firePoint.position, firePoint.rotation, firePoint);
                Destroy(flash, 0.1f);
            }

            // --- BULLET SPAWNING (UPDATED) ---
            if (bulletPrefab)
            {
                GameObject bulletObj = Instantiate(bulletPrefab, firePoint.position, firePoint.rotation);

                EnemyBullet bulletScript = bulletObj.GetComponent<EnemyBullet>();

                // We pass the SPECIFIC 'playerHeadTarget' instead of the generic player transform
                if (bulletScript != null && player != null)
                {
                    // Fallback to player.transform if head is forgotten, but warn user
                    Transform target = playerHeadTarget != null ? playerHeadTarget : player.transform;

                    bulletScript.Initialize(target, player);
                }
            }
        }

        // B. Audio
        if (!fireSound.IsNull)
        {
            RuntimeManager.PlayOneShot(fireSound, transform.position);
        }
    }

    public void ResetEnemy()
    {
        StopAllCoroutines();

        transform.position = startPosition;
        transform.rotation = startRotation;

        isDead = false;
        hasFired = false;
        if (arbiter != null) arbiter.enemyHasStartedAction = false;

        if (aiAnimator)
        {
            aiAnimator.enabled = true;
            aiAnimator.Rebind();
            aiAnimator.speed = 1f;
            aiAnimator.Play("Idle");
        }

        if (deathHandler != null) deathHandler.ResetVisuals();

        duelRoutine = StartCoroutine(DuelRoutine());
    }

    public void StopCombat()
    {
        // 1. Stop the Timer/Logic
        StopAllCoroutines();

        // 2. Set "isDead" to true prevents any future actions
        // (Block 'FireAtPlayer', block 'RegisterDrawAction')
        // We use this flag so we don't need a new separate bool.
        isDead = true;

        // 3. Optional: Stop the gun sound if it was just about to play or is playing?
        // Usually handled by FMOD automatically or short one-shots, so ignored here.
    }
    public void NotifyDeath()
    {
        isDead = true;
        if (duelRoutine != null) StopCoroutine(duelRoutine);
        if (aiAnimator) aiAnimator.speed = 1f;
    }
}