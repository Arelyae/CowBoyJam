using UnityEngine;
using System.Collections.Generic;
using DG.Tweening;
using Unity.Cinemachine;       // Cinemachine 3.x
using UnityEngine.Splines;     // Unity Splines
using Unity.Mathematics;       // Required for Spline math
using System.Linq;

// RESOLVE CONFLICT: Explicitly tell Unity to use its own Random engine
using Random = UnityEngine.Random;

public class CameraDirector : MonoBehaviour
{
    // =================================================================================
    //                                DATA STRUCTURES
    // =================================================================================

    [System.Serializable]
    public class KillCamScenario
    {
        public string name = "New Kill Cam";
        [Tooltip("Drag a GameObject with a SplineContainer from your scene here.")]
        public SplineContainer sourcePath;

        [Header("Timing")]
        public float duration = 4.0f;
        public AnimationCurve speedCurve = AnimationCurve.Linear(0, 0, 1, 1);

        [Header("Orientation")]
        public bool lookAtPlayer;
        public bool lookAtEnemy;

        [Header("Attachment (GoPro Mode)")]
        [Tooltip("If TRUE, camera follows the target's position.")]
        public bool attachToTarget;

        [Header("Rotation Locks")]
        public bool lockRotationZ = true;
        public bool lockRotationX = true;
        public bool lockRotationY = false;

        [Header("Position Locks")]
        public bool lockPositionX = false;
        public bool lockPositionY = false;
        public bool lockPositionZ = false;

        public bool lookForward = true;
    }

    // =================================================================================
    //                                     INSPECTOR
    // =================================================================================

    [Header("--- Cinemachine 3 Setup ---")]
    [Tooltip("The camera used for Kill Cams (Must have SplineDolly).")]
    public CinemachineCamera killCamVC;

    [Tooltip("The camera used for normal gameplay (Priority 10).")]
    public CinemachineCamera gameplayVC;

    [Tooltip("REQUIRED for Legacy Profiles: An empty SplineContainer in the scene.")]
    public SplineContainer sharedSplineContainer;

    [Header("--- Reset Settings ---")]
    [Tooltip("Drag the object where the camera should snap back to (e.g. 'CamHolder' inside your Player).")]
    public Transform gameplayReturnTarget;

    [Header("--- Actors ---")]
    public Transform playerTransform;
    public Transform enemyTransform;

    [Header("--- UI & Aux ---")]
    public GameObject splitScreenCanvas;
    public Camera auxCamA;
    public Camera auxCamB;

    [Header("--- Legacy Profiles ---")]
    public List<KillCamProfile> profiles;

    [Header("--- New Scenarios ---")]
    public List<KillCamScenario> killCamScenarios;

    // =================================================================================
    //                                INTERNAL STATE
    // =================================================================================

    private Dictionary<object, int> _usageMap = new Dictionary<object, int>();
    private CinemachineSplineDolly _dolly;
    private Transform _currentTarget;

    void Start()
    {
        if (killCamVC)
        {
            _dolly = killCamVC.GetComponent<CinemachineSplineDolly>();
            killCamVC.Priority = 0; // Ensure it starts off
        }

        if (gameplayVC)
        {
            gameplayVC.Priority = 10; // Ensure gameplay starts on
        }

        // Initialize Fairness Map
        foreach (var p in profiles) { if (!_usageMap.ContainsKey(p)) _usageMap.Add(p, 0); }
        foreach (var s in killCamScenarios) { if (!_usageMap.ContainsKey(s)) _usageMap.Add(s, 0); }

        ResetCamera();
    }

    // =================================================================================
    //                                MAIN LOGIC
    // =================================================================================

