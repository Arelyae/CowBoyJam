using UnityEngine;
using FMODUnity;
using FMOD.Studio;
using System.Collections.Generic;
using DG.Tweening;
using System;
using System.Runtime.InteropServices;
using AOT;

public class DuelAudioDirector : MonoBehaviour
{
    [Header("--- FMOD Settings ---")]
    public EventReference duelMusic;

    [Tooltip("Name of the global intensity parameter in FMOD Studio")]
    public string intensityParamName = "Intensity";

    [Tooltip("The text to look for inside FMOD Markers to trigger the signal.")]
    public string markerSearchString = "NextShot_"; // <--- NEW VARIABLE

    [Header("--- Stingers (Parameters) ---")]
    [Tooltip("List of FMOD Parameter NAMES (strings) that trigger a stinger within the main music event.")]
    public List<string> victoryStingerParams;

    [Header("--- Transition Settings ---")]
    [Tooltip("How fast the intensity moves towards the target.")]
    public float smoothingSpeed = 20f;

    // --- SIGNAL TO OTHER SCRIPTS ---
    // Subscribe to this: audioDirector.OnNextShotMarker += YourMethod;
    public event Action<string> OnNextShotMarker;

    // Internal FMOD instance
    private EventInstance musicInstance;

    // Logic for smoothing
    private float currentIntensity = 0f;
    private float targetIntensity = 0f;

    // Logic for No-Repeat
    private int lastStingerIndex = -1;

    // --- CALLBACK VARIABLES ---
    private FMOD.Studio.EVENT_CALLBACK _musicCallback;
    private GCHandle _gcHandle;
    private bool _markerDetected = false;
    private string _markerString = "";

    void Start()
    {
        if (!IsPlaying())
        {
            StartMusic();
        }
    }

    void Update()
    {
        // 1. UPDATE INTENSITY
        if (musicInstance.isValid() && Mathf.Abs(currentIntensity - targetIntensity) > 0.01f)
        {
            currentIntensity = Mathf.MoveTowards(currentIntensity, targetIntensity, smoothingSpeed * Time.deltaTime);
            musicInstance.setParameterByName(intensityParamName, currentIntensity);
        }

        // 2. DISPATCH MARKER SIGNAL (Main Thread)
        if (_markerDetected)
        {
            _markerDetected = false;

            if (OnNextShotMarker != null)
            {
                Debug.Log($"[AUDIO DIRECTOR] Dispatching Signal: {_markerString}");
                OnNextShotMarker.Invoke(_markerString);
            }
        }
    }

    public void StartMusic()
    {
        if (duelMusic.IsNull) return;

        musicInstance = RuntimeManager.CreateInstance(duelMusic);

        // --- SETUP CALLBACK ---
        _gcHandle = GCHandle.Alloc(this);
        musicInstance.setUserData(GCHandle.ToIntPtr(_gcHandle));

        _musicCallback = new FMOD.Studio.EVENT_CALLBACK(MusicCallback);
        musicInstance.setCallback(_musicCallback, FMOD.Studio.EVENT_CALLBACK_TYPE.TIMELINE_MARKER);
        // ----------------------

        musicInstance.start();
        musicInstance.release();
        musicInstance.setParameterByName(intensityParamName, currentIntensity);
    }

    // --- STATIC FMOD CALLBACK ---
    [MonoPInvokeCallback(typeof(FMOD.Studio.EVENT_CALLBACK))]
    static FMOD.RESULT MusicCallback(FMOD.Studio.EVENT_CALLBACK_TYPE type, IntPtr instancePtr, IntPtr parameterPtr)
    {
        if (type == FMOD.Studio.EVENT_CALLBACK_TYPE.TIMELINE_MARKER)
        {
            FMOD.Studio.EventInstance instance = new FMOD.Studio.EventInstance(instancePtr);
            IntPtr userDataPtr;
            instance.getUserData(out userDataPtr);

            if (userDataPtr != IntPtr.Zero)
            {
                GCHandle handle = GCHandle.FromIntPtr(userDataPtr);
                if (handle.IsAllocated && handle.Target is DuelAudioDirector director)
                {
                    var parameter = (FMOD.Studio.TIMELINE_MARKER_PROPERTIES)Marshal.PtrToStructure(parameterPtr, typeof(FMOD.Studio.TIMELINE_MARKER_PROPERTIES));
                    string name = (string)parameter.name;

                    // USE THE INSPECTOR VARIABLE FOR COMPARISON
                    if (name != null && name.Contains(director.markerSearchString))
                    {
                        director._markerString = name;
                        director._markerDetected = true;
                    }
                }
            }
        }
        return FMOD.RESULT.OK;
    }

    // --- STINGER LOGIC ---
    public void PlayVictoryStinger(int index = -1)
    {
        if (victoryStingerParams == null || victoryStingerParams.Count == 0) return;
        if (!musicInstance.isValid()) return;

        int targetIndex = -1;

        if (index != -1)
        {
            targetIndex = Mathf.Clamp(index, 0, victoryStingerParams.Count - 1);
        }
        else
        {
            if (victoryStingerParams.Count == 1) targetIndex = 0;
            else
            {
                do { targetIndex = UnityEngine.Random.Range(0, victoryStingerParams.Count); }
                while (targetIndex == lastStingerIndex);
            }
        }

        lastStingerIndex = targetIndex;
        string paramName = victoryStingerParams[targetIndex];

        if (!string.IsNullOrEmpty(paramName))
        {
            Debug.Log($"[AUDIO] Triggering Stinger: '{paramName}'");
            musicInstance.setParameterByName(paramName, 1f);

            DOVirtual.DelayedCall(0.1f, () =>
            {
                if (musicInstance.isValid()) musicInstance.setParameterByName(paramName, 0f);
            }).SetUpdate(true);
        }
    }

    public void SetIntensity(float value)
    {
        targetIntensity = Mathf.Clamp(value, 0f, 100f);
        Debug.Log($"[AUDIO] Target Intensity FORCED to: {targetIntensity}");
    }

    public void IncreaseIntensity(float amount) { targetIntensity = Mathf.Clamp(targetIntensity + amount, 0f, 100f); }
    public void DecreaseIntensity(float amount) { targetIntensity = Mathf.Clamp(targetIntensity - amount, 0f, 100f); }
    public void ResetIntensity() { targetIntensity = 0f; }

    public void StopMusic()
    {
        if (musicInstance.isValid())
        {
            musicInstance.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
        }
    }

    private bool IsPlaying()
    {
        if (!musicInstance.isValid()) return false;
        PLAYBACK_STATE state;
        musicInstance.getPlaybackState(out state);
        return state != PLAYBACK_STATE.STOPPED;
    }

    void OnDestroy()
    {
        StopMusic();
        if (_gcHandle.IsAllocated) _gcHandle.Free();
    }
}