using UnityEngine;

public class DuelBullet : MonoBehaviour
{
    [Header("--- Paramètres ---")]
    [Tooltip("Vitesse de la balle (ex: 50 pour voir le trait, 100 pour instantané)")]
    public float speed = 80f;

    [Tooltip("Distance minimum pour considérer que c'est touché")]
    public float hitDistance = 0.2f;

    [Tooltip("Durée de vie max si on rate (pour ne pas polluer la scène)")]
    public float maxLifetime = 2.0f;

    private Rigidbody targetHead;
    private bool isLethal = false;
    private AIDeathHandler enemyScript; // Référence pour tuer l'ennemi

    // Fonction d'initialisation appelée par le DuelController
    public void Initialize(Rigidbody target, AIDeathHandler enemy, bool lethal)
    {
        targetHead = target;
        enemyScript = enemy;
        isLethal = lethal;

        // Si c'est un tir raté (Déshonorant), on tire tout droit sans viser
        if (!isLethal || targetHead == null)
        {
            // Détruit la balle après X secondes pour nettoyer
            Destroy(gameObject, maxLifetime);
        }
    }

    void Update()
    {
        // 1. CAS DU TIR MORTEL (HONORABLE)
        if (isLethal && targetHead != null)
        {
            // La balle fonce vers la position actuelle de la tête
            Vector3 targetPos = targetHead.position;

            // Déplacement
            transform.position = Vector3.MoveTowards(transform.position, targetPos, speed * Time.deltaTime);

            // On oriente la balle vers la cible pour que le Trail Renderer suive bien
            transform.LookAt(targetPos);

            // Vérification de l'impact (Distance)
            if (Vector3.Distance(transform.position, targetPos) < hitDistance)
            {
                HitTarget();
            }
        }
        // 2. CAS DU TIR RATÉ (DÉSHONORANT)
        else
        {
            // On avance juste tout droit
            transform.Translate(Vector3.forward * speed * Time.deltaTime);
        }
    }

    void HitTarget()
    {
        if (enemyScript != null)
        {
            // On calcule la direction de l'impact (Vecteur Balle -> Tête)
            // C'est important pour que la tête parte en arrière dans le sens du tir
            Vector3 impactDir = transform.forward;

            // C'est LA BALLE qui déclenche la mort maintenant
            enemyScript.TriggerHeadshotDeath(impactDir);
        }

        // On détruit la balle (Visuellement, elle est "entrée" dans la tête)
        Destroy(gameObject);
    }
}