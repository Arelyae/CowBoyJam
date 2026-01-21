using UnityEngine;
using FMODUnity;
using FMOD.Studio;
using System;
using System.Runtime.InteropServices;

public class DuelAudioDirector : MonoBehaviour
{
    [Header("--- Links ---")]
    public DuelCinematographer cinematographer;

    [Header("--- FMOD Settings ---")]
    public EventReference duelMusic;

    // Internal FMOD state
    private EventInstance musicInstance;
    private GCHandle timelineHandle;

    // FLAGS
    private static bool _cutTriggered = false;
    private static string _lastMarkerName = "";

    void Start()
    {
        StartMusic(); 
    }

    public void StartMusic()
    {
        // 1. Safety Cleanup first
        StopMusic();

        if (duelMusic.IsNull) return;

        // 2. Create Instance
        musicInstance = RuntimeManager.CreateInstance(duelMusic);

        timelineHandle = GCHandle.Alloc(this);
        musicInstance.setUserData(GCHandle.ToIntPtr(timelineHandle));

        musicInstance.setCallback(OnFMODCallback, EVENT_CALLBACK_TYPE.TIMELINE_MARKER);

        // 3. Reset Flags (Crucial for Restart)
        _cutTriggered = false;
        _lastMarkerName = "";

        musicInstance.start();
        musicInstance.release();
    }

    public void StopMusic()
    {
        // 1. Stop FMOD
        if (musicInstance.isValid())
        {
            musicInstance.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
            musicInstance.setCallback(null);
        }

        // 2. Free Memory
        if (timelineHandle.IsAllocated) timelineHandle.Free();

        // 3. RESET FLAGS
        // This ensures pending marker events don't fire after we stop
        _cutTriggered = false;
    }

    void Update()
    {
        if (_cutTriggered)
        {
            _cutTriggered = false;

            // Log for debug
            // Debug.Log($"AUDIO: Marker detected. Triggering Cut.");

            if (cinematographer != null)
            {
                cinematographer.TriggerNextShot();
            }
        }
    }

    [AOT.MonoPInvokeCallback(typeof(EVENT_CALLBACK))]
    static FMOD.RESULT OnFMODCallback(EVENT_CALLBACK_TYPE type, IntPtr instancePtr, IntPtr parameterPtr)
    {
        if (type == EVENT_CALLBACK_TYPE.TIMELINE_MARKER)
        {
            var parameter = (TIMELINE_MARKER_PROPERTIES)Marshal.PtrToStructure(parameterPtr, typeof(TIMELINE_MARKER_PROPERTIES));
            string detectedName = (string)parameter.name;

            // Simple Filter: Check if marker name contains "Shot", "Cut", or "Next"
            if (detectedName.Contains("Shot") || detectedName.Contains("Cut") || detectedName.Contains("Next"))
            {
                _lastMarkerName = detectedName;
                _cutTriggered = true;
            }
        }
        return FMOD.RESULT.OK;
    }

    void OnDestroy()
    {
        StopMusic();
    }
}