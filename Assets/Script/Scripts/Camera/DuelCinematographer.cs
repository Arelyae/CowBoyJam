using UnityEngine;
using Unity.Cinemachine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening; // Using DOTween for reliable timers

public class DuelCinematographer : MonoBehaviour
{
    // Internal struct to hold the runtime data
    private class RuntimeShot
    {
        public CinemachineCamera cam;
        public float duration;
    }

    [Header("--- Logic Links ---")]
    public EnemyDuelAI enemyAI;
    public DuelController playerController;
    public DuelAudioDirector audioDirector;

    [Header("--- Scene Registry ---")]
    [Tooltip("Drag EVERY Cinemachine Camera in your scene here.")]
    public List<CinemachineCamera> allSceneCameras;

    // The Playlist now holds our runtime data struct
    private List<RuntimeShot> _currentPlaylist = new List<RuntimeShot>();

    [Header("--- Safety ---")]
    public float safetyBuffer = 1.0f;

    // Internal State
    private float _duelStartTime;
    private CinemachineCamera _activeCam;
    private bool _isActive = false;
    private bool _isLocked = false;
    private int _playlistIndex = 0;

    // Timer Reference (so we can kill it if an Audio Marker happens first)
    private Tween _shotTimer;

    // Priorities
    private const int PRIORITY_ACTIVE = 20;
    private const int PRIORITY_INACTIVE = 0;

    private void OnEnable()
    {
        if (playerController != null)
        {
            playerController.OnFumble += LockCamera;
            playerController.OnDeath += LockCamera;
        }
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

    // --- LOADING THE PROFILE ---
    public void LoadProfileCinematics(DuelEnemyProfile profile)
    {
        _currentPlaylist.Clear();
        StopCinematics(); // Clear state

        if (profile.cinematicSequence == null || profile.cinematicSequence.Count == 0)
        {
            Debug.Log($"[CINEMATICS] Enemy {profile.enemyName} has no shots defined.");
            return;
        }

        foreach (var step in profile.cinematicSequence)
        {
            // Find the camera by name
            CinemachineCamera foundCam = allSceneCameras.FirstOrDefault(c => c.gameObject.name == step.cameraName);

            if (foundCam != null)
            {
                _currentPlaylist.Add(new RuntimeShot
                {
                    cam = foundCam,
                    duration = step.duration
                });
            }
            else
            {
                Debug.LogWarning($"[CINEMATICS] Camera '{step.cameraName}' not found in registry.");
            }
        }

        Debug.Log($"[CINEMATICS] Loaded {_currentPlaylist.Count} shots for {profile.enemyName}");
    }

    // --- HANDLERS ---
    private void HandleAudioMarker(string markerName)
    {
        // Audio Marker forces the next shot immediately
        // This will automatically kill any running timer for the current shot
        Debug.Log($"[CINEMATICS] Audio Marker '{markerName}' received. Switching.");
        TriggerNextShot();
    }

    private void LockCamera()
    {
        _isLocked = true;
        _shotTimer?.Kill(); // Stop auto-switching if player fumbles
    }

    // --- PUBLIC API ---

    public void StartCinematicSequence()
    {
        StopCinematics();
        TriggerNextShot();
    }

    public void TriggerNextShot()
    {
        // Kill any existing timer so we don't double-skip
        _shotTimer?.Kill();

        // 1. FIRST TIME START
        if (!_isActive)
        {
            if (_currentPlaylist.Count == 0) return;
            _isActive = true;
            _isLocked = false;
            _duelStartTime = Time.time;
            _playlistIndex = 0;
        }

        // 2. CHECK BLOCKERS
        if (IsInDangerZone() || _isLocked) return;

        // 3. ACTIVATE NEXT SHOT
        if (_playlistIndex < _currentPlaylist.Count)
        {
            RuntimeShot currentShot = _currentPlaylist[_playlistIndex];
            ActivateCamera(currentShot.cam);

            // 4. HANDLE DURATION
            if (currentShot.duration > 0f)
            {
                // Auto-switch after 'duration' seconds
                _shotTimer = DOVirtual.DelayedCall(currentShot.duration, () =>
                {
                    if (_isActive && !_isLocked)
                    {
                        TriggerNextShot(); // Recursion (Safe via Delay)
                    }
                }).SetUpdate(true); // Ignore timeScale if needed, or false if you want it paused on pause
            }

            _playlistIndex++;
        }
        else
        {
            // Playlist finished
            StopCinematics();
        }
    }

    public void StopCinematics()
    {
        _shotTimer?.Kill();

        if (!_isActive) return;

        _isActive = false;
        _isLocked = false;
        ResetAllCameras();
    }

    bool IsInDangerZone()
    {
        float enemyWait = (enemyAI.difficultyProfile != null) ? enemyAI.difficultyProfile.minWaitTime : 2.0f;
        float switchCutoffTime = _duelStartTime + enemyWait - safetyBuffer;
        return Time.time > switchCutoffTime;
    }

    void ActivateCamera(CinemachineCamera cam)
    {
        if (_activeCam != null) _activeCam.Priority = PRIORITY_INACTIVE;

        if (cam != null)
        {
            cam.Priority = PRIORITY_ACTIVE;
            _activeCam = cam;

            var dolly = cam.GetComponent<CinemachineSplineDolly>();
            if (dolly != null) dolly.CameraPosition = 0f;
        }
    }

    void ResetAllCameras()
    {
        foreach (var cam in allSceneCameras)
        {
            if (cam != null) cam.Priority = PRIORITY_INACTIVE;
        }
        _activeCam = null;
    }
}