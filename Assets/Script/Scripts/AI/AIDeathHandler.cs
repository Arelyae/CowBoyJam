using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class AIDeathHandler : MonoBehaviour
{
    [Header("--- Models ---")]
    public GameObject aliveModel;
    public GameObject ragdollModel;

    [Header("--- Ragdoll Physics ---")]
    public Rigidbody ragdollHeadRigidbody;
    public float headshotForce = 100f;
    public Vector3 impactModifier = new Vector3(0f, 0.5f, 0f);

    [Header("--- Configuration ---")]
    public float deathDelay = 0.05f;

    [Header("--- External Links ---")]
    public EnemyDuelAI combatScript;
    public EndManager endManager;

    // --- MEMOIRE (Pour le Reset) ---
    private Vector3 _startLocalPosAlive;
    private Quaternion _startLocalRotAlive;

    private Vector3 _startLocalPosRagdoll;
    private Quaternion _startLocalRotRagdoll;

    private Rigidbody[] _allRigidbodies;

    // Pour m�moriser la pose des os du ragdoll
    private struct BoneTransform
    {
        public Vector3 localPosition;
        public Quaternion localRotation;
    }
    private Dictionary<Transform, BoneTransform> _initialBoneTransforms = new Dictionary<Transform, BoneTransform>();

    void Awake()
    {
        // 1. SAUVEGARDE DES POSITIONS INITIALES (Le Vivant)
        if (aliveModel)
        {
            _startLocalPosAlive = aliveModel.transform.localPosition;
            _startLocalRotAlive = aliveModel.transform.localRotation;
            aliveModel.SetActive(true);
        }

        // 2. SAUVEGARDE DU RAGDOLL
        if (ragdollModel)
        {
            // On sauvegarde sa position locale par rapport au parent global
            _startLocalPosRagdoll = ragdollModel.transform.localPosition;
            _startLocalRotRagdoll = ragdollModel.transform.localRotation;

            _allRigidbodies = ragdollModel.GetComponentsInChildren<Rigidbody>();

            // On sauvegarde la pose de chaque os (T-Pose/Idle)
            foreach (Transform t in ragdollModel.GetComponentsInChildren<Transform>())
            {
                if (t != ragdollModel.transform)
                {
                    _initialBoneTransforms[t] = new BoneTransform
                    {
                        localPosition = t.localPosition,
                        localRotation = t.localRotation
                    };
                }
            }
            ragdollModel.SetActive(false);
        }
    }

    public void TriggerHeadshotDeath(Vector3 incomingDirection)
    {
        if (combatScript != null) combatScript.NotifyDeath();
        StartCoroutine(SwapModelsRoutine(incomingDirection));
    }

    IEnumerator SwapModelsRoutine(Vector3 dir)
    {
        if (deathDelay > 0f) yield return new WaitForSeconds(deathDelay);

        // A. On t�l�porte le Ragdoll sur le Vivant (World Space) pour une transition fluide
        if (aliveModel != null && ragdollModel != null)
        {
            ragdollModel.transform.position = aliveModel.transform.position;
            ragdollModel.transform.rotation = aliveModel.transform.rotation;
        }

        // B. SWAP
        if (aliveModel) aliveModel.SetActive(false);
        if (ragdollModel)
        {
            ragdollModel.SetActive(true);
            SetRagdollPhysics(true); // On active la physique
        }

        if (endManager != null) endManager.TriggerVictory("Enemy Down!");

        // C. IMPULSION
        if (ragdollHeadRigidbody != null)
        {
            dir.Normalize();
            Vector3 finalDirection = (dir + impactModifier).normalized;
            ragdollHeadRigidbody.AddForce(finalDirection * headshotForce, ForceMode.Impulse);
            ragdollHeadRigidbody.AddTorque(Random.insideUnitSphere * headshotForce * 0.5f, ForceMode.Impulse);
        }
    }

    // --- RESET VISUALS ---
    public void ResetVisuals()
    {
        StopAllCoroutines();

        // 1. RESET VIVANT
        if (aliveModel)
        {
            aliveModel.SetActive(true);
            // On remet � la position initiale m�moris�e (et pas Vector3.zero)
            aliveModel.transform.localPosition = _startLocalPosAlive;
            aliveModel.transform.localRotation = _startLocalRotAlive;
        }

        // 2. RESET RAGDOLL
        if (ragdollModel)
        {
            // A. Couper la physique d'abord
            SetRagdollPhysics(false);

            // B. Remettre chaque os � sa place (Anti-spaghetti)
            foreach (var kvp in _initialBoneTransforms)
            {
                if (kvp.Key != null)
                {
                    kvp.Key.localPosition = kvp.Value.localPosition;
                    kvp.Key.localRotation = kvp.Value.localRotation;
                }
            }

            // C. Remettre le Ragdoll entier � sa place initiale m�moris�e
            ragdollModel.transform.localPosition = _startLocalPosRagdoll;
            ragdollModel.transform.localRotation = _startLocalRotRagdoll;

            ragdollModel.SetActive(false);
        }
    }

    private void SetRagdollPhysics(bool state)
    {
        if (_allRigidbodies == null) return;

        foreach (Rigidbody rb in _allRigidbodies)
        {
            rb.isKinematic = !state;
            if (!state)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
        }
    }
}