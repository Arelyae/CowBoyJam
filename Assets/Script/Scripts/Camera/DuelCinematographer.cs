using UnityEngine;
using Unity.Cinemachine; // Unity 6 / Cinemachine 3.x
using System.Collections;
using System.Collections.Generic;

public class DuelCinematographer : MonoBehaviour
{
    [System.Serializable]
    public class CinematicShot
    {
        public string name;
        [Tooltip("The Virtual Camera to activate for this shot.")]
        public CinemachineCamera virtualCamera;
    }

    [Header("--- Logic Links ---")]
    public EnemyDuelAI enemyAI;
    public DuelController playerController;
    public DuelArbiter arbiter;
    public DuelAudioDirector audioDirector; // <--- NEW REFERENCE

    [Header("--- Cinematic Sequences ---")]
    [Tooltip("The script will step through these shots one by one when TriggerNextShot() is called.")]
    public List<CinematicShot> availableShots;

    [Header("--- Safety ---")]
    [Tooltip("Ignore 'Next Shot' triggers if we are this close (in seconds) to the Enemy's attack time.")]
    public float safetyBuffer = 1.0f;

    // Internal State
    private float _duelStartTime;
    private CinemachineCamera _currentCam;
    private bool _isActive = false;
    private bool _isLocked = false;
    private int _shotIndex = 0;

    // Priorities
    private const int PRIORITY_ACTIVE = 20;
    private const int PRIORITY_INACTIVE = 0;

    // --- EVENT LISTENING ---
    private void OnEnable()
    {
        // 1. Listen to Player Events (Fumble/Death)
        if (playerController != null)
        {
            playerController.OnFumble += LockCamera;
            playerController.OnDeath += LockCamera;
        }

        // 2. NEW: Listen to Audio Markers (Music Cuts)
        if (audioDirector != null)
        {
            audioDirector.OnNextShotMarker += HandleAudioMarker;
        }
    }

    private void OnDisable()
    {
        if (playerController != null)
        {
            playerController.OnFumble -= LockCamera;
            playerController.OnDeath -= LockCamera;
        }

        if (audioDirector != null)
        {
            audioDirector.OnNextShotMarker -= HandleAudioMarker;
        }
    }

    // --- NEW HANDLER ---
    private void HandleAudioMarker(string markerName)
    {
        // The AudioDirector has already filtered this to ensure it contains "NextShot_"
        // We simply proceed to the next camera in the list.
        // (You could parse markerName if you wanted specific shot logic, e.g. "NextShot_CloseUp")
        TriggerNextShot();
    }
    // -------------------

    private void LockCamera()
    {
        // The player messed up! Freeze the camera system.
        _isLocked = true;
    }

    // --- PUBLIC API ---

    public void StartCinematicSequence()
    {
        StopCinematics();
        TriggerNextShot();
    }

    public void TriggerNextShot()
    {
        // 1. FIRST TIME TRIGGER (START)
        if (!_isActive)
        {
            if (availableShots.Count == 0) return;

            _isActive = true;
            _isLocked = false; // Reset lock on start
            _duelStartTime = Time.time;
            _shotIndex = 0;
        }

        // 2. CHECK BLOCKERS (Danger Zone OR Fumble Lock)
        if (IsInDangerZone() || _isLocked)
        {
            return;
        }

        // 3. GET AND ACTIVATE NEXT SHOT
        CinematicShot nextShot = GetNextShot();

        if (nextShot != null)
        {
            ActivateShot(nextShot);
        }
        else
        {
            // End of list -> Stop (Return to gameplay or hold last shot)
            StopCinematics();
        }
    }

    public void StopCinematics()
    {
        if (!_isActive) return;

        _isActive = false;
        _isLocked = false;

        ResetAllCameras();
    }

    // --- CORE LOGIC ---

    bool IsInDangerZone()
    {
        float enemyWait = (enemyAI.difficultyProfile != null) ? enemyAI.difficultyProfile.minWaitTime : 2.0f;
        float switchCutoffTime = _duelStartTime + enemyWait - safetyBuffer;

        // If we are past the cutoff, we are in danger.
        return Time.time > switchCutoffTime;
    }

    // --- HELPER FUNCTIONS ---

    void ActivateShot(CinematicShot shot)
    {
        if (_currentCam != null) _currentCam.Priority = PRIORITY_INACTIVE;

        if (shot.virtualCamera != null)
        {
            shot.virtualCamera.Priority = PRIORITY_ACTIVE;
            _currentCam = shot.virtualCamera;

            // Optional: Reset Dolly Track if using Tracked Dolly
            // (Requires CinemachineSplineDolly or similar component)
            var dolly = shot.virtualCamera.GetComponent<CinemachineSplineDolly>();
            if (dolly != null)
            {
                dolly.CameraPosition = 0f;
            }
        }
    }

    CinematicShot GetNextShot()
    {
        if (availableShots.Count == 0) return null;

        // If we run out of shots, wrap around? Or stop?
        // Current logic: Stop (returns null if index >= count)
        // If you want to loop: _shotIndex % availableShots.Count
        if (_shotIndex >= availableShots.Count) return null;

        CinematicShot shot = availableShots[_shotIndex];
        _shotIndex++;
        return shot;
    }

    void ResetAllCameras()
    {
        foreach (var shot in availableShots)
        {
            if (shot.virtualCamera != null) shot.virtualCamera.Priority = PRIORITY_INACTIVE;
        }
        _currentCam = null;
    }
}