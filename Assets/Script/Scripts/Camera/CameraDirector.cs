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

    // Internal State
    private Vector3 _startPos;
    private Quaternion _startRot;
    private float _startFOV;

    // MEMORY: To ensure we don't repeat the same cam twice
    private KillCamProfile _lastUsedProfile;

    void Start()
    {
        if (mainCamera == null) mainCamera = Camera.main;

        // Memorize initial "Game Play" position
        _startPos = mainCamera.transform.position;
        _startRot = mainCamera.transform.rotation;
        _startFOV = mainCamera.fieldOfView;

        ResetCamera();
    }

    public void TriggerKillCam()
    {
        if (profiles.Count == 0) return;

        Debug.Log("--- KILL CAM TRIGGERED ---");

        // 1. Create a temporary list of candidates
        List<KillCamProfile> candidates = new List<KillCamProfile>(profiles);

        // 2. FILTER: If we have more than 1 profile, remove the last one used.
        if (_lastUsedProfile != null && candidates.Count > 1)
        {
            candidates.Remove(_lastUsedProfile);
        }

        // 3. PICK RANDOM
        KillCamProfile chosenProfile = candidates[Random.Range(0, candidates.Count)];
        _lastUsedProfile = chosenProfile;

        // 4. Apply
        ApplyProfile(chosenProfile);
    }

    void ApplyProfile(KillCamProfile p)
    {
        // 1. Cleanup
        DisableSplitScreen();
        mainCamera.transform.DOKill();
        mainCamera.DOKill();

        // 2. SET STARTING POSITION (Instant Snap)
        mainCamera.transform.position = p.mainWorldPos;
        mainCamera.transform.rotation = Quaternion.Euler(p.mainWorldRot);
        mainCamera.fieldOfView = p.mainFOV;

        // 3. APPLY MODE SPECIFIC LOGIC
        switch (p.camMode)
        {
            case KillCamMode.Animated:
                ApplyAnimatedProfile(p);
                break;

            case KillCamMode.SplitScreen:
                EnableSplitScreen(p);
                break;

            case KillCamMode.Standard:
            default:
                break;
        }
    }

    // --- ANIMATED CAMERA LOGIC (UPDATED WITH CURVE) ---
    void ApplyAnimatedProfile(KillCamProfile p)
    {
        // We use SetEase(AnimationCurve) directly.

        // Move
        mainCamera.transform.DOMove(p.mainDestPos, p.animDuration)
            .SetEase(p.animCurve) // <--- USING CURVE
            .SetUpdate(true);

        // Rotate
        mainCamera.transform.DORotate(p.mainDestRot, p.animDuration)
            .SetEase(p.animCurve) // <--- USING CURVE
            .SetUpdate(true);

        // Zoom FOV
        mainCamera.DOFieldOfView(p.mainDestFOV, p.animDuration)
            .SetEase(p.animCurve) // <--- USING CURVE
            .SetUpdate(true);
    }
    // --------------------------------------------------

    void EnableSplitScreen(KillCamProfile p)
    {
        if (splitScreenCanvas) splitScreenCanvas.SetActive(true);

        if (auxCamA)
        {
            auxCamA.gameObject.SetActive(true);
            auxCamA.fieldOfView = p.camA_FOV;
            auxCamA.transform.position = p.camA_WorldPos;
            auxCamA.transform.rotation = Quaternion.Euler(p.camA_WorldRot);
        }

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
        if (splitScreenCanvas) splitScreenCanvas.SetActive(false);
        if (auxCamA) auxCamA.gameObject.SetActive(false);
        if (auxCamB) auxCamB.gameObject.SetActive(false);
    }

    public void ResetCamera()
    {
        DisableSplitScreen();

        mainCamera.transform.DOKill();
        if (mainCamera.GetComponent<Camera>()) mainCamera.GetComponent<Camera>().DOKill();

        mainCamera.transform.position = _startPos;
        mainCamera.transform.rotation = _startRot;
        mainCamera.fieldOfView = _startFOV;
    }

#if UNITY_EDITOR
    [Header("--- RECORDING TOOL ---")]
    public KillCamProfile profileToRecord;

    [ContextMenu("1. Record START Positions (Standard/Point A)")]
    public void RecordStartPositions()
    {
        if (profileToRecord == null) { Debug.LogError("Assign a Profile first!"); return; }

        if (mainCamera)
        {
            profileToRecord.mainWorldPos = mainCamera.transform.position;
            profileToRecord.mainWorldRot = mainCamera.transform.eulerAngles;
            profileToRecord.mainFOV = mainCamera.fieldOfView;
            Debug.Log($"Recorded START pos for '{profileToRecord.name}'");
        }

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

        UnityEditor.EditorUtility.SetDirty(profileToRecord);
    }

    [ContextMenu("2. Record DESTINATION Positions (Point B)")]
    public void RecordDestinationPositions()
    {
        if (profileToRecord == null) { Debug.LogError("Assign a Profile first!"); return; }

        if (mainCamera)
        {
            profileToRecord.mainDestPos = mainCamera.transform.position;
            profileToRecord.mainDestRot = mainCamera.transform.eulerAngles;
            profileToRecord.mainDestFOV = mainCamera.fieldOfView;
            Debug.Log($"Recorded DESTINATION pos for '{profileToRecord.name}'");
        }
        UnityEditor.EditorUtility.SetDirty(profileToRecord);
    }
#endif
}