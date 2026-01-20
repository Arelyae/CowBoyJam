using UnityEngine;
using System.Collections.Generic;

public enum KillCamMode
{
    Standard,
    SplitScreen,
    Animated,
    Splines
}

// Helper class to store both data points
[System.Serializable]
public class SplinePoint
{
    public Vector3 pos;
    public Vector3 rot; // Stored as Euler angles
}

[CreateAssetMenu(fileName = "NewWorldCam", menuName = "Duel/Kill Cam Profile (World)")]
public class KillCamProfile : ScriptableObject
{
    [Header("--- Mode ---")]
    public KillCamMode camMode = KillCamMode.Standard;

    [Header("--- Main Camera (Start Position) ---")]
    public Vector3 mainWorldPos;
    public Vector3 mainWorldRot;
    public float mainFOV = 60f;

    [Header("--- Animated Mode (A to B) ---")]
    public Vector3 mainDestPos;
    public Vector3 mainDestRot;
    public float mainDestFOV = 40f;
    public float animDuration = 2.0f;
    public AnimationCurve animCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("--- Spline Mode (Path) ---")]
    [Tooltip("The path points. The camera will pass through these positions and rotations.")]
    public List<SplinePoint> splinePath = new List<SplinePoint>();

    public float splineDuration = 4.0f;

    [Tooltip("Control speed along the path.")]
    public AnimationCurve splineSpeedCurve = AnimationCurve.Linear(0, 0, 1, 1);

    [Tooltip("If TRUE: Camera ignores point rotations and looks forward along the path.\nIf FALSE: Camera smoothly interpolates through the rotations defined in the points.")]
    public bool lookForward = false;

    [Header("--- Aux Cams (SplitScreen) ---")]
    public Vector3 camA_WorldPos;
    public Vector3 camA_WorldRot;
    public float camA_FOV = 40f;
    public Vector3 camB_WorldPos;
    public Vector3 camB_WorldRot;
    public float camB_FOV = 40f;
}