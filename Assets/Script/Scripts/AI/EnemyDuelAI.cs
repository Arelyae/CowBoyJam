using UnityEngine;
using System.Collections;

public class EnemyDuelAI : MonoBehaviour
{
    [Header("--- Références ---")]
    public DuelArbiter arbiter;
    public DuelController player;
    public Animator aiAnimator; // L'animator du modèle "Vivant"

    [Header("--- Tension (Attente avant dégainé) ---")]
    public float minWaitTime = 2.0f;
    public float maxWaitTime = 5.0f;

    [Header("--- Difficulté (Vitesse de tir) ---")]
    [Tooltip("Temps le plus rapide (ex: 0.4s = Très Difficile)")]
    public float fastestDrawSpeed = 0.4f;

    [Tooltip("Temps le plus lent (ex: 0.8s = Facile)")]
    public float slowestDrawSpeed = 0.8f;

    private Coroutine duelRoutine;
    private bool isDead = false;

    void Start()
    {
        // On s'assure que l'animator est à vitesse normale au début
        if (aiAnimator) aiAnimator.speed = 1f;

        duelRoutine = StartCoroutine(DuelRoutine());
    }

    IEnumerator DuelRoutine()
    {
        // 1. PHASE DE TENSION
        // L'IA attend, immobile (Idle)
        yield return new WaitForSeconds(Random.Range(minWaitTime, maxWaitTime));

        if (isDead) yield break;

        // 2. CALCUL DE LA DIFFICULTÉ
        // On choisit une durée aléatoire entre le "facile" et le "difficile"
        float chosenDuration = Random.Range(fastestDrawSpeed, slowestDrawSpeed);

        // On calcule le multiplicateur de vitesse
        // Exemple : 1s / 0.5s = Speed 2 (L'animation joue 2x plus vite)
        float animSpeedMultiplier = 1.0f / chosenDuration;

        // 3. DÉCLENCHEMENT DE L'ACTION
        if (aiAnimator)
        {
            aiAnimator.speed = animSpeedMultiplier; // On applique la vitesse
            aiAnimator.SetTrigger("Fire");          // On lance l'anim
        }

        // On prévient l'arbitre que le mouvement a commencé (Honneur)
        if (arbiter != null)
        {
            arbiter.enemyHasStartedAction = true;
            Debug.Log($"IA : Dégaine ! (Vitesse x{animSpeedMultiplier:F2} | Durée: {chosenDuration}s)");
        }

        // 4. LA FENÊTRE DE MORT
        // On attend exactement la durée de l'animation accélérée
        yield return new WaitForSeconds(chosenDuration);

        // --- SÉCURITÉ ---
        if (isDead || !this.enabled) yield break;

        // 5. PUNITION (L'animation est finie, le coup part)
        if (player != null)
        {
            Debug.Log("IA : Pan ! (Animation terminée)");
            player.Die();

            // Note : L'animation aura visuellement fini son geste de tir à ce moment précis.
        }
    }

    public void NotifyDeath()
    {
        isDead = true;
        if (duelRoutine != null) StopCoroutine(duelRoutine);
        Debug.Log("IA : Morte avant la fin de son animation.");
    }
}