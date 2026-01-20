using System.Collections;
using UnityEngine;
using FMODUnity; // Don't forget this for Audio!

public class EnemyDuelAI : MonoBehaviour
{
    [Header("--- Configuration ---")]
    [Tooltip("Glissez ici un profil (ScriptableObject) pour définir la difficulté")]
    public DuelEnemyProfile difficultyProfile;

    [Header("--- Modules ---")]
    public AIDeathHandler deathHandler;

    [Header("--- Références ---")]
    public DuelArbiter arbiter;
    public DuelController player;
    public ScoreManager scoreManager;
    public Animator aiAnimator;
    public Renderer aiRenderer;

    [Header("--- Combat VFX & Audio (New) ---")]
    public Transform firePoint;          // The tip of the gun
    public GameObject muzzleFlashPrefab;
    public GameObject bulletPrefab;      // Visual bullet (optional)
    public EventReference fireSound;     // FMOD Sound

    private Vector3 startPosition;
    private Quaternion startRotation;

    private Coroutine duelRoutine;
    private bool isDead = false;
    private bool hasFired = false; // Prevents double firing

    void Start()
    {
        // Sécurité : Vérifier si un profil est assigné
        if (difficultyProfile == null)
        {
            Debug.LogError("ERREUR : Pas de profil de difficulté assigné sur l'ennemi !");
            return;
        }

        // Setup visuel (Skin)
        if (aiRenderer != null && difficultyProfile.skinMaterial != null)
        {
            aiRenderer.material = difficultyProfile.skinMaterial;
        }

        if (aiAnimator) aiAnimator.speed = 1f;

        startPosition = transform.position;
        startRotation = transform.rotation;

        ResetEnemy(); // Use ResetEnemy to initialize properly
    }

    // --- CALLED BY ANIMATION EVENT (Via Relay) ---
    public void RegisterDrawAction()
    {
        if (isDead) return;

        // 1. On enregistre le temps exact pour le score de réflexe
        if (scoreManager != null)
        {
            scoreManager.aiActionTimestamp = Time.time;
        }

        // 2. On valide que l'ennemi a bougé (Le tir du joueur devient Honorable)
        if (arbiter != null)
        {
            arbiter.enemyHasStartedAction = true;
        }

        Debug.Log("IA : Mouvement détecté (Event Animation) ! Le chrono est lancé.");
    }

    IEnumerator DuelRoutine()
    {
        // 1. PHASE DE TENSION (Attente aléatoire)
        float waitTime = Random.Range(difficultyProfile.minWaitTime, difficultyProfile.maxWaitTime);
        yield return new WaitForSeconds(waitTime);

        if (isDead) yield break;

        // 2. CALCUL DE LA VITESSE
        float chosenDuration = Random.Range(difficultyProfile.fastestDrawSpeed, difficultyProfile.slowestDrawSpeed);
        // Formule : Animation de base (1s) / Durée voulue
        float animSpeedMultiplier = 1.0f / chosenDuration;

        // 3. LANCEMENT DE L'ANIMATION
        if (aiAnimator)
        {
            aiAnimator.speed = animSpeedMultiplier;
            aiAnimator.SetTrigger("Fire"); // This triggers the Draw+Shoot animation
        }

        Debug.Log($"IA ({difficultyProfile.enemyName}) : Lance l'anim (Durée prévue: {chosenDuration:F3}s)");

        // 4. FENÊTRE DE TIR
        // On attend exactement la durée calculée pour que le tir parte visuellement à la fin du geste
        yield return new WaitForSeconds(chosenDuration);

        // --- SÉCURITÉ ---
        if (isDead || !this.enabled) yield break;

        // 5. FEU ! (Si le joueur n'a pas tiré avant)
        FireAtPlayer();
    }

    // --- NEW FUNCTION: VISUALS + KILL ---
    void FireAtPlayer()
    {
        if (isDead || hasFired) return;
        hasFired = true;

        // A. Visuals
        if (firePoint != null)
        {
            // Flash
            if (muzzleFlashPrefab)
            {
                GameObject flash = Instantiate(muzzleFlashPrefab, firePoint.position, firePoint.rotation, firePoint);
                Destroy(flash, 0.1f);
            }
            // Bullet (Visual)
            if (bulletPrefab)
            {
                Instantiate(bulletPrefab, firePoint.position, firePoint.rotation);
            }
        }

        // B. Audio
        if (!fireSound.IsNull)
        {
            RuntimeManager.PlayOneShot(fireSound, transform.position);
        }

        // C. KILL PLAYER
        if (player != null)
        {
            Debug.Log($"IA : Pan ! ({difficultyProfile.enemyName} a gagné)");
            player.Die();
        }
    }

    public void ResetEnemy()
    {
        StopAllCoroutines();

        // Reset Position
        transform.position = startPosition;
        transform.rotation = startRotation;

        // Reset Logic
        isDead = false;
        hasFired = false;
        if (arbiter != null) arbiter.enemyHasStartedAction = false;

        // Reset Animator
        if (aiAnimator)
        {
            aiAnimator.enabled = true;
            aiAnimator.Rebind();
            aiAnimator.speed = 1f;
            aiAnimator.Play("Idle"); // Force Idle
        }

        // --- APPEL DU RESET VISUEL (Ragdoll) ---
        if (deathHandler != null)
        {
            deathHandler.ResetVisuals();
        }

        // Restart Loop
        duelRoutine = StartCoroutine(DuelRoutine());
    }

    public void NotifyDeath()
    {
        isDead = true;
        if (duelRoutine != null) StopCoroutine(duelRoutine);

        // On remet la vitesse normale pour l'animation de mort
        if (aiAnimator) aiAnimator.speed = 1f;
    }
}