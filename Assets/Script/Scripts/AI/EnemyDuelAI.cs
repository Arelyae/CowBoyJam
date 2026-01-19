using UnityEngine;
using System.Collections;

public class EnemyDuelAI : MonoBehaviour
{
    [Header("--- Configuration ---")]
    [Tooltip("Glissez ici un profil (ScriptableObject) pour définir la difficulté")]
    public DuelEnemyProfile difficultyProfile; // <--- LE CERVEAU

    [Header("--- Références ---")]
    public DuelArbiter arbiter;
    public DuelController player;
    public Animator aiAnimator;
    public Renderer aiRenderer; // Pour changer le skin si besoin

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

        // Setup visuel (Optionnel : applique le skin du profil)
        if (aiRenderer != null && difficultyProfile.skinMaterial != null)
        {
            aiRenderer.material = difficultyProfile.skinMaterial;
        }

        if (aiAnimator) aiAnimator.speed = 1f;

        duelRoutine = StartCoroutine(DuelRoutine());
    }

    IEnumerator DuelRoutine()
    {
        // 1. PHASE DE TENSION
        // On pioche les valeurs dans le ScriptableObject
        float waitTime = Random.Range(difficultyProfile.minWaitTime, difficultyProfile.maxWaitTime);
        yield return new WaitForSeconds(waitTime);

        if (isDead) yield break;

        // 2. CALCUL DE LA DIFFICULTÉ
        // On pioche la vitesse de tir dans le profil
        float chosenDuration = Random.Range(difficultyProfile.fastestDrawSpeed, difficultyProfile.slowestDrawSpeed);

        // Formule : Animation de base (1s) / Durée voulue
        float animSpeedMultiplier = 1.0f / chosenDuration;

        // 3. ACTION
        if (aiAnimator)
        {
            aiAnimator.speed = animSpeedMultiplier;
            aiAnimator.SetTrigger("Fire");
        }

        if (arbiter != null)
        {
            arbiter.enemyHasStartedAction = true;
            Debug.Log($"IA ({difficultyProfile.enemyName}) : Dégaine en {chosenDuration:F3}s (Speed x{animSpeedMultiplier:F2})");
        }

        // 4. FENÊTRE DE MORT
        yield return new WaitForSeconds(chosenDuration);

        // --- SÉCURITÉ ---
        if (isDead || !this.enabled) yield break;

        // 5. PUNITION
        if (player != null)
        {
            Debug.Log($"IA : Pan ! ({difficultyProfile.enemyName} a gagné)");
            player.Die();
        }
    }

    public void NotifyDeath()
    {
        isDead = true;
        if (duelRoutine != null) StopCoroutine(duelRoutine);
    }
}