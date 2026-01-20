using UnityEngine;
using System.Collections.Generic;
using DG.Tweening;
using System.Linq; // Needed for Linq queries (Min, Where, ToList)

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

    // TRACKING: Maps Profile -> Times Used
    private Dictionary<KillCamProfile, int> _usageMap = new Dictionary<KillCamProfile, int>();

    void Start()
    {
        if (mainCamera == null) mainCamera = Camera.main;

        _startPos = mainCamera.transform.position;
        _startRot = mainCamera.transform.rotation;
        _startFOV = mainCamera.fieldOfView;

        // Initialize Usage Map
        foreach (var p in profiles)
        {
            if (!_usageMap.ContainsKey(p)) _usageMap.Add(p, 0);
        }

        ResetCamera();
    }

    public void TriggerKillCam()
    {
        if (profiles.Count == 0) return;

        Debug.Log("--- KILL CAM TRIGGERED ---");

        // 1. Ensure map is up to date (in case profiles were added dynamically)
        foreach (var p in profiles)
        {
            if (!_usageMap.ContainsKey(p)) _usageMap.Add(p, 0);
        }

        // 2. FIND LEAST USED COUNT
        int minUsage = int.MaxValue;
        foreach (var p in profiles)
        {
            if (_usageMap[p] < minUsage) minUsage = _usageMap[p];
        }

        // 3. GATHER CANDIDATES (All profiles that have the minimum usage)
        List<KillCamProfile> candidates = new List<KillCamProfile>();
        foreach (var p in profiles)
        {
            if (_usageMap[p] == minUsage) candidates.Add(p);
        }

        // 4. PICK RANDOM FROM LEAST USED
        KillCamProfile chosenProfile = candidates[Random.Range(0, candidates.Count)];

        // 5. UPDATE COUNT
        _usageMap[chosenProfile]++;
        Debug.Log($"Selected '{chosenProfile.name}' (Usage: {_usageMap[chosenProfile]})");

        ApplyProfile(chosenProfile);
    }

    void ApplyProfile(KillCamProfile p)
    {
        DisableSplitScreen();
        mainCamera.transform.DOKill();
        mainCamera.DOKill();

        // Snap to Start Position
        mainCamera.transform.position = p.mainWorldPos;
        mainCamera.transform.rotation = Quaternion.Euler(p.mainWorldRot);
        mainCamera.fieldOfView = p.mainFOV;

        switch (p.camMode)
        {
            case KillCamMode.Splines:
                ApplySplineProfile(p);
                break;

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

    void ApplySplineProfile(KillCamProfile p)
    {
        if (p.splinePath == null || p.splinePath.Count == 0) return;

        // Prepare Data
        Vector3[] pathPositions = new Vector3[p.splinePath.Count];
        for (int i = 0; i < p.splinePath.Count; i++) pathPositions[i] = p.splinePath[i].pos;

        mainCamera.transform.position = p.mainWorldPos;

        // Position Tween
        var pathTween = mainCamera.transform.DOPath(pathPositions, p.splineDuration, PathType.CatmullRom);
        pathTween.SetEase(p.splineSpeedCurve).SetOptions(false).SetUpdate(true);

        // Rotation Logic
        if (p.lookForward)
        {
            pathTween.SetLookAt(0.01f);
        }
        else
        {
            // Custom Rotation Interpolation
            DOVirtual.Float(0f, 1f, p.splineDuration, (t) =>
            {
                List<Quaternion> rotKeyframes = new List<Quaternion>();
                rotKeyframes.Add(Quaternion.Euler(p.mainWorldRot)); // Start Rot
                foreach (var point in p.splinePath) rotKeyframes.Add(Quaternion.Euler(point.rot));

                int count = rotKeyframes.Count;
                if (count < 2) return;

                float segmentSize = 1f / (count - 1);
                int index = Mathf.FloorToInt(t / segmentSize);
                if (index >= count - 1) index = count - 2;

                float fraction = (t - (index * segmentSize)) / segmentSize;
                mainCamera.transform.rotation = Quaternion.Slerp(rotKeyframes[index], rotKeyframes[index + 1], fraction);

            }).SetEase(p.splineSpeedCurve).SetUpdate(true);
        }
    }

    void ApplyAnimatedProfile(KillCamProfile p)
    {
        DOVirtual.Float(0f, 1f, p.animDuration, (t) =>
        {
            float progress = p.animCurve.Evaluate(t);
            mainCamera.transform.position = Vector3.LerpUnclamped(p.mainWorldPos, p.mainDestPos, progress);
            mainCamera.transform.rotation = Quaternion.SlerpUnclamped(Quaternion.Euler(p.mainWorldRot), Quaternion.Euler(p.mainDestRot), progress);
            mainCamera.fieldOfView = Mathf.LerpUnclamped(p.mainFOV, p.mainDestFOV, progress);
        }).SetUpdate(true);
    }

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

    [ContextMenu("1. Record START Positions")]
    public void RecordStartPositions()
    {
        if (profileToRecord == null) { Debug.LogError("Assign a Profile first!"); return; }
        if (mainCamera)
        {
            profileToRecord.mainWorldPos = mainCamera.transform.position;
            profileToRecord.mainWorldRot = mainCamera.transform.eulerAngles;
            profileToRecord.mainFOV = mainCamera.fieldOfView;
            Debug.Log($"Recorded START for '{profileToRecord.name}'");
        }
        if (auxCamA) { profileToRecord.camA_WorldPos = auxCamA.transform.position; profileToRecord.camA_WorldRot = auxCamA.transform.eulerAngles; profileToRecord.camA_FOV = auxCamA.fieldOfView; }
        if (auxCamB) { profileToRecord.camB_WorldPos = auxCamB.transform.position; profileToRecord.camB_WorldRot = auxCamB.transform.eulerAngles; profileToRecord.camB_FOV = auxCamB.fieldOfView; }
        UnityEditor.EditorUtility.SetDirty(profileToRecord);
    }

    [ContextMenu("2. Record DESTINATION Positions")]
    public void RecordDestinationPositions()
    {
        if (profileToRecord == null) { Debug.LogError("Assign a Profile first!"); return; }
        if (mainCamera)
        {
            profileToRecord.mainDestPos = mainCamera.transform.position;
            profileToRecord.mainDestRot = mainCamera.transform.eulerAngles;
            profileToRecord.mainDestFOV = mainCamera.fieldOfView;
            Debug.Log($"Recorded DESTINATION for '{profileToRecord.name}'");
        }
        UnityEditor.EditorUtility.SetDirty(profileToRecord);
    }

    [ContextMenu("3. Add Current Pos/Rot to Spline")]
    public void AddSplinePoint()
    {
        if (profileToRecord == null) { Debug.LogError("Assign a Profile first!"); return; }
        if (mainCamera)
        {
            SplinePoint sp = new SplinePoint();
            sp.pos = mainCamera.transform.position;
            sp.rot = mainCamera.transform.eulerAngles;
            profileToRecord.splinePath.Add(sp);
            Debug.Log($"Added Spline Point #{profileToRecord.splinePath.Count} to '{profileToRecord.name}'");
        }
        UnityEditor.EditorUtility.SetDirty(profileToRecord);
    }

    [ContextMenu("4. Clear Spline Points")]
    public void ClearSpline()
    {
        if (profileToRecord == null) return;
        profileToRecord.splinePath.Clear();
        Debug.Log("Spline cleared.");
        UnityEditor.EditorUtility.SetDirty(profileToRecord);
    }
#endif
}