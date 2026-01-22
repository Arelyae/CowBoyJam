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
    public TextMeshProUGUI drawSpeedText;
    public Image drawSpeedBackground;
    public TextMeshProUGUI reflexText;
    public Image reflexBackground;

    [Header("--- Navigation Prompts ---")]
    [Tooltip("The parent object holding your Prompt Images (e.g. 'Press [Space] to Continue').")]
    public CanvasGroup navPromptsGroup;

    [Header("--- Config ---")]
    public float delayBeforeScoreAppears = 1.5f;
    public float labelTypingDuration = 0.5f;
    public float delayBeforeCounting = 0.2f;
    public float scoreCountingDuration = 1.0f;
    public float delayBetweenLines = 0.5f;
    public float promptsFadeInDuration = 0.5f;

    [Header("--- Colors ---")]
    public Color speedTextColor = new Color(1f, 0.84f, 0f);
    public Color reflexNormalColor = Color.white;
    public Color reflexFastColor = Color.green;
    public Color anticipationTextColor = Color.grey;
    public float fastReflexThreshold = 0.25f;

    [Header("--- Audio / Haptics ---")]
    public EventReference startTypingSound;
    public EventReference scoreCountingSound;
    public EventReference skipSound;

    [Range(1, 50)] public int audioTriggerStep = 3;
    [Range(0f, 1f)] public float lowFreqMotor = 0.5f;
    [Range(0f, 1f)] public float highFreqMotor = 0.1f;
    public float hapticDuration = 0.05f;

    [Header("--- Labels ---")]
    public string drawSpeedLabel = "Draw Speed: ";
    public string reflexLabel = "Reflex vs IA: ";
    public string anticipationLabel = "Anticipation (Pre-Shot)";

    // Internal Data
    public bool IsAnimating { get; private set; } = false;

    // NEW: Public flag for GameProgressionManager
    public bool AreInputsActive { get; private set; } = false;

    private Sequence _currentSeq;
    private string _finalDrawText;
    private string _finalReflexText;

    // Data passing
    [HideInInspector] public float aiActionTimestamp = -1f;
    [HideInInspector] public float playerDrawTimestamp = -1f;
    [HideInInspector] public float playerFireTimestamp = -1f;

    void Start()
    {
        if (scorePanel) scorePanel.SetActive(false);
        ResetPromptsUI();
    }

    void OnDisable() { StopHaptics(); }

    public void ResetScore()
    {
        IsAnimating = false;
        AreInputsActive = false; // Disable Inputs

        if (scorePanel) scorePanel.SetActive(false);

        ResetScoreUIOnly();
        ResetPromptsUI();

        aiActionTimestamp = -1f;
        playerDrawTimestamp = -1f;
        playerFireTimestamp = -1f;

        _currentSeq.Kill();
        this.transform.DOKill();
        if (drawSpeedText) drawSpeedText.DOKill();
        if (reflexText) reflexText.DOKill();
        if (navPromptsGroup) navPromptsGroup.DOKill();

        StopHaptics();
    }

    public void DisplayScore()
    {
        if (scorePanel) scorePanel.SetActive(false);
        ResetScoreUIOnly();
        ResetPromptsUI();

        IsAnimating = true;
        AreInputsActive = false;

        // --- 1. CALCULATION LOGIC ---
        float executionTime = playerFireTimestamp - playerDrawTimestamp;
        float reflexTime = playerDrawTimestamp - aiActionTimestamp;

        string hexSpeed = ColorToHex(speedTextColor);
        _finalDrawText = $"{drawSpeedLabel}<color=#{hexSpeed}>{executionTime:F3}s</color>";

        if (aiActionTimestamp <= 0 || reflexTime < 0)
        {
            string hexAntic = ColorToHex(anticipationTextColor);
            _finalReflexText = $"<color=#{hexAntic}>{anticipationLabel}</color>";
        }
        else
        {
            Color c = (reflexTime < fastReflexThreshold) ? reflexFastColor : reflexNormalColor;
            string hexReflex = ColorToHex(c);
            _finalReflexText = $"{reflexLabel}<color=#{hexReflex}>{reflexTime:F3}s</color>";
        }

        // --- 2. ANIMATION SEQUENCE ---
        _currentSeq = DOTween.Sequence().SetUpdate(true);
        _currentSeq.AppendInterval(delayBeforeScoreAppears);
        _currentSeq.AppendCallback(() => { if (scorePanel) scorePanel.SetActive(true); });

        // Phase 1: Draw Speed
        if (drawSpeedText != null)
        {
            _currentSeq.Append(CreateScoreTween(drawSpeedText, drawSpeedBackground, drawSpeedLabel, executionTime, hexSpeed));
        }

        _currentSeq.AppendInterval(delayBetweenLines);

        // Phase 2: Reflex
        if (reflexText != null)
        {
            if (aiActionTimestamp <= 0 || reflexTime < 0)
            {
                _currentSeq.AppendCallback(() => PlaySound(startTypingSound));
                if (reflexBackground) _currentSeq.Join(DOTween.To(() => 0f, x => reflexBackground.fillAmount = x, 1f, labelTypingDuration).SetEase(Ease.Linear).SetUpdate(true));
                _currentSeq.Append(DOTween.To(() => "", x => reflexText.text = x, _finalReflexText, labelTypingDuration).SetOptions(true, ScrambleMode.None).SetUpdate(true));
            }
            else
            {
                Color c = (reflexTime < fastReflexThreshold) ? reflexFastColor : reflexNormalColor;
                _currentSeq.Append(CreateScoreTween(reflexText, reflexBackground, reflexLabel, reflexTime, ColorToHex(c)));
            }
        }

        // --- 3. SHOW PROMPTS ---
        if (navPromptsGroup != null)
        {
            _currentSeq.AppendInterval(0.2f);
            _currentSeq.Append(navPromptsGroup.DOFade(1f, promptsFadeInDuration).SetUpdate(true));
            _currentSeq.AppendCallback(() =>
            {
                // We don't enable 'interactable' because they aren't buttons
                AreInputsActive = true; // Signal GameProgressionManager to listen
            });
        }

        _currentSeq.OnComplete(() =>
        {
            IsAnimating = false;
            StopHaptics();
        });
    }

    public void SkipAnimation()
    {
        if (!IsAnimating) return;

        _currentSeq.Kill();
        StopHaptics();

        if (scorePanel) scorePanel.SetActive(true);

        // Instant Fill UI
        if (drawSpeedText) drawSpeedText.text = _finalDrawText;
        if (drawSpeedBackground) drawSpeedBackground.fillAmount = 1f;

        if (reflexText) reflexText.text = _finalReflexText;
        if (reflexBackground) reflexBackground.fillAmount = 1f;

        // --- SHOW PROMPTS IMMEDIATELY ---
        if (navPromptsGroup != null)
        {
            navPromptsGroup.alpha = 1f;
            AreInputsActive = true;
        }

        PlaySound(skipSound.IsNull ? scoreCountingSound : skipSound);
        TriggerHaptic();

        IsAnimating = false;
    }

    // --- UTILITIES ---
    private void ResetScoreUIOnly()
    {
        if (drawSpeedText) drawSpeedText.text = "";
        if (reflexText) reflexText.text = "";
        if (drawSpeedBackground) drawSpeedBackground.fillAmount = 0f;
        if (reflexBackground) reflexBackground.fillAmount = 0f;
    }

    private void ResetPromptsUI()
    {
        AreInputsActive = false;
        if (navPromptsGroup != null)
        {
            navPromptsGroup.alpha = 0f;
        }
    }

    private Sequence CreateScoreTween(TextMeshProUGUI targetText, Image bgImage, string label, float targetValue, string hexColor)
    {
        Sequence s = DOTween.Sequence().SetUpdate(true);
        s.AppendCallback(() => PlaySound(startTypingSound));
        s.Append(DOTween.To(() => "", x => targetText.text = x, label, labelTypingDuration).SetEase(Ease.Linear).SetUpdate(true));
        if (bgImage != null) s.Join(DOTween.To(() => 0f, x => bgImage.fillAmount = x, 1f, labelTypingDuration).SetEase(Ease.OutQuad).SetUpdate(true));

        if (delayBeforeCounting > 0) s.AppendInterval(delayBeforeCounting);

        int lastSoundMilli = 0;
        s.Append(DOTween.To(() => 0f, x =>
        {
            targetText.text = $"{label}<color=#{hexColor}>{x:F3}s</color>";
            int currentMilli = Mathf.FloorToInt(x * 1000);
            if (currentMilli >= lastSoundMilli + audioTriggerStep)
            {
                PlaySound(scoreCountingSound);
                TriggerHaptic();
                lastSoundMilli = currentMilli;
            }
        }, targetValue, scoreCountingDuration).SetEase(Ease.OutExpo).SetUpdate(true));

        return s;
    }

    private string ColorToHex(Color color) { return ColorUtility.ToHtmlStringRGB(color); }
    void PlaySound(EventReference sound) { if (!sound.IsNull) RuntimeManager.PlayOneShot(sound); }

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