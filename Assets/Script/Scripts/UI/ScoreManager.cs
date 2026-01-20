using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using FMODUnity;
using UnityEngine.InputSystem;

public class ScoreManager : MonoBehaviour
{
    [Header("--- UI References ---")]
    public GameObject scorePanel;

    [Header("Line 1: Speed")]
    public TextMeshProUGUI drawSpeedText;
    public Image drawSpeedBackground;

    [Header("Line 2: Reflex")]
    public TextMeshProUGUI reflexText;
    public Image reflexBackground;

    [Header("--- 1. INITIALIZATION ---")]
    public float delayBeforeScoreAppears = 1.5f;

    [Header("--- 2. ANIMATION TIMERS ---")]
    public float labelTypingDuration = 0.5f;
    public float delayBeforeCounting = 0.2f;
    public float scoreCountingDuration = 1.0f;

    [Header("--- 3. COLORS (Inspector Control) ---")]
    public Color speedTextColor = new Color(1f, 0.84f, 0f); // Gold default
    public Color reflexNormalColor = Color.white;
    public Color reflexFastColor = Color.green;
    public Color anticipationTextColor = Color.grey;

    [Tooltip("If reflex is faster than this (seconds), use the Fast Color.")]
    public float fastReflexThreshold = 0.25f;

    [Header("--- 4. TRANSITION ---")]
    public float delayBetweenLines = 0.5f;

    [Header("--- FMOD Audio ---")]
    public EventReference startTypingSound;
    public EventReference scoreCountingSound;

    [Tooltip("Step threshold for playing sound/haptics. 1 = Every ms. 10 = Every 10ms.")]
    [Range(1, 50)]
    public int audioTriggerStep = 3;

    [Header("--- Haptics (Vibration) ---")]
    [Range(0f, 1f)] public float lowFreqMotor = 0.5f;
    [Range(0f, 1f)] public float highFreqMotor = 0.1f;
    public float hapticDuration = 0.05f;

    [Header("--- Labels ---")]
    public string drawSpeedLabel = "Draw Speed: ";
    public string reflexLabel = "Reflex vs IA: ";
    public string anticipationLabel = "Anticipation (Pre-Shot)";

    // Internal Data
    [HideInInspector] public float aiActionTimestamp = -1f;
    [HideInInspector] public float playerDrawTimestamp = -1f;
    [HideInInspector] public float playerFireTimestamp = -1f;

    void Start()
    {
        if (scorePanel) scorePanel.SetActive(false);
    }

    void OnDisable()
    {
        StopHaptics();
    }

    public void ResetScore()
    {
        if (scorePanel) scorePanel.SetActive(false);

        if (drawSpeedText) drawSpeedText.text = "";
        if (reflexText) reflexText.text = "";

        if (drawSpeedBackground) drawSpeedBackground.fillAmount = 0f;
        if (reflexBackground) reflexBackground.fillAmount = 0f;

        aiActionTimestamp = -1f;
        playerDrawTimestamp = -1f;
        playerFireTimestamp = -1f;

        this.transform.DOKill();
        if (drawSpeedText) drawSpeedText.DOKill();
        if (reflexText) reflexText.DOKill();

        StopHaptics();
    }

