using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using FMODUnity;

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

    [Header("--- 3. TRANSITION ---")]
    public float delayBetweenLines = 0.5f;

    [Header("--- FMOD Audio ---")]
    public EventReference startTypingSound;
    public EventReference scoreCountingSound;

    [Tooltip("Step threshold for playing sound. 1 = Every ms. 5 = Every 5ms.")]
    [Range(1, 50)]
    public int audioTriggerStep = 3; // <--- NEW PARAMETER

    [Header("--- Labels ---")]
    public string drawSpeedLabel = "Draw Speed: ";
    public string reflexLabel = "Reflex vs AI: ";
    public string anticipationLabel = "Anticipation (Pre-Shot)";

    // Internal Data
    [HideInInspector] public float aiActionTimestamp = -1f;
    [HideInInspector] public float playerDrawTimestamp = -1f;
    [HideInInspector] public float playerFireTimestamp = -1f;

    void Start()
    {
        if (scorePanel) scorePanel.SetActive(false);
    }

    public void DisplayScore()
    {
        if (scorePanel) scorePanel.SetActive(false);
        if (drawSpeedText) drawSpeedText.text = "";
        if (reflexText) reflexText.text = "";

        // Reset Backgrounds
        if (drawSpeedBackground) drawSpeedBackground.fillAmount = 0f;
        if (reflexBackground) reflexBackground.fillAmount = 0f;

        float executionTime = playerFireTimestamp - playerDrawTimestamp;
        float reflexTime = playerFireTimestamp - aiActionTimestamp;

        Sequence scoreSequence = DOTween.Sequence().SetUpdate(true);

        // --- PHASE 0 ---
        scoreSequence.AppendInterval(delayBeforeScoreAppears);
        scoreSequence.AppendCallback(() => { if (scorePanel) scorePanel.SetActive(true); });

        // --- PHASE 1 ---
        if (drawSpeedText != null)
        {
            scoreSequence.Append(CreateScoreTween(drawSpeedText, drawSpeedBackground, drawSpeedLabel, executionTime, "#FFD700"));
        }

        scoreSequence.AppendInterval(delayBetweenLines);

        // --- PHASE 2 ---
        if (reflexText != null)
        {
            if (aiActionTimestamp <= 0)
            {
                // Anticipation Case
                string fullText = $"<color=grey>{anticipationLabel}</color>";

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
                // Normal Case
                string colorHex = (reflexTime < 0.25f) ? "#00FF00" : "#FFFFFF";
                scoreSequence.Append(CreateScoreTween(reflexText, reflexBackground, reflexLabel, reflexTime, colorHex));
            }
        }
    }

    private Sequence CreateScoreTween(TextMeshProUGUI targetText, Image bgImage, string label, float targetValue, string hexColor)
    {
        Sequence s = DOTween.Sequence().SetUpdate(true);

        // A. LABEL TYPING
        s.AppendCallback(() => PlaySound(startTypingSound));

        s.Append(
            DOTween.To(() => "", x => targetText.text = x, label, labelTypingDuration)
            .SetEase(Ease.Linear)
            .SetUpdate(true)
        );

        // Join Background Fill
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

        // C. COUNTING NUMBERS + STEPPED AUDIO
        // Local variable to track when we last played a sound
        int lastSoundMilli = 0;

        s.Append(
            DOTween.To(() => 0f, x =>
            {
                targetText.text = $"{label}<color={hexColor}>{x:F3}s</color>";

                // --- NEW AUDIO LOGIC ---
                // Convert current float to int milliseconds (0.452 -> 452)
                int currentMilli = Mathf.FloorToInt(x * 1000);

                // We play sound only if the number has increased by 'audioTriggerStep'
                if (currentMilli >= lastSoundMilli + audioTriggerStep)
                {
                    PlaySound(scoreCountingSound);
                    lastSoundMilli = currentMilli; // Reset tracker to current
                }
                // -----------------------

            }, targetValue, scoreCountingDuration)
            .SetEase(Ease.OutExpo)
            .SetUpdate(true)
        );

        return s;
    }

    void PlaySound(EventReference sound)
    {
        if (!sound.IsNull) RuntimeManager.PlayOneShot(sound);
    }
}