using UnityEngine;
using DG.Tweening;

public enum KillCamMode
{
    Standard,
    SplitScreen,
    Animated
}

[CreateAssetMenu(fileName = "NewWorldCam", menuName = "Duel/Kill Cam Profile (World)")]
public class KillCamProfile : ScriptableObject
{
    [Header("--- Mode ---")]
    public KillCamMode camMode = KillCamMode.Standard;

    [Header("--- Main Camera (Start Position) ---")]
    [Tooltip("For 'Standard', this is the static position. For 'Animated', this is the START point.")]
    public Vector3 mainWorldPos;
    public Vector3 mainWorldRot;
    public float mainFOV = 60f;

    // --- NEW SECTION: ANIMATION DATA ---
    [Header("--- Animated Mode Settings (Destination) ---")]
    [Tooltip("The camera will move TO this position.")]
    public Vector3 mainDestPos;
    public Vector3 mainDestRot;
    public float mainDestFOV = 40f;

    [Space(10)]
    public float animDuration = 2.0f;

    [Tooltip("Define the motion curve here (e.g., Ease In Out)")]
    public AnimationCurve animCurve = AnimationCurve.EaseInOut(0, 0, 1, 1); // <--- NOW A CURVE
    // -----------------------------------

    [Header("--- Aux Cam A (Only used if SplitScreen) ---")]
    public Vector3 camA_WorldPos;
    public Vector3 camA_WorldRot;
    public float camA_FOV = 40f;

    [Header("--- Aux Cam B (Only used if SplitScreen) ---")]
    public Vector3 camB_WorldPos;
    public Vector3 camB_WorldRot;
    public float camB_FOV = 40f;
}