using System.Collections;
using UnityEngine;

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


    private Vector3 startPosition;
    private Quaternion startRotation;

    private Coroutine duelRoutine;
    private bool isDead = false;

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

        duelRoutine = StartCoroutine(DuelRoutine());
    }

    // --- NOUVELLE FONCTION ---
    // Cette fonction est appelée par le script "EnemyAnimationRelay" 
    // au moment exact où l'Event d'Animation se déclenche.
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
            aiAnimator.SetTrigger("Fire");
        }

        // --- MODIFICATION ---
        // Nous ne définissons PLUS le temps (aiActionTimestamp) ici.
        // Nous attendons que l'animation appelle RegisterDrawAction().
        // --------------------

        Debug.Log($"IA ({difficultyProfile.enemyName}) : Lance l'anim (Durée prévue: {chosenDuration:F3}s)");

        // 4. FENÊTRE DE MORT (Attente que l'animation finisse le tir)
        yield return new WaitForSeconds(chosenDuration);

        // --- SÉCURITÉ ---
        // Si l'IA est morte pendant qu'elle dégainait, elle ne tire pas
        if (isDead || !this.enabled) yield break;

        // 5. PUNITION (Si le joueur n'a pas tiré avant)
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
        if (arbiter != null) arbiter.enemyHasStartedAction = false;

        // Reset Animator
        if (aiAnimator)
        {
            aiAnimator.enabled = true;
            aiAnimator.Rebind();
            aiAnimator.speed = 1f;
            aiAnimator.Play("Idle");
        }

        // --- APPEL DU RESET VISUEL (Ragdoll) ---
        if (deathHandler != null)
        {
            deathHandler.ResetVisuals();
        }
        // ---------------------------------------

        StartCoroutine(DuelRoutine());
    }

    public void NotifyDeath()
    {
        isDead = true;
        if (duelRoutine != null) StopCoroutine(duelRoutine);

        // On remet la vitesse normale pour l'animation de mort
        if (aiAnimator) aiAnimator.speed = 1f;
    }
}