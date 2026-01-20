using UnityEngine;
using System.Collections.Generic;
using DG.Tweening;

public class CameraDirector : MonoBehaviour
{
    [Header("--- 1. The Real Cameras ---")]
    public Camera mainCamera;
    public Camera auxCamA;
    public Camera auxCamB;

    [Header("--- 2. UI ---")]
    public GameObject splitScreenCanvas;

    [Header("--- 3. Profiles ---")]
    public List<KillCamProfile> profiles;

    private Vector3 _startPos;
    private Quaternion _startRot;
    private float _startFOV;

    void Start()
    {
        if (mainCamera == null) mainCamera = Camera.main;
        _startPos = mainCamera.transform.position;
        _startRot = mainCamera.transform.rotation;
        _startFOV = mainCamera.fieldOfView;
        ResetCamera();
    }

    public void TriggerKillCam()
    {
        if (profiles.Count == 0) return;

        KillCamProfile p = profiles[Random.Range(0, profiles.Count)];
        ApplyProfile(p);
    }

    void ApplyProfile(KillCamProfile p)
    {
        // 1. MAIN CAMERA (Always applied)
        mainCamera.transform.position = p.mainWorldPos;
        mainCamera.transform.rotation = Quaternion.Euler(p.mainWorldRot);
        mainCamera.fieldOfView = p.mainFOV;

        // 2. CHECK THE ENUM
        switch (p.camMode)
        {
            case KillCamMode.SplitScreen:
                EnableSplitScreen(p);
                break;

            case KillCamMode.Standard:
            default:
                DisableSplitScreen(); // STRICTLY DISABLE UI AND AUX CAMS
                break;
        }
    }

    void EnableSplitScreen(KillCamProfile p)
    {
        // Activate UI
        if (splitScreenCanvas) splitScreenCanvas.SetActive(true);

        // Activate and Position Aux Cam A
        if (auxCamA)
        {
            auxCamA.gameObject.SetActive(true);
            auxCamA.fieldOfView = p.camA_FOV;
            auxCamA.transform.position = p.camA_WorldPos;
            auxCamA.transform.rotation = Quaternion.Euler(p.camA_WorldRot);
        }

        // Activate and Position Aux Cam B
        if (auxCamB)
        {
            auxCamB.gameObject.SetActive(true);
            auxCamB.fieldOfView = p.camB_FOV;
            auxCamB.transform.position = p.camB_WorldPos;
            auxCamB.transform.rotation = Quaternion.Euler(p.camB_WorldRot);
        }
    }

    void DisableSplitScreen()
    {
        // STRICTLY TURN OFF EVERYTHING RELATED TO SPLIT SCREEN
        if (splitScreenCanvas) splitScreenCanvas.SetActive(false);
        if (auxCamA) auxCamA.gameObject.SetActive(false);
        if (auxCamB) auxCamB.gameObject.SetActive(false);
    }

    public void ResetCamera()
    {
        DisableSplitScreen(); // Ensure clean slate

        mainCamera.transform.DOKill();
        mainCamera.transform.position = _startPos;
        mainCamera.transform.rotation = _startRot;
        mainCamera.fieldOfView = _startFOV;
    }

#if UNITY_EDITOR
    [Header("--- RECORDING TOOL ---")]
    public KillCamProfile profileToRecord;

    [ContextMenu("Record WORLD Positions")]
    public void RecordPositions()
    {
        if (profileToRecord == null) { Debug.LogError("Assign a Profile first!"); return; }

        if (mainCamera)
        {
            profileToRecord.mainWorldPos = mainCamera.transform.position;
            profileToRecord.mainWorldRot = mainCamera.transform.eulerAngles;
            profileToRecord.mainFOV = mainCamera.fieldOfView;
        }

        // Only record Aux cams if they are active/relevant, or just record them anyway
        if (auxCamA)
        {
            profileToRecord.camA_WorldPos = auxCamA.transform.position;
            profileToRecord.camA_WorldRot = auxCamA.transform.eulerAngles;
            profileToRecord.camA_FOV = auxCamA.fieldOfView;
        }

        if (auxCamB)
        {
            profileToRecord.camB_WorldPos = auxCamB.transform.position;
            profileToRecord.camB_WorldRot = auxCamB.transform.eulerAngles;
            profileToRecord.camB_FOV = auxCamB.fieldOfView;
        }

        Debug.Log($"SAVED WORLD POSITIONS to '{profileToRecord.name}'.");
        UnityEditor.EditorUtility.SetDirty(profileToRecord);
    }
#endif
}