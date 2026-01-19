using UnityEngine;

[CreateAssetMenu(fileName = "NewEnemyProfile", menuName = "Duel/Enemy Profile")]
public class DuelEnemyProfile : ScriptableObject
{
    [Header("Identité")]
    public string enemyName = "John Doe";

    [Header("Tension (Attente avant de bouger)")]
    [Tooltip("Temps minimum d'attente immobile (Idle)")]
    public float minWaitTime = 2.0f;
    [Tooltip("Temps maximum d'attente immobile")]
    public float maxWaitTime = 5.0f;

    [Header("Vitesse de Tir (Difficulté)")]
    [Tooltip("Le temps le plus rapide possible pour ce type d'ennemi (ex: 0.3s = Boss)")]
    public float fastestDrawSpeed = 0.4f;

    [Tooltip("Le temps le plus lent possible (ex: 1.0s = Facile)")]
    public float slowestDrawSpeed = 0.8f;

    [Header("Bonus (Optionnel)")]
    [Tooltip("Matériau spécifique pour cet ennemi (si vous voulez changer sa tenue)")]
    public Material skinMaterial;
}