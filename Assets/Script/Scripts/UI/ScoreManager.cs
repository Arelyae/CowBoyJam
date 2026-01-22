using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using FMODUnity;
using UnityEngine.InputSystem;

public enum NavigationAction { Retry, Continue, Restart }

public class ScoreManager : MonoBehaviour
{
    [Header("--- UI References ---")]
    public GameObject scorePanel;
    public TextMeshProUGUI drawSpeedText;
    public Image drawSpeedBackground;
    public TextMeshProUGUI reflexText;
    public Image reflexBackground;

    [Header("--- Navigation Prompts ---")]
    [Tooltip("The parent holding ALL prompts (used for main fade in).")]
    public CanvasGroup navPromptsContainer;

    [Header("--- Individual Prompt Groups ---")]
    public CanvasGroup retryPrompt;
    public CanvasGroup continuePrompt;
    public CanvasGroup restartPrompt;

    [Header("--- Config ---")]
    public float delayBeforeScoreAppears = 1.5f;
    public float labelTypingDuration = 0.5f;

    // --- VARIABLE MANQUANTE RÉINTÉGRÉE ---
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

    [Header("--- Audio ---")]
    public EventReference startTypingSound;
    public EventReference scoreCountingSound;
    public EventReference skipSound;
    public EventReference confirmSelectionSound;

    [Range(1, 50)] public int audioTriggerStep = 3;
    [Range(0f, 1f)] public float lowFreqMotor = 0.5f;
    [Range(0f, 1f)] public float highFreqMotor = 0.1f;
    public float hapticDuration = 0.05f;

    [Header("--- Labels ---")]
    public string drawSpeedLabel = "Draw Speed: ";
    public string reflexLabel = "Reflex vs IA: ";
    public string anticipationLabel = "Anticipation (Pre-Shot)";

    // Internal State
    public bool IsAnimating { get; private set; } = false;
    public bool AreInputsActive { get; private set; } = false;

    private Sequence _currentSeq;
    private string _finalDrawText;
    private string _finalReflexText;

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
        AreInputsActive = false;
        if (scorePanel) scorePanel.SetActive(false);

        ResetScoreUIOnly();
        ResetPromptsUI();

        _currentSeq.Kill();
        this.transform.DOKill();
        StopHaptics();
    }

    public void DisplayScore()
    {
        if (scorePanel) scorePanel.SetActive(false);
        ResetScoreUIOnly();
        ResetPromptsUI();

        IsAnimating = true;
        AreInputsActive = false;

        float executionTime = playerFireTimestamp - playerDrawTimestamp;
        float reflexTime = playerDrawTimestamp - aiActionTimestamp;

        string hexSpeed = ColorUtility.ToHtmlStringRGB(speedTextColor);
        _finalDrawText = $"{drawSpeedLabel}<color=#{hexSpeed}>{executionTime:F3}s</color>";

        if (aiActionTimestamp <= 0 || reflexTime < 0)
        {
            string hexAntic = ColorUtility.ToHtmlStringRGB(anticipationTextColor);
            _finalReflexText = $"<color=#{hexAntic}>{anticipationLabel}</color>";
        }
        else
        {
            Color c = (reflexTime < fastReflexThreshold) ? reflexFastColor : reflexNormalColor;
            string hexReflex = ColorUtility.ToHtmlStringRGB(c);
            _finalReflexText = $"{reflexLabel}<color=#{hexReflex}>{reflexTime:F3}s</color>";
        }

        _currentSeq = DOTween.Sequence().SetUpdate(true);
        _currentSeq.AppendInterval(delayBeforeScoreAppears);
        _currentSeq.AppendCallback(() => { if (scorePanel) scorePanel.SetActive(true); });

        if (drawSpeedText != null) _currentSeq.Append(CreateScoreTween(drawSpeedText, drawSpeedBackground, drawSpeedLabel, executionTime, hexSpeed));
        _currentSeq.AppendInterval(delayBetweenLines);

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
                _currentSeq.Append(CreateScoreTween(reflexText, reflexBackground, reflexLabel, reflexTime, ColorUtility.ToHtmlStringRGB(c)));
            }
        }

        if (navPromptsContainer != null)
        {
            _currentSeq.AppendInterval(0.2f);
            _currentSeq.Append(navPromptsContainer.DOFade(1f, promptsFadeInDuration).SetUpdate(true));
            _currentSeq.AppendCallback(() => AreInputsActive = true);
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
        if (drawSpeedText) drawSpeedText.text = _finalDrawText;
        if (reflexText) reflexText.text = _finalReflexText;
        if (drawSpeedBackground) drawSpeedBackground.fillAmount = 1f;
        if (reflexBackground) reflexBackground.fillAmount = 1f;

        if (navPromptsContainer != null)
        {
            navPromptsContainer.alpha = 1f;
            AreInputsActive = true;
        }

        PlaySound(skipSound.IsNull ? scoreCountingSound : skipSound);
        TriggerHaptic();
        IsAnimating = false;
    }

    public void HighlightSelection(NavigationAction selectedAction)
    {
        AreInputsActive = false;
        _currentSeq.Kill();

        PlaySound(confirmSelectionSound);

        CanvasGroup target = null;
        CanvasGroup[] others = null;

        switch (selectedAction)
        {
            case NavigationAction.Retry:
                target = retryPrompt;
                others = new CanvasGroup[] { continuePrompt, restartPrompt };
                break;
            case NavigationAction.Continue:
                target = continuePrompt;
                others = new CanvasGroup[] { retryPrompt, restartPrompt };
                break;
            case NavigationAction.Restart:
                target = restartPrompt;
                others = new CanvasGroup[] { retryPrompt, continuePrompt };
                break;
        }

        foreach (var other in others)
        {
            if (other != null) other.DOFade(0f, 0.15f).SetUpdate(true);
        }

        if (target != null)
        {
            target.alpha = 1f;
            target.transform.DOScale(1.15f, 0.2f).SetLoops(2, LoopType.Yoyo).SetUpdate(true);
        }
    }

    private void ResetPromptsUI()
    {
        AreInputsActive = false;

        if (navPromptsContainer != null) navPromptsContainer.alpha = 0f;

        ResetSinglePrompt(retryPrompt);
        ResetSinglePrompt(continuePrompt);
        ResetSinglePrompt(restartPrompt);
    }

    private void ResetSinglePrompt(CanvasGroup g)
    {
        if (g != null)
        {
            g.alpha = 1f;
            g.transform.localScale = Vector3.one;
        }
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

    void PlaySound(EventReference sound) { if (!sound.IsNull) RuntimeManager.PlayOneShot(sound); }
    void TriggerHaptic() { if (Gamepad.current != null) { Gamepad.current.SetMotorSpeeds(lowFreqMotor, highFreqMotor); DOVirtual.DelayedCall(hapticDuration, StopHaptics).SetUpdate(true); } }
    void StopHaptics() { if (Gamepad.current != null) Gamepad.current.SetMotorSpeeds(0f, 0f); }
}