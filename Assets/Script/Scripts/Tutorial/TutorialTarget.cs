using UnityEngine;
using DG.Tweening;
using System; // <--- Add this

public class TutorialTarget : MonoBehaviour
{
    // --- NEW EVENT ---
    public event Action OnHit;

    [Header("--- Physics Setup ---")]
    public Rigidbody hitBox;
    public float impactForce = 15f;
    public Vector3 uprightRotation = Vector3.zero;

    private bool _isHit = false;
    private Vector3 _startPosition;

    void Start()
    {
        if (hitBox == null) hitBox = GetComponent<Rigidbody>();
        _startPosition = transform.position;
        ResetTarget();
    }

    public void ReceiveHit(Vector3 bulletVelocity)
    {
        if (_isHit) return;
        _isHit = true;

        // --- NOTIFY MANAGER ---
        OnHit?.Invoke();

        // Physics Logic (Same as before)
        transform.DOKill();
        if (hitBox != null)
        {
            hitBox.isKinematic = false;
            hitBox.AddForce(bulletVelocity.normalized * impactForce, ForceMode.Impulse);
            hitBox.AddTorque(transform.right * impactForce * 0.5f, ForceMode.Impulse);
        }
    }

    public void ResetTarget()
    {
        _isHit = false;

        if (hitBox != null)
        {
            hitBox.isKinematic = true;
            hitBox.linearVelocity = Vector3.zero;
            hitBox.angularVelocity = Vector3.zero;
        }

        transform.position = _startPosition;
        transform.DOKill();
        transform.DOLocalRotate(uprightRotation, 0.5f).SetEase(Ease.OutBack);
    }
}