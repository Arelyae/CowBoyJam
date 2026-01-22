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

    [Header("--- Target Reference ---")]
    [Tooltip("Drag the Player's Camera or Head object here so the AI aims at the face.")]
    public Transform playerHeadTarget;

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
        startPosition = transform.position;
        startRotation = transform.rotation;

        if (difficultyProfile == null)
        {
            Debug.LogError("ERROR: No Difficulty Profile assigned!");
            return;
        }

        // Initialize Appearance
        UpdateVisuals();

        ResetEnemy();
    }

    // --- NEW: CALLED BY PROGRESSION MANAGER ---
    public void UpdateProfile(DuelEnemyProfile newProfile)
    {
        if (newProfile == null) return;

        this.difficultyProfile = newProfile;

        // Update Skin immediately
        UpdateVisuals();

        Debug.Log($"ENEMY AI: Profile swapped to {newProfile.name}");
    }

    private void UpdateVisuals()
    {
        if (aiRenderer != null && difficultyProfile != null && difficultyProfile.skinMaterial != null)
        {
            aiRenderer.material = difficultyProfile.skinMaterial;
        }
    }
    // ------------------------------------------

    public void RegisterDrawAction()
    {
        if (isDead) return;

        if (scoreManager != null) scoreManager.aiActionTimestamp = Time.time;
        if (arbiter != null) arbiter.enemyHasStartedAction = true;

        Debug.Log("AI: Movement detected (Reflex Clock Start).");
    }

    IEnumerator DuelRoutine()
    {
        // Use stats from the CURRENT difficultyProfile
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

            if (bulletPrefab)
            {
                GameObject bulletObj = Instantiate(bulletPrefab, firePoint.position, firePoint.rotation);
                EnemyBullet bulletScript = bulletObj.GetComponent<EnemyBullet>();

                if (bulletScript != null && player != null)
                {
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

        // Restart routine with potentially new stats
        if (difficultyProfile != null)
        {
            duelRoutine = StartCoroutine(DuelRoutine());
        }
    }

    public void StopCombat()
    {
        StopAllCoroutines();
        isDead = true;
    }

    public void NotifyDeath()
    {
        isDead = true;
        if (duelRoutine != null) StopCoroutine(duelRoutine);
        if (aiAnimator) aiAnimator.speed = 1f;
    }
}