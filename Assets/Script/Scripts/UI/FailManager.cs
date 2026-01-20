using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using FMODUnity;

public class FailManager : MonoBehaviour
{
    [Header("--- UI References ---")]
    public GameObject failPanel;

    [Tooltip("Set Image Type to FILLED in Inspector!")]
    public Image backgroundFill;
    public TextMeshProUGUI titleText;

    [Tooltip("Set Image Type to FILLED in Inspector!")]
    public Image decorationImage;
    public TextMeshProUGUI reasonText;

    [Header("--- Timing Settings ---")]
    public float delayBeforeSequence = 0.5f;

    [Header("1. Title Settings")]
    public float imageFillDuration = 0.5f;
    public float delayImageToText = 0.2f;
    public float titleTypingDuration = 0.5f;

    [Header("2. Reason Settings")]
    public float delayBetweenPhases = 0.5f;
    public float reasonTypingDuration = 1.5f;

    [Header("--- Audio ---")]
    public EventReference phase1Sound;
    public EventReference phase2Sound;
    public EventReference typingClickSound;
    public EventReference skipSound; // <--- Son du Skip

    // --- SKIP DATA ---
    public bool IsAnimating { get; private set; } = false;
    private Sequence _currentSeq;
    private string _finalTitle;
    private string _finalReason;

    void Start()
    {
        Hide();
    }

    public void TriggerFailSequence(string titleContent, string reasonContent)
    {
        // 1. Reset
        ResetUIElements();
        if (failPanel) failPanel.SetActive(true);

        // 2. Memorize for Skip
        _finalTitle = titleContent;
        _finalReason = reasonContent;
        IsAnimating = true; // <--- STARTED

        // 3. Create Sequence
        _currentSeq = DOTween.Sequence().SetUpdate(true);

        // --- STEP 0: INITIAL DELAY ---
        _currentSeq.AppendInterval(delayBeforeSequence);

        // --- PHASE 1: TITLE ---
        _currentSeq.AppendCallback(() => PlaySound(phase1Sound));

        if (backgroundFill)
        {
            backgroundFill.fillAmount = 0f;
            _currentSeq.Append(backgroundFill.DOFillAmount(1f, imageFillDuration).SetEase(Ease.OutCubic));
        }

        _currentSeq.AppendInterval(delayImageToText);

        if (titleText)
        {
            AddTypewriterToSequence(_currentSeq, titleText, titleContent, titleTypingDuration);
        }

        // --- WAIT ---
        _currentSeq.AppendInterval(delayBetweenPhases);

        // --- PHASE 2: REASON ---
        _currentSeq.AppendCallback(() => PlaySound(phase2Sound));

        if (decorationImage)
        {
            decorationImage.fillAmount = 0f;
            _currentSeq.Append(decorationImage.DOFillAmount(1f, imageFillDuration).SetEase(Ease.OutCubic));
        }

        _currentSeq.AppendInterval(delayImageToText);

        if (reasonText)
        {
            AddTypewriterToSequence(_currentSeq, reasonText, reasonContent, reasonTypingDuration);
        }

        // --- END ---
        _currentSeq.OnComplete(() => IsAnimating = false);
    }

    // --- SKIP FUNCTION ---
    public void SkipAnimation()
    {
        if (!IsAnimating) return;

        // 1. Kill Sequence
        _currentSeq.Kill();

        // 2. Force Final State
        if (backgroundFill) backgroundFill.fillAmount = 1f;
        if (decorationImage) decorationImage.fillAmount = 1f;

        if (titleText) titleText.text = _finalTitle;
        if (reasonText) reasonText.text = _finalReason;

        // 3. Feedback
        PlaySound(skipSound);

        // 4. Finish
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
            }, content, duration).SetEase(Ease.Linear)
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