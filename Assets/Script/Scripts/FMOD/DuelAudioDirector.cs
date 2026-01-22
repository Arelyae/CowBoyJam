using UnityEngine;
using FMODUnity;
using FMOD.Studio;

public class DuelAudioDirector : MonoBehaviour
{
    [Header("--- FMOD Settings ---")]
    public EventReference duelMusic;
    [Tooltip("Name of the local parameter in FMOD Studio")]
    public string intensityParamName = "Intensity";

    [Header("--- Transition Settings ---")]
    [Tooltip("How fast the intensity moves towards the target (Units per Second). Example: 20 means it takes 5 seconds to go from 0 to 100.")]
    public float smoothingSpeed = 20f;

    // Internal FMOD instance
    private EventInstance musicInstance;

    // Logic for smoothing
    private float currentIntensity = 0f;
    private float targetIntensity = 0f;

    void Start()
    {
        if (!IsPlaying())
        {
            StartMusic();
        }
    }

    void Update()
    {
        // Only update if we have a valid instance and we aren't at the target yet
        // We use a small epsilon (0.01f) to stop processing when close enough
        if (musicInstance.isValid() && Mathf.Abs(currentIntensity - targetIntensity) > 0.01f)
        {
            // Move current towards target smoothly
            currentIntensity = Mathf.MoveTowards(currentIntensity, targetIntensity, smoothingSpeed * Time.deltaTime);

            // Apply to FMOD
            musicInstance.setParameterByName(intensityParamName, currentIntensity);
        }
    }

    public void StartMusic()
    {
        if (duelMusic.IsNull) return;

        musicInstance = RuntimeManager.CreateInstance(duelMusic);
        musicInstance.start();
        musicInstance.release();

        // Apply initial state instantly
        musicInstance.setParameterByName(intensityParamName, currentIntensity);
    }

    public void IncreaseIntensity(float amount)
    {
        // Add to TARGET
        targetIntensity = Mathf.Clamp(targetIntensity + amount, 0f, 100f);
        Debug.Log($"[AUDIO] Target Intensity INCREASED to: {targetIntensity}");
    }

    public void DecreaseIntensity(float amount)
    {
        // Subtract from TARGET
        targetIntensity = Mathf.Clamp(targetIntensity - amount, 0f, 100f);
        Debug.Log($"[AUDIO] Target Intensity DECREASED to: {targetIntensity}");
    }

    public void ResetIntensity()
    {
        // Just set the target. The Update loop handles the smooth fade down.
        targetIntensity = 0f;
        Debug.Log($"[AUDIO] Target Intensity Set to 0 (Smoothing down...)");
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