    public void TriggerKillCam()
    {
        // 1. Sync Map
        foreach (var p in profiles) { if (!_usageMap.ContainsKey(p)) _usageMap.Add(p, 0); }
        foreach (var s in killCamScenarios) { if (!_usageMap.ContainsKey(s)) _usageMap.Add(s, 0); }

        if (_usageMap.Count == 0 || killCamVC == null) return;

        Debug.Log("--- KILL CAM TRIGGERED ---");

        // 2. Selection Logic
        int minUsage = _usageMap.Values.Min();
        List<object> candidates = new List<object>();
        candidates.AddRange(profiles.Where(p => _usageMap[p] == minUsage));
        candidates.AddRange(killCamScenarios.Where(s => _usageMap[s] == minUsage));

        if (candidates.Count == 0) return;

        object chosen = candidates[Random.Range(0, candidates.Count)];
        _usageMap[chosen]++;

        // 3. Dispatch
        if (chosen is KillCamProfile profile) ApplyLegacyProfile(profile);
        else if (chosen is KillCamScenario scenario) ApplyScenario(scenario);
    }

    public void ResetCamera()
    {
        Debug.Log("--- RESET CAMERA ---");

        // 1. Kill all Animations
        DOTween.Kill("KillCamTween");
        if (killCamVC) killCamVC.transform.DOKill();

        // 2. DETACH PARENT (Crucial Safety Step)
        if (killCamVC) killCamVC.transform.SetParent(null);

        // 3. Turn OFF Kill Cam
        if (killCamVC) killCamVC.Priority = 0;

        // 4. SNAP GAMEPLAY CAM TO TARGET
        if (gameplayVC != null && gameplayReturnTarget != null)
        {
            // A. Move the VC transform to the player's head immediately
            gameplayVC.transform.position = gameplayReturnTarget.position;
            gameplayVC.transform.rotation = gameplayReturnTarget.rotation;

            // B. Ensure Priority is high enough to take over
            gameplayVC.Priority = 10;
        }
        else
        {
            Debug.LogWarning("ResetCamera: 'Gameplay VC' or 'Return Target' is missing!");
        }

        _currentTarget = null;
        DisableSplitScreen();
    }

    // --- NEW: CALLED BY FINAL SCORE MANAGER ---
    public void DisableAllCameras()
    {
        Debug.Log("--- DISABLING ALL GAMEPLAY CAMERAS FOR CINEMATIC ---");

        // Stop any running animations
        DOTween.Kill("KillCamTween");
        if (killCamVC) killCamVC.transform.DOKill();

        // Set all priorities to 0 so the Final Camera (Priority 200) takes over
        if (killCamVC) killCamVC.Priority = 0;
        if (gameplayVC) gameplayVC.Priority = 0;

        DisableSplitScreen();
    }
    // ------------------------------------------

    // =================================================================================
    //                                NEW SCENARIO LOGIC (Scene Splines)
    // =================================================================================

    void ApplyScenario(KillCamScenario s)
    {
        DisableSplitScreen();
        killCamVC.transform.DOKill();
        DOTween.Kill("KillCamTween");

        // IMPORTANT: Enable the Dolly for splines
        if (_dolly != null) _dolly.enabled = true;

        _currentTarget = GetTarget(s.lookAtEnemy, s.lookAtPlayer);

        // A. Assign Spline
        if (_dolly != null && s.sourcePath != null)
        {
            _dolly.Spline = s.sourcePath;
            _dolly.CameraPosition = 0f;

            DOVirtual.Float(0f, 1f, s.duration, (val) =>
            {
                if (_dolly != null) _dolly.CameraPosition = val;
            }).SetEase(s.speedCurve).SetId("KillCamTween").SetUpdate(true);
        }

        // B. Rotation & Follow Loop
        StartFollowAndRotateLoop(s.duration, s.lookForward, s.sourcePath,
                                 s.attachToTarget,
                                 s.lockRotationX, s.lockRotationY, s.lockRotationZ,
                                 s.lockPositionX, s.lockPositionY, s.lockPositionZ);

        // C. Take Control
        killCamVC.Priority = 100;
    }

    // =================================================================================
    //                                LEGACY PROFILE LOGIC (Scriptable Objects)
    // =================================================================================

    void ApplyLegacyProfile(KillCamProfile p)
    {
        DisableSplitScreen();
        killCamVC.transform.DOKill();
        DOTween.Kill("KillCamTween");

        _currentTarget = GetTarget(p.lookAtEnemy, p.lookAtPlayer);
        killCamVC.Lens.FieldOfView = p.mainFOV;

        // CRITICAL FIX: Only enable Dolly if we are actually using Splines!
        if (_dolly != null)
        {
            if (p.camMode == KillCamMode.Splines) _dolly.enabled = true;
            else _dolly.enabled = false;
        }

        switch (p.camMode)
        {
            case KillCamMode.Splines:
                ApplyLegacySpline(p);
                break;

            case KillCamMode.Animated:
                ApplyAnimatedProfile(p);
                break;

            case KillCamMode.SplitScreen:
                EnableSplitScreen(p);
                return; // EXIT (Don't enable VC)

            case KillCamMode.Standard:
            default:
                // 1. Set Position
                killCamVC.transform.position = p.mainWorldPos;
                killCamVC.transform.rotation = Quaternion.Euler(p.mainWorldRot);

                // 2. Start Continuous Loop
                StartFollowAndRotateLoop(60f, false, null,
                                         p.attachToTarget,
                                         p.lockRotationX, p.lockRotationY, p.lockRotationZ,
                                         p.lockPositionX, p.lockPositionY, p.lockPositionZ);
                break;
        }

        killCamVC.Priority = 100;
    }

    void ApplyLegacySpline(KillCamProfile p)
    {
        if (sharedSplineContainer == null) return;

        // 1. Build Spline
        Spline spline = sharedSplineContainer.Spline;
        if (spline == null) spline = sharedSplineContainer.AddSpline();
        spline.Clear();
        sharedSplineContainer.transform.position = Vector3.zero;
        sharedSplineContainer.transform.rotation = Quaternion.identity;

        if (p.splinePath != null)
        {
            foreach (var point in p.splinePath)
                spline.Add(new BezierKnot(point.pos), TangentMode.AutoSmooth);
        }

        // 2. Animate Dolly
        if (_dolly != null)
        {
            _dolly.Spline = sharedSplineContainer;
            _dolly.CameraPosition = 0f;
            DOVirtual.Float(0f, 1f, p.splineDuration, (val) =>
            {
                if (_dolly != null) _dolly.CameraPosition = val;
            }).SetEase(p.splineSpeedCurve).SetId("KillCamTween").SetUpdate(true);
        }

        // 3. Rotation Logic (Continuous)
        StartFollowAndRotateLoop(p.splineDuration, p.lookForward, sharedSplineContainer,
                                 p.attachToTarget,
                                 p.lockRotationX, p.lockRotationY, p.lockRotationZ,
                                 p.lockPositionX, p.lockPositionY, p.lockPositionZ);
    }

    void ApplyAnimatedProfile(KillCamProfile p)
    {
        // For animated, we simplify: We either rely on the Animation Curve OR the Attachment logic.
        // Mixing both is complex, so here we prioritize the Curve, but use LookAt Logic.

        DOVirtual.Float(0f, 1f, p.animDuration, (t) =>
        {
            float progress = p.animCurve.Evaluate(t);
            killCamVC.transform.position = Vector3.LerpUnclamped(p.mainWorldPos, p.mainDestPos, progress);

            // Continuous LookAt during animation
            if (_currentTarget != null)
                killCamVC.transform.LookAt(_currentTarget);
            else
                killCamVC.transform.rotation = Quaternion.SlerpUnclamped(Quaternion.Euler(p.mainWorldRot), Quaternion.Euler(p.mainDestRot), progress);

            killCamVC.Lens.FieldOfView = Mathf.LerpUnclamped(p.mainFOV, p.mainDestFOV, progress);
        }).SetId("KillCamTween").SetUpdate(true);
    }

    // =================================================================================
    //                  CORE LOOP: HANDLES ROTATION, POSITION & LOCKS
    // =================================================================================

    void StartFollowAndRotateLoop(float duration, bool lookForward, SplineContainer path,
                                  bool isAttached,
                                  bool lockRotX, bool lockRotY, bool lockRotZ,
                                  bool lockPosX, bool lockPosY, bool lockPosZ)
    {
        // 1. Capture Initial States
        Vector3 initialOffset = Vector3.zero;
        Quaternion initialCameraRot = killCamVC.transform.rotation;
        Vector3 initialTargetPos = Vector3.zero;

        if (_currentTarget != null)
        {
            initialTargetPos = _currentTarget.position;
            // Calculate vector from Target -> Camera
            initialOffset = killCamVC.transform.position - _currentTarget.position;
        }

        DOVirtual.Float(0f, 1f, duration, (t) => {
            if (killCamVC == null) return;

            // --- A. POSITION (ATTACHMENT & LOCKING) ---
            if (isAttached && _currentTarget != null)
            {
                // Determine which target position to use (Current vs Initial)
                Vector3 currentTargetPos = _currentTarget.position;
                Vector3 targetPosToUse = currentTargetPos;

                if (lockPosX) targetPosToUse.x = initialTargetPos.x;
                if (lockPosY) targetPosToUse.y = initialTargetPos.y;
                if (lockPosZ) targetPosToUse.z = initialTargetPos.z;

                // Apply offset to the blended target position
                killCamVC.transform.position = targetPosToUse + initialOffset;
            }

            // --- B. ROTATION (LOOKAT & LOCKING) ---
            Quaternion targetRotation = killCamVC.transform.rotation; // Default

            if (_currentTarget != null)
            {
                // Basic LookAt
                Vector3 direction = _currentTarget.position - killCamVC.transform.position;
                if (direction != Vector3.zero) targetRotation = Quaternion.LookRotation(direction);
            }
            else if (lookForward && path != null)
            {
                float3 pos, tangent, up;
                path.Evaluate(t, out pos, out tangent, out up);
                Vector3 tanVec = (Vector3)tangent; Vector3 upVec = (Vector3)up;
                if (tanVec != Vector3.zero) targetRotation = Quaternion.LookRotation(tanVec, upVec);
            }

            // Apply Rotation Locking
            if (isAttached && _currentTarget != null)
            {
                Vector3 currentEuler = targetRotation.eulerAngles;
                Vector3 stableEuler = initialCameraRot.eulerAngles;

                float x = lockRotX ? stableEuler.x : currentEuler.x;
                float y = lockRotY ? stableEuler.y : currentEuler.y;
                float z = lockRotZ ? stableEuler.z : currentEuler.z;

                targetRotation = Quaternion.Euler(x, y, z);
            }

            killCamVC.transform.rotation = targetRotation;

        }).SetId("KillCamTween").SetUpdate(true);
    }

    // =================================================================================
    //                                HELPERS
    // =================================================================================

    Transform GetTarget(bool lookEnemy, bool lookPlayer)
    {
        if (lookEnemy && enemyTransform != null) return enemyTransform;
        if (lookPlayer && playerTransform != null) return playerTransform;
        return null;
    }

    void EnableSplitScreen(KillCamProfile p)
    {
        if (killCamVC) killCamVC.Priority = 0;
        if (splitScreenCanvas) splitScreenCanvas.SetActive(true);
        if (auxCamA)
        {
            auxCamA.gameObject.SetActive(true);
            auxCamA.fieldOfView = p.camA_FOV;
            auxCamA.transform.position = p.camA_WorldPos;
            auxCamA.transform.rotation = Quaternion.Euler(p.camA_WorldRot);
            if (p.lookAtPlayer && playerTransform) auxCamA.transform.LookAt(playerTransform);
            if (p.lookAtEnemy && enemyTransform) auxCamA.transform.LookAt(enemyTransform);
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
}