using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using FMODUnity;

public class FailManager : MonoBehaviour
{
    [Header("--- UI References ---")]
    public GameObject failPanel;

    // NEW: The "Press R to Restart" container
    [Header("--- 4. Restart Prompt (Appears at End) ---")]
    public CanvasGroup restartPromptGroup;
    public float promptFadeInDuration = 0.5f;

    [Header("--- 1. Screen Overlay (Red Flash) ---")]
    public Image screenOverlay;
    [Range(0f, 1f)] public float overlayMaxAlpha = 0.6f;
    public float overlayFadeDuration = 0.5f;

    [Header("--- 2. Title Section (Top) ---")]
    public Image backgroundFill;
    public TextMeshProUGUI titleText;

    [Header("--- 3. Reason Section (Bottom) ---")]
    public Image decorationImage;
    public TextMeshProUGUI reasonText;

    [Header("--- Animation Timings ---")]
    public float delayBeforeSequence = 0.5f;
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

    // --- STATE DATA ---
    public bool IsAnimating { get; private set; } = false;
    public bool IsActive { get; private set; } = false;

    private Sequence _currentSeq;
    private Tween _promptLoopTween; // Separate tween for the pulsing effect
    private string _finalTitle;
    private string _finalReason;
    private bool _shouldShowOverlay;

    void Start()
    {
        if (backgroundFill && backgroundFill.type != Image.Type.Filled) backgroundFill.type = Image.Type.Filled;
        if (decorationImage && decorationImage.type != Image.Type.Filled) decorationImage.type = Image.Type.Filled;
        Hide();
    }

    public void TriggerFailSequence(string titleContent, string reasonContent, bool showOverlay)
    {
        ResetUIElements();
        if (failPanel) failPanel.SetActive(true);

        _finalTitle = titleContent;
        _finalReason = reasonContent;
        _shouldShowOverlay = showOverlay;

        IsAnimating = true;
        IsActive = true;

        _currentSeq = DOTween.Sequence().SetUpdate(true);

        // --- STEP 0: OVERLAY ---
        if (screenOverlay)
        {
            Color c = screenOverlay.color;
            c.a = 0f;
            screenOverlay.color = c;
            if (showOverlay)
            {
                _currentSeq.Insert(0f, screenOverlay.DOFade(overlayMaxAlpha, overlayFadeDuration)
                    .SetEase(Ease.OutCubic).SetUpdate(true));
            }
        }

        // --- STEP 1: MAIN SEQUENCE ---
        _currentSeq.AppendInterval(delayBeforeSequence);

        // Phase 1: Title
        _currentSeq.AppendCallback(() => PlaySound(phase1Sound));
        if (backgroundFill)
        {
            backgroundFill.fillAmount = 0f;
            _currentSeq.Append(backgroundFill.DOFillAmount(1f, fillDuration).SetEase(Ease.OutCubic).SetUpdate(true));
        }
        _currentSeq.AppendInterval(delayImageToText);
        if (titleText) AddTypewriterToSequence(_currentSeq, titleText, titleContent, titleTypingDuration);

        // Phase 2: Reason
        _currentSeq.AppendInterval(delayBetweenPhases);
        _currentSeq.AppendCallback(() => PlaySound(phase2Sound));
        if (decorationImage)
        {
            decorationImage.fillAmount = 0f;
            _currentSeq.Append(decorationImage.DOFillAmount(1f, fillDuration).SetEase(Ease.OutCubic).SetUpdate(true));
        }
        _currentSeq.AppendInterval(delayImageToText);
        if (reasonText) AddTypewriterToSequence(_currentSeq, reasonText, reasonContent, reasonTypingDuration);

        // --- STEP 3: RESTART PROMPT (At the very end) ---
        _currentSeq.AppendCallback(() =>
        {
            ShowRestartPrompt();
            IsAnimating = false; // Sequence is done, player can now reset
        });
    }

    // --- SEPARATE FUNCTION FOR THE PROMPT ---
    private void ShowRestartPrompt()
    {
        if (restartPromptGroup == null) return;

        // 1. Fade In
        restartPromptGroup.DOFade(1f, promptFadeInDuration).SetUpdate(true);

        // 2. Start Pulsing Loop (Separate Tween)
        // This ensures the pulsing continues even after the main sequence dies
        _promptLoopTween = restartPromptGroup.transform.DOScale(1.05f, 0.8f)
            .SetLoops(-1, LoopType.Yoyo)
            .SetEase(Ease.InOutSine)
            .SetUpdate(true);
    }

    public void SkipAnimation()
    {
        if (!IsAnimating) return;

        _currentSeq.Kill();

        if (screenOverlay)
        {
            Color c = screenOverlay.color;
            c.a = _shouldShowOverlay ? overlayMaxAlpha : 0f;
            screenOverlay.color = c;
        }

        if (backgroundFill) backgroundFill.fillAmount = 1f;
        if (decorationImage) decorationImage.fillAmount = 1f;
        if (titleText) titleText.text = _finalTitle;
        if (reasonText) reasonText.text = _finalReason;

        // Force Show Prompt Immediately
        ShowRestartPrompt();

        PlaySound(skipSound);
        IsAnimating = false;
    }

    public void Hide()
    {
        _currentSeq.Kill();
        if (_promptLoopTween != null) _promptLoopTween.Kill();

        IsAnimating = false;
        IsActive = false;

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

        // Reset Prompt
        if (restartPromptGroup)
        {
            restartPromptGroup.alpha = 0f; // Hidden by default
            restartPromptGroup.transform.localScale = Vector3.one;
        }
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

    void PlaySound(EventReference sound)
    {
        if (!sound.IsNull) RuntimeManager.PlayOneShot(sound, Camera.main.transform.position);
    }
}