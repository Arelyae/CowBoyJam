using UnityEngine;

public class AutoRotate : MonoBehaviour
{
    [Header("Paramètres")]
    [Tooltip("Vitesse de rotation en degrés par seconde")]
    public float rotationSpeed = 30f;

    void Update()
    {
        // On tourne autour de l'axe Y (Haut)
        // (0, 1, 0) correspond à l'axe Y
        transform.Rotate(0, rotationSpeed * Time.deltaTime, 0);
    }
}