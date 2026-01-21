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
    private bool _isLocked = false; // NEW: Prevents switching on Fumble
    private int _shotIndex = 0;

    // Priorities
    private const int PRIORITY_ACTIVE = 20;
    private const int PRIORITY_INACTIVE = 0;

    // --- EVENT LISTENING ---
    private void OnEnable()
    {
        if (playerController != null)
        {
            playerController.OnFumble += LockCamera;
            // Optional: You can also lock on Death if you want the camera to stay put when shot
            playerController.OnDeath += LockCamera;
        }
    }

    private void OnDisable()
    {
        if (playerController != null)
        {
            playerController.OnFumble -= LockCamera;
            playerController.OnDeath -= LockCamera;
        }
    }

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
        // If we are in the danger zone OR the player has fumbled, we DO NOT switch.
        // We stay on the current shot to show the tension or the failure.
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
        _isLocked = false; // Clean reset

        ResetAllCameras();
    }

    // --- CORE LOGIC ---

    bool IsInDangerZone()
    {
        float enemyWait = (enemyAI.difficultyProfile != null) ? enemyAI.difficultyProfile.minWaitTime : 2.0f;
        float switchCutoffTime = _duelStartTime + enemyWait - safetyBuffer;
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