    public void DisplayScore()
    {
        if (scorePanel) scorePanel.SetActive(false);
        ResetScoreUIOnly();

        float executionTime = playerFireTimestamp - playerDrawTimestamp;
        float reflexTime = playerFireTimestamp - aiActionTimestamp;

        Sequence scoreSequence = DOTween.Sequence().SetUpdate(true);

        // --- PHASE 0 ---
        scoreSequence.AppendInterval(delayBeforeScoreAppears);
        scoreSequence.AppendCallback(() => { if (scorePanel) scorePanel.SetActive(true); });

        // --- PHASE 1: EXECUTION SPEED ---
        if (drawSpeedText != null)
        {
            string hexColor = ColorToHex(speedTextColor);
            scoreSequence.Append(CreateScoreTween(drawSpeedText, drawSpeedBackground, drawSpeedLabel, executionTime, hexColor));
        }

        scoreSequence.AppendInterval(delayBetweenLines);

        // --- PHASE 2: REFLEX ---
        if (reflexText != null)
        {
            if (aiActionTimestamp <= 0)
            {
                // Anticipation (Player shot before AI moved)
                string hexColor = ColorToHex(anticipationTextColor);
                string fullText = $"<color=#{hexColor}>{anticipationLabel}</color>";

                scoreSequence.AppendCallback(() => PlaySound(startTypingSound));

                if (reflexBackground != null)
                {
                    DOTween.To(() => 0f, x => reflexBackground.fillAmount = x, 1f, labelTypingDuration)
                        .SetEase(Ease.Linear).SetUpdate(true);
                }

                scoreSequence.Append(
                    DOTween.To(() => "", x => reflexText.text = x, fullText, labelTypingDuration)
                    .SetOptions(true, ScrambleMode.None)
                    .SetEase(Ease.Linear)
                    .SetUpdate(true)
                );
            }
            else
            {
                // Normal Reflex
                Color chosenColor = (reflexTime < fastReflexThreshold) ? reflexFastColor : reflexNormalColor;
                string hexColor = ColorToHex(chosenColor);

                scoreSequence.Append(CreateScoreTween(reflexText, reflexBackground, reflexLabel, reflexTime, hexColor));
            }
        }

        scoreSequence.OnComplete(() => StopHaptics());
    }

    private void ResetScoreUIOnly()
    {
        if (drawSpeedText) drawSpeedText.text = "";
        if (reflexText) reflexText.text = "";
        if (drawSpeedBackground) drawSpeedBackground.fillAmount = 0f;
        if (reflexBackground) reflexBackground.fillAmount = 0f;
    }

    private Sequence CreateScoreTween(TextMeshProUGUI targetText, Image bgImage, string label, float targetValue, string hexColor)
    {
        Sequence s = DOTween.Sequence().SetUpdate(true);

        // A. LABEL
        s.AppendCallback(() => PlaySound(startTypingSound));
        s.Append(
            DOTween.To(() => "", x => targetText.text = x, label, labelTypingDuration)
            .SetEase(Ease.Linear)
            .SetUpdate(true)
        );

        // BG Fill
        if (bgImage != null)
        {
            bgImage.fillAmount = 0f;
            s.Join(
                DOTween.To(() => 0f, x => bgImage.fillAmount = x, 1f, labelTypingDuration)
                .SetEase(Ease.OutQuad)
                .SetUpdate(true)
            );
        }

        // B. PAUSE
        if (delayBeforeCounting > 0) s.AppendInterval(delayBeforeCounting);

        // C. COUNTING + HAPTICS
        int lastSoundMilli = 0;

        s.Append(
            DOTween.To(() => 0f, x =>
            {
                // We inject the Hex Color here
                targetText.text = $"{label}<color=#{hexColor}>{x:F3}s</color>";

                int currentMilli = Mathf.FloorToInt(x * 1000);
                if (currentMilli >= lastSoundMilli + audioTriggerStep)
                {
                    PlaySound(scoreCountingSound);
                    TriggerHaptic();
                    lastSoundMilli = currentMilli;
                }

            }, targetValue, scoreCountingDuration)
            .SetEase(Ease.OutExpo)
            .SetUpdate(true)
        );

        s.AppendCallback(() => StopHaptics());
        return s;
    }

    // Helper to convert Unity Color to Hex String for TextMeshPro
    private string ColorToHex(Color color)
    {
        return ColorUtility.ToHtmlStringRGB(color);
    }

    void PlaySound(EventReference sound)
    {
        if (!sound.IsNull) RuntimeManager.PlayOneShot(sound);
    }

    // --- HAPTIC SYSTEM ---
    void TriggerHaptic()
    {
        if (Gamepad.current == null) return;
        Gamepad.current.SetMotorSpeeds(lowFreqMotor, highFreqMotor);
        DOVirtual.DelayedCall(hapticDuration, StopHaptics).SetUpdate(true);
    }

    void StopHaptics()
    {
        if (Gamepad.current == null) return;
        Gamepad.current.SetMotorSpeeds(0f, 0f);
    }
}