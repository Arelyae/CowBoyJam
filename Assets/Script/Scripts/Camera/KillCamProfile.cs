using UnityEngine;
using System.Collections.Generic;

public enum KillCamMode
{
    Standard,
    SplitScreen,
    Animated,
    Splines
}

// --- RESTAURATION DE LA CLASSE REQUISE ---
[System.Serializable]
public class SplinePoint
{
    public Vector3 pos;
    public Vector3 rot;
}

[CreateAssetMenu(fileName = "NewWorldCam", menuName = "Duel/Kill Cam Profile (World)")]
public class KillCamProfile : ScriptableObject
{
    [Header("--- Mode ---")]
    public KillCamMode camMode = KillCamMode.Standard;

    [Header("--- Orientation Overrides ---")]
    [Tooltip("Priority 1: Tracks the Player object.")]
    public bool lookAtPlayer = false;

    [Tooltip("Priority 1 (Override): Tracks the Enemy object.")]
    public bool lookAtEnemy = false;

    [Header("--- Main Camera (Start Position) ---")]
    public Vector3 mainWorldPos;
    public Vector3 mainWorldRot;
    public float mainFOV = 60f;

    [Header("--- Animated Mode (Linear Move) ---")]
    public Vector3 mainDestPos;
    public Vector3 mainDestRot;
    public float mainDestFOV = 40f;
    public float animDuration = 2.0f;
    public AnimationCurve animCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("--- Spline Mode (Legacy / ScriptableObject) ---")]
    // CETTE LISTE EST REQUISE pour que ApplyLegacySpline fonctionne
    public List<SplinePoint> splinePath = new List<SplinePoint>();

    public float splineDuration = 4.0f;
    public AnimationCurve splineSpeedCurve = AnimationCurve.Linear(0, 0, 1, 1);
    public bool lookForward = true;

    [Header("--- Aux Cams (SplitScreen) ---")]
    public Vector3 camA_WorldPos;
    public Vector3 camA_WorldRot;
    public float camA_FOV = 40f;
    public Vector3 camB_WorldPos;
    public Vector3 camB_WorldRot;
    public float camB_FOV = 40f;
}