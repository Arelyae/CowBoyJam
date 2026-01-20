using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using FMODUnity;

public class FailManager : MonoBehaviour
{
    [Header("--- UI References ---")]
    public GameObject failPanel;

    [Header("--- 1. Screen Overlay (Red Flash) ---")]
    public Image screenOverlay;
    [Range(0f, 1f)] public float overlayMaxAlpha = 0.6f;
    public float overlayFadeDuration = 0.5f;

    [Header("--- 2. Title Section (Top) ---")]
    [Tooltip("Image Type must be set to FILLED in Inspector")]
    public Image backgroundFill;
    public TextMeshProUGUI titleText;

    [Header("--- 3. Reason Section (Bottom) ---")]
    [Tooltip("Image Type must be set to FILLED in Inspector")]
    public Image decorationImage;
    public TextMeshProUGUI reasonText;

    [Header("--- Animation Timings ---")]
    public float delayBeforeSequence = 0.5f;

    [Tooltip("Duration of the Image Fill animation")]
    public float fillDuration = 0.5f;

    public float delayImageToText = 0.2f;
    public float titleTypingDuration = 0.5f;
    public float reasonTypingDuration = 1.5f;
    public float delayBetweenPhases = 0.5f;

    [Header("--- Audio ---")]
    public EventReference phase1Sound;
    public EventReference phase2Sound;
    public EventReference typingClickSound;
    public EventReference skipSound;

    // --- SKIP DATA ---
    public bool IsAnimating { get; private set; } = false;
    private Sequence _currentSeq;
    private string _finalTitle;
    private string _finalReason;
    private bool _shouldShowOverlay; // Internal memory for Skip

    void Start()
    {
        // Safety Checks
        if (backgroundFill && backgroundFill.type != Image.Type.Filled) backgroundFill.type = Image.Type.Filled;
        if (decorationImage && decorationImage.type != Image.Type.Filled) decorationImage.type = Image.Type.Filled;

        Hide();
    }

    // UPDATE: Added 'bool showOverlay' argument
    public void TriggerFailSequence(string titleContent, string reasonContent, bool showOverlay)
    {
        ResetUIElements();
        if (failPanel) failPanel.SetActive(true);

        _finalTitle = titleContent;
        _finalReason = reasonContent;
        _shouldShowOverlay = showOverlay; // Remember for Skip
        IsAnimating = true;

        _currentSeq = DOTween.Sequence().SetUpdate(true);

        // --- STEP 0: IMMEDIATE OVERLAY (CONDITIONAL) ---
        if (screenOverlay)
        {
            // Reset to clear
            Color c = screenOverlay.color;
            c.a = 0f;
            screenOverlay.color = c;

            // Only animate if requested (Death)
            if (showOverlay)
            {
                _currentSeq.Insert(0f, screenOverlay.DOFade(overlayMaxAlpha, overlayFadeDuration)
                    .SetEase(Ease.OutCubic)
                    .SetUpdate(true));
            }
        }

        // --- STEP 1: DELAY ---
        _currentSeq.AppendInterval(delayBeforeSequence);

        // --- PHASE 1: BACKGROUND + TITLE ---
        _currentSeq.AppendCallback(() => PlaySound(phase1Sound));

        if (backgroundFill)
        {
            backgroundFill.fillAmount = 0f;
            _currentSeq.Append(backgroundFill.DOFillAmount(1f, fillDuration)
                .SetEase(Ease.OutCubic)
                .SetUpdate(true));
        }

        _currentSeq.AppendInterval(delayImageToText);

        if (titleText)
        {
            AddTypewriterToSequence(_currentSeq, titleText, titleContent, titleTypingDuration);
        }

        // --- WAIT ---
        _currentSeq.AppendInterval(delayBetweenPhases);

        // --- PHASE 2: DECORATION + REASON ---
        _currentSeq.AppendCallback(() => PlaySound(phase2Sound));

        if (decorationImage)
        {
            decorationImage.fillAmount = 0f;
            _currentSeq.Append(decorationImage.DOFillAmount(1f, fillDuration)
                .SetEase(Ease.OutCubic)
                .SetUpdate(true));
        }

        _currentSeq.AppendInterval(delayImageToText);

        if (reasonText)
        {
            AddTypewriterToSequence(_currentSeq, reasonText, reasonContent, reasonTypingDuration);
        }

        _currentSeq.OnComplete(() => IsAnimating = false);
    }

    public void SkipAnimation()
    {
        if (!IsAnimating) return;

        _currentSeq.Kill();

        // Overlay Skip Logic
        if (screenOverlay)
        {
            Color c = screenOverlay.color;
            c.a = _shouldShowOverlay ? overlayMaxAlpha : 0f; // Respect the boolean
            screenOverlay.color = c;
        }

        if (backgroundFill) backgroundFill.fillAmount = 1f;
        if (decorationImage) decorationImage.fillAmount = 1f;

        if (titleText) titleText.text = _finalTitle;
        if (reasonText) reasonText.text = _finalReason;

        PlaySound(skipSound);
        IsAnimating = false;
    }

    private void AddTypewriterToSequence(Sequence s, TextMeshProUGUI target, string content, float duration)
    {
        int lastLength = 0;
        s.Append(
            DOTween.To(() => "", x =>
            {
                target.text = x;
                if (x.Length > lastLength)
                {
                    PlaySound(typingClickSound);
                    lastLength = x.Length;
                }
            }, content, duration)
            .SetEase(Ease.Linear)
            .SetUpdate(true)
        );
    }

    public void Hide()
    {
        IsAnimating = false;
        if (failPanel) failPanel.SetActive(false);
        ResetUIElements();
    }

    private void ResetUIElements()
    {
        if (screenOverlay)
        {
            Color c = screenOverlay.color;
            c.a = 0f;
            screenOverlay.color = c;
        }

        if (backgroundFill) backgroundFill.fillAmount = 0f;
        if (decorationImage) decorationImage.fillAmount = 0f;

        if (titleText) titleText.text = "";
        if (reasonText) reasonText.text = "";
    }

    void PlaySound(EventReference sound)
    {
        if (!sound.IsNull) RuntimeManager.PlayOneShot(sound, Camera.main.transform.position);
    }
}