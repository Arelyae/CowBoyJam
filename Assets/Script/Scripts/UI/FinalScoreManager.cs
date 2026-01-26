using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using FMODUnity;
using Unity.Cinemachine;
using UnityEngine.InputSystem;

public class FinalScoreManager : MonoBehaviour
{
    [Header("--- References ---")]
    public CameraDirector cameraDirector;
    public DuelAudioDirector audioDirector;
    public LeaderboardManager leaderboardManager;

    [Header("--- Cinematic ---")]
    [Tooltip("Drag your Cinemachine Camera here.")]
    public CinemachineCamera finalCamera;

    [Header("--- UI Container ---")]
    public GameObject finalScorePanel;
    public CanvasGroup mainCanvasGroup;

    [Header("--- Stat Rows ---")]
    public TextMeshProUGUI avgReflexText;
    public Image avgReflexBackground;
    public TextMeshProUGUI avgDrawText;
    public Image avgDrawBackground;
    public TextMeshProUGUI totalScoreText;
    public Image totalScoreBackground;

    [Header("--- Navigation Prompts ---")]
    public CanvasGroup restartPromptGroup;

    [Header("--- Animation Timings ---")]
    public float startDelay = 1.0f;
    public float fillDuration = 0.5f;
    public float delayAfterFill = 0.3f;
    public float labelTypingDuration = 0.5f;
    public float countingDuration = 1.5f;
    public float delayBetweenStats = 0.6f;
    public float fadeInDuration = 1.0f;
    [Tooltip("How long to wait after the score finishes before opening the Leaderboard.")]
    public float delayBeforeLeaderboard = 1.0f;

    [Header("--- Colors ---")]
    public Color labelColor = Color.white;
    public Color valueColor = new Color(1f, 0.84f, 0f); // Gold
    public Color totalScoreColor = Color.red;

    [Header("--- Audio ---")]
    public EventReference fillerSound;
    public EventReference typingSound;
    public EventReference countingSound;
    public EventReference finishSound;
    public EventReference ambienceSound;

    [Tooltip("How many milliseconds pass before playing the next count sound/haptic.")]
    [Range(1, 50)] public int audioTriggerStep = 3;

    [Header("--- Haptics ---")]
    [Range(0f, 1f)] public float lowFreqMotor = 0.5f;
    [Range(0f, 1f)] public float highFreqMotor = 0.1f;
    public float hapticDuration = 0.05f;

    // --- State ---
    public bool IsAnimating { get; private set; } = false;
    public bool IsSequenceFinished { get; private set; } = false;

    private Sequence _seq;
    private float _finalReflex;
    private float _finalDraw;
    private float _finalTotal;

    void Start()
    {
        if (finalScorePanel) finalScorePanel.SetActive(false);
        if (finalCamera)
        {
            finalCamera.Priority = 0;
            finalCamera.gameObject.SetActive(false);
        }
        ResetUI();
    }

    void OnDisable() { StopHaptics(); }

    public void TriggerEndingSequence(float avgReflex, float avgDraw, float totalScore)
    {
        _finalReflex = avgReflex;
        _finalDraw = avgDraw;
        _finalTotal = totalScore;

        IsAnimating = true;
        IsSequenceFinished = false;

        // 1. Maximize Audio Intensity
        if (audioDirector != null)
        {
            audioDirector.SetIntensity(100f);
        }

        // 2. Disable Gameplay Cameras
        if (cameraDirector != null)
        {
            cameraDirector.DisableAllCameras();
        }

        // 3. Activate Final Camera
        if (finalCamera)
        {
            finalCamera.gameObject.SetActive(true);
            finalCamera.Priority = 200;
        }

        // 4. Show Panel
        if (finalScorePanel) finalScorePanel.SetActive(true);
        if (mainCanvasGroup)
        {
            mainCanvasGroup.alpha = 0f;
            mainCanvasGroup.DOFade(1f, fadeInDuration).SetUpdate(true);
        }

        // 5. Build Animation Sequence
        _seq = DOTween.Sequence().SetUpdate(true);
        _seq.AppendInterval(startDelay);

        _seq.Append(CreateStatTween(avgReflexText, avgReflexBackground, "AVG REFLEX: ", avgReflex, valueColor));
        _seq.AppendInterval(delayBetweenStats);

        _seq.Append(CreateStatTween(avgDrawText, avgDrawBackground, "AVG DRAW: ", avgDraw, valueColor));
        _seq.AppendInterval(delayBetweenStats);

        _seq.Append(CreateStatTween(totalScoreText, totalScoreBackground, "TOTAL RATING: ", totalScore, totalScoreColor));

        _seq.AppendCallback(() =>
        {
            CompleteSequence();
        });
    }

    public void SkipAnimation()
    {
        if (!IsAnimating) return;

        _seq.Kill();
        StopHaptics();

        SnapStatToFinal(avgReflexText, avgReflexBackground, "AVG REFLEX: ", _finalReflex, valueColor);
        SnapStatToFinal(avgDrawText, avgDrawBackground, "AVG DRAW: ", _finalDraw, valueColor);
        SnapStatToFinal(totalScoreText, totalScoreBackground, "TOTAL RATING: ", _finalTotal, totalScoreColor);

        CompleteSequence();
    }

    private void CompleteSequence()
    {
        if (!finishSound.IsNull) RuntimeManager.PlayOneShot(finishSound);

        StopHaptics();
        IsAnimating = false;
        IsSequenceFinished = true;

        // Trigger Leaderboard (or fallback to prompt)
        if (leaderboardManager != null)
        {
            DOVirtual.DelayedCall(delayBeforeLeaderboard, () =>
            {
                leaderboardManager.OpenLeaderboard(_finalTotal);
                // We keep the simple prompt hidden as the leaderboard covers it
            }).SetUpdate(true);
        }
        else
        {
            ShowRestartPrompt();
        }
    }

    private Sequence CreateStatTween(TextMeshProUGUI targetText, Image bgImage, string label, float targetValue, Color valColor)
    {
        Sequence s = DOTween.Sequence().SetUpdate(true);

        // A. Filler Image
        if (bgImage != null)
        {
            bgImage.fillAmount = 0f;
            s.AppendCallback(() => PlaySound(fillerSound));
            s.Append(bgImage.DOFillAmount(1f, fillDuration).SetEase(Ease.OutCubic).SetUpdate(true));
        }

        // B. Wait
        s.AppendInterval(delayAfterFill);

        // C. Type Label
        s.AppendCallback(() => PlaySound(typingSound));
        s.Append(DOTween.To(() => "", x => targetText.text = x, label, labelTypingDuration).SetEase(Ease.Linear).SetUpdate(true));

        // D. Count Score
        string hexColor = ColorUtility.ToHtmlStringRGB(valColor);
        int lastSoundMilli = 0;

        s.Append(DOTween.To(() => 0f, x =>
        {
            targetText.text = $"{label}<color=#{hexColor}>{x:F3}s</color>";
            int currentMilli = Mathf.FloorToInt(x * 1000);

            if (currentMilli >= lastSoundMilli + audioTriggerStep)
            {
                PlaySound(countingSound);
                TriggerHaptic();
                lastSoundMilli = currentMilli;
            }
        }, targetValue, countingDuration).SetEase(Ease.OutExpo).SetUpdate(true));

        return s;
    }

    private void SnapStatToFinal(TextMeshProUGUI txt, Image bg, string label, float value, Color color)
    {
        if (bg) bg.fillAmount = 1f;
        string hexColor = ColorUtility.ToHtmlStringRGB(color);
        if (txt) txt.text = $"{label}<color=#{hexColor}>{value:F3}s</color>";
    }

    private void ShowRestartPrompt()
    {
        if (restartPromptGroup)
        {
            restartPromptGroup.DOFade(1f, 1f).SetUpdate(true);
            restartPromptGroup.interactable = true;
            restartPromptGroup.blocksRaycasts = true;
        }
    }

    private void ResetUI()
    {
        if (avgReflexText) avgReflexText.text = "";
        if (avgDrawText) avgDrawText.text = "";
        if (totalScoreText) totalScoreText.text = "";
        if (avgReflexBackground) avgReflexBackground.fillAmount = 0f;
        if (avgDrawBackground) avgDrawBackground.fillAmount = 0f;
        if (totalScoreBackground) totalScoreBackground.fillAmount = 0f;
        if (restartPromptGroup) restartPromptGroup.alpha = 0f;
    }

    void PlaySound(EventReference sound)
    {
        if (!sound.IsNull) RuntimeManager.PlayOneShot(sound, Camera.main.transform.position);
    }

    void TriggerHaptic()
    {
        if (Gamepad.current != null)
        {
            Gamepad.current.SetMotorSpeeds(lowFreqMotor, highFreqMotor);
            DOVirtual.DelayedCall(hapticDuration, StopHaptics).SetUpdate(true);
        }
    }

    void StopHaptics()
    {
        if (Gamepad.current != null) Gamepad.current.SetMotorSpeeds(0f, 0f);
    }
}