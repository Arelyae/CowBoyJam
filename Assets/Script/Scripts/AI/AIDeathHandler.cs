using UnityEngine;
using System.Collections;

public class AIDeathHandler : MonoBehaviour
{
    [Header("--- Mannequins ---")]
    [Tooltip("Le modèle vivant (avec Animator). Sera désactivé à la mort.")]
    public GameObject aliveModel;

    [Tooltip("Le modèle Ragdoll (avec Rigidbodies). Sera activé à la mort.")]
    public GameObject ragdollModel;

    [Header("--- Physique du Ragdoll ---")]
    [Tooltip("L'os de la tête SUR LE RAGDOLL (pour appliquer la force)")]
    public Rigidbody ragdollHeadRigidbody;

    public float headshotForce = 100f;
    public Vector3 impactModifier = new Vector3(0f, 0.5f, 0f);

    [Header("--- Configuration ---")]
    public float deathDelay = 0.05f;

    [Header("--- Liens Externes ---")]
    public EnemyDuelAI combatScript;
    public EndManager endManager;

    void Awake()
    {
        // SETUP INITIAL :
        // On s'assure que le vivant est là et le mort est caché
        if (aliveModel) aliveModel.SetActive(true);
        if (ragdollModel) ragdollModel.SetActive(false);
    }

    public void TriggerHeadshotDeath(Vector3 incomingDirection)
    {
        // 1. On coupe l'IA de combat (Cerveau)
        if (combatScript != null) combatScript.NotifyDeath();

        // 2. On lance la transition
        StartCoroutine(SwapModelsRoutine(incomingDirection));
    }

    IEnumerator SwapModelsRoutine(Vector3 dir)
    {
        // Petit délai pour l'impact (Hit Frame)
        if (deathDelay > 0f) yield return new WaitForSeconds(deathDelay);

        // --- LE SWAP ---

        // 1. On positionne le Ragdoll exactement là où est le vivant
        if (aliveModel != null && ragdollModel != null)
        {
            ragdollModel.transform.position = aliveModel.transform.position;
            ragdollModel.transform.rotation = aliveModel.transform.rotation;
        }

        // 2. On désactive le vivant
        if (aliveModel) aliveModel.SetActive(false);

        // 3. On active le mort
        if (ragdollModel) ragdollModel.SetActive(true);

        // ---------------

        // 4. Victoire !
        if (endManager != null) endManager.TriggerVictory("Ennemi abattu !");

        // 5. Application de la force sur la tête du Ragdoll
        if (ragdollHeadRigidbody != null)
        {
            dir.Normalize();
            Vector3 finalDirection = (dir + impactModifier).normalized;

            // On s'assure que le RB n'est pas kinematic (sécurité)
            ragdollHeadRigidbody.isKinematic = false;

            ragdollHeadRigidbody.AddForce(finalDirection * headshotForce, ForceMode.Impulse);
            ragdollHeadRigidbody.AddTorque(Random.insideUnitSphere * headshotForce * 0.5f, ForceMode.Impulse);
        }
    }
}