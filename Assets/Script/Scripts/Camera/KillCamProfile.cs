using UnityEngine;

// 1. Define the Enum
public enum KillCamMode
{
    Standard,       
    SplitScreen   
}

[CreateAssetMenu(fileName = "NewWorldCam", menuName = "Duel/Kill Cam Profile (World)")]
public class KillCamProfile : ScriptableObject
{
    [Header("--- Mode ---")]
    public KillCamMode camMode = KillCamMode.Standard; // <--- THE ENUM

    [Header("--- Main Camera (Absolute World Position) ---")]
    public Vector3 mainWorldPos;
    public Vector3 mainWorldRot;
    public float mainFOV = 60f;

    [Header("--- Aux Cam A (Only used if SplitScreen) ---")]
    public Vector3 camA_WorldPos;
    public Vector3 camA_WorldRot;
    public float camA_FOV = 40f;

    [Header("--- Aux Cam B (Only used if SplitScreen) ---")]
    public Vector3 camB_WorldPos;
    public Vector3 camB_WorldRot;
    public float camB_FOV = 40f;
}