using UnityEngine;
using System.Collections.Generic;

public enum KillCamMode { Standard, SplitScreen, Animated, Splines }

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

    [Header("--- Orientation ---")]
    public bool lookAtPlayer = false;
    public bool lookAtEnemy = false;

    [Header("--- Attachment (GoPro Mode) ---")]
    [Tooltip("If TRUE, camera follows the target.")]
    public bool attachToTarget = false;

    [Header("--- Lock Rotation (Vomit Filter) ---")]
    [Tooltip("Keep horizon level (Prevent Roll).")]
    public bool lockRotationZ = true;
    [Tooltip("Prevent looking up/down with target (Prevent Pitch).")]
    public bool lockRotationX = true;
    [Tooltip("Prevent turning with target (Prevent Yaw).")]
    public bool lockRotationY = false;

    [Header("--- Lock Position (Stabilizer) ---")]
    [Tooltip("If TRUE, camera won't follow target movement on X axis.")]
    public bool lockPositionX = false;
    [Tooltip("If TRUE, camera won't follow target movement on Y axis (Height).")]
    public bool lockPositionY = false;
    [Tooltip("If TRUE, camera won't follow target movement on Z axis.")]
    public bool lockPositionZ = false;

    [Header("--- Main Camera (Start Position) ---")]
    public Vector3 mainWorldPos;
    public Vector3 mainWorldRot;
    public float mainFOV = 60f;

    [Header("--- Animated Mode ---")]
    public Vector3 mainDestPos;
    public Vector3 mainDestRot;
    public float mainDestFOV = 40f;
    public float animDuration = 2.0f;
    public AnimationCurve animCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("--- Spline Mode (Legacy) ---")]
    public List<SplinePoint> splinePath = new List<SplinePoint>();
    public float splineDuration = 4.0f;
    public AnimationCurve splineSpeedCurve = AnimationCurve.Linear(0, 0, 1, 1);
    public bool lookForward = true;

    [Header("--- Aux Cams ---")]
    public Vector3 camA_WorldPos; public Vector3 camA_WorldRot; public float camA_FOV = 40f;
    public Vector3 camB_WorldPos; public Vector3 camB_WorldRot; public float camB_FOV = 40f;
}