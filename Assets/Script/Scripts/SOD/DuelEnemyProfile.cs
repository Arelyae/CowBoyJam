using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "NewEnemyProfile", menuName = "Duel/Enemy Profile")]
public class DuelEnemyProfile : ScriptableObject
{
    [System.Serializable]
    public struct CinematicStep
    {
        [Tooltip("Name of the Camera GameObject in the scene.")]
        public string cameraName;

        [Tooltip("Time in seconds to hold this shot. Set to 0 to wait for an Audio Marker (NextShot_).")]
        public float duration;
    }

    [Header("Identité")]
    public string enemyName = "John Doe";

    [Header("Tension (Attente avant de bouger)")]
    [Tooltip("Temps minimum d'attente immobile (Idle)")]
    public float minWaitTime = 2.0f;
    [Tooltip("Temps maximum d'attente immobile")]
    public float maxWaitTime = 5.0f;

    [Header("Vitesse de Tir (Difficulté)")]
    public float fastestDrawSpeed = 0.4f;
    public float slowestDrawSpeed = 0.8f;

    [Header("Audio Atmosphere")]
    [Range(0f, 100f)]
    public float musicIntensityStep = 10f;

    [Header("Bonus (Optionnel)")]
    public Material skinMaterial;

    [Header("--- Cinematics ---")]
    [Tooltip("Define the sequence of shots. Use Duration > 0 for auto-switch, or 0 to wait for music markers.")]
    public List<CinematicStep> cinematicSequence;
}