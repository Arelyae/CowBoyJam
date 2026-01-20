using UnityEngine;

public class EnemyBullet : MonoBehaviour
{
    [Header("--- Settings ---")]
    [Tooltip("Bullet speed")]
    public float speed = 80f;

    [Tooltip("Minimum distance to register a hit")]
    public float hitDistance = 0.5f; // Slightly larger for the player to ensure it hits

    [Tooltip("Max lifetime if missed")]
    public float maxLifetime = 2.0f;

    private Transform targetTransform; // The Player (usually the Camera or Head)
    private DuelController playerScript; // Reference to kill the player

    // Initialization called by EnemyDuelAI
    public void Initialize(Transform target, DuelController playerCtrl)
    {
        targetTransform = target;
        playerScript = playerCtrl;

        // Safety: destroy after X seconds in case something goes wrong
        Destroy(gameObject, maxLifetime);
    }

    void Update()
    {
        // 1. HOMING BEHAVIOR (The AI doesn't miss if it shoots successfully)
        if (targetTransform != null)
        {
            // Move towards the player's head/camera
            Vector3 targetPos = targetTransform.position;

            // Move
            transform.position = Vector3.MoveTowards(transform.position, targetPos, speed * Time.deltaTime);

            // Rotate towards target (for Trail Renderer)
            transform.LookAt(targetPos);

            // Check Impact
            if (Vector3.Distance(transform.position, targetPos) < hitDistance)
            {
                HitPlayer();
            }
        }
        // 2. FALLBACK (Just go forward if target is lost)
        else
        {
            transform.Translate(Vector3.forward * speed * Time.deltaTime);
        }
    }

    void HitPlayer()
    {
        if (playerScript != null)
        {
            // Trigger Player Death
            playerScript.Die();
        }

        // Destroy the visual bullet
        Destroy(gameObject);
    }
}