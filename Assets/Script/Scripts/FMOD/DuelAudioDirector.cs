using UnityEngine;
using FMODUnity;
using FMOD.Studio;
using System.Collections.Generic;
using DG.Tweening;

public class DuelAudioDirector : MonoBehaviour
{
    [Header("--- FMOD Settings ---")]
    public EventReference duelMusic;

    [Tooltip("Name of the global intensity parameter in FMOD Studio")]
    public string intensityParamName = "Intensity";

    [Header("--- Stingers (Parameters) ---")]
    [Tooltip("List of FMOD Parameter NAMES (strings) that trigger a stinger within the main music event.")]
    public List<string> victoryStingerParams;

    [Header("--- Transition Settings ---")]
    [Tooltip("How fast the intensity moves towards the target.")]
    public float smoothingSpeed = 20f;

    // Internal FMOD instance
    private EventInstance musicInstance;

    // Logic for smoothing
    private float currentIntensity = 0f;
    private float targetIntensity = 0f;

    // Logic for No-Repeat
    private int lastStingerIndex = -1;

    void Start()
    {
        if (!IsPlaying())
        {
            StartMusic();
        }
    }

    void Update()
    {
        // Smoothly interpolate Intensity Parameter
        if (musicInstance.isValid() && Mathf.Abs(currentIntensity - targetIntensity) > 0.01f)
        {
            currentIntensity = Mathf.MoveTowards(currentIntensity, targetIntensity, smoothingSpeed * Time.deltaTime);
            musicInstance.setParameterByName(intensityParamName, currentIntensity);
        }
    }

    public void StartMusic()
    {
        if (duelMusic.IsNull) return;

        musicInstance = RuntimeManager.CreateInstance(duelMusic);
        musicInstance.start();
        musicInstance.release();
        musicInstance.setParameterByName(intensityParamName, currentIntensity);
    }

    // --- UPDATED: NO REPEAT LOGIC + AUTO RESET ---
    public void PlayVictoryStinger(int index = -1)
    {
        if (victoryStingerParams == null || victoryStingerParams.Count == 0) return;
        if (!musicInstance.isValid()) return;

        int targetIndex = -1;

        // A. Specific Request (Force index)
        if (index != -1)
        {
            targetIndex = Mathf.Clamp(index, 0, victoryStingerParams.Count - 1);
        }
        // B. Random Request (Avoid Repeat)
        else
        {
            if (victoryStingerParams.Count == 1)
            {
                // Only one option, we have to repeat it
                targetIndex = 0;
            }
            else
            {
                // Reroll until we get a different index
                do
                {
                    targetIndex = Random.Range(0, victoryStingerParams.Count);
                }
                while (targetIndex == lastStingerIndex);
            }
        }

        // Save for next time
        lastStingerIndex = targetIndex;

        // Trigger the Stinger
        string paramName = victoryStingerParams[targetIndex];

        if (!string.IsNullOrEmpty(paramName))
        {
            Debug.Log($"[AUDIO] Triggering Stinger: '{paramName}' (1 -> 0)");

            // 1. Trigger ON
            musicInstance.setParameterByName(paramName, 1f);

            // 2. Trigger OFF (Auto-Reset after 0.1s)
            DOVirtual.DelayedCall(0.1f, () =>
            {
                if (musicInstance.isValid())
                {
                    musicInstance.setParameterByName(paramName, 0f);
                }
            }).SetUpdate(true);
        }
    }
    // ---------------------------------------------

    public void SetIntensity(float value)
    {
        targetIntensity = Mathf.Clamp(value, 0f, 100f);
        Debug.Log($"[AUDIO] Target Intensity FORCED to: {targetIntensity}");
    }

    public void IncreaseIntensity(float amount)
    {
        targetIntensity = Mathf.Clamp(targetIntensity + amount, 0f, 100f);
    }

    public void DecreaseIntensity(float amount)
    {
        targetIntensity = Mathf.Clamp(targetIntensity - amount, 0f, 100f);
    }

    public void ResetIntensity()
    {
        targetIntensity = 0f;
    }

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
    }
}