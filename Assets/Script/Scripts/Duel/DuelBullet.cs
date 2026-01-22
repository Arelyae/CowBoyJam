using UnityEngine;

public class DuelBullet : MonoBehaviour
{
    [Header("--- Paramètres ---")]
    public float speed = 80f;
    public float hitDistance = 0.2f;
    public float maxLifetime = 2.0f;

    private Rigidbody targetHead;
    private bool isLethal = false;
    private AIDeathHandler enemyScript;

    // NEW: Reference to the practice target
    private TutorialTarget practiceScript;

    // OVERLOAD: New Initialize function for Training Mode
    public void InitializeTraining(Rigidbody target, TutorialTarget trainingTarget)
    {
        targetHead = target;
        practiceScript = trainingTarget;
        isLethal = true; // Training shots are always treated as "Lethal" intent
        Destroy(gameObject, maxLifetime);
    }

    // Existing Initialize (Keep this for the AI)
    public void Initialize(Rigidbody target, AIDeathHandler enemy, bool lethal)
    {
        targetHead = target;
        enemyScript = enemy;
        isLethal = lethal;
        if (!isLethal || targetHead == null) Destroy(gameObject, maxLifetime);
    }

    void Update()
    {
        // 1. STANDARD HOMING LOGIC (Same as before)
        if (isLethal && targetHead != null)
        {
            Vector3 targetPos = targetHead.position;
            transform.position = Vector3.MoveTowards(transform.position, targetPos, speed * Time.deltaTime);
            transform.LookAt(targetPos);

            if (Vector3.Distance(transform.position, targetPos) < hitDistance)
            {
                HitTarget();
            }
        }
        else
        {
            transform.Translate(Vector3.forward * speed * Time.deltaTime);
        }
    }

    void HitTarget()
    {
        // CASE A: Hit an Enemy AI
        if (enemyScript != null)
        {
            Vector3 impactDir = transform.forward;
            enemyScript.TriggerHeadshotDeath(impactDir);
        }
        // CASE B: Hit a Practice Target (NEW)
        // In DuelBullet.cs -> HitTarget()
        else if (practiceScript != null)
        {
            // Ensure we pass the direction the bullet was traveling
            practiceScript.ReceiveHit(transform.forward);
        }

        Destroy(gameObject);
    }
}