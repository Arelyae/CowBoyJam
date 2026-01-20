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

    [Header("--- Config ---")]
    public float delayBeforeScoreAppears = 1.5f;
    public float labelTypingDuration = 0.5f;
    public float delayBeforeCounting = 0.2f;
    public float scoreCountingDuration = 1.0f;
    public float delayBetweenLines = 0.5f;

    [Header("--- Colors ---")]
    public Color speedTextColor = new Color(1f, 0.84f, 0f);
    public Color reflexNormalColor = Color.white;
    public Color reflexFastColor = Color.green;
    public Color anticipationTextColor = Color.grey;
    public float fastReflexThreshold = 0.25f;

    [Header("--- Audio / Haptics ---")]
    public EventReference startTypingSound;
    public EventReference scoreCountingSound;
    public EventReference skipSound; // <--- NOUVEAU : Son unique du Skip

    [Range(1, 50)] public int audioTriggerStep = 3;
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

    // --- SKIP SYSTEM VARIABLES ---
    public bool IsAnimating { get; private set; } = false;
    private Sequence _currentSeq;

    // On mémorise ce qu'on doit afficher à la fin pour le Skip instantané
    private string _finalDrawText;
    private string _finalReflexText;

    void Start() { if (scorePanel) scorePanel.SetActive(false); }
    void OnDisable() { StopHaptics(); }

    public void ResetScore()
    {
        IsAnimating = false;
        if (scorePanel) scorePanel.SetActive(false);

        ResetScoreUIOnly();

        aiActionTimestamp = -1f;
        playerDrawTimestamp = -1f;
        playerFireTimestamp = -1f;

        _currentSeq.Kill(); // On tue la séquence si elle existe
        this.transform.DOKill();
        if (drawSpeedText) drawSpeedText.DOKill();
        if (reflexText) reflexText.DOKill();

        StopHaptics();
    }

    public void DisplayScore()
    {
        if (scorePanel) scorePanel.SetActive(false);
        ResetScoreUIOnly();

        IsAnimating = true;

        // --- 1. PRÉ-CALCUL DES RÉSULTATS ---
        float executionTime = playerFireTimestamp - playerDrawTimestamp;

        // Calcul du temps de réflexe
        float reflexTime = playerDrawTimestamp - aiActionTimestamp;

        // Calcul Draw Text Final
        string hexSpeed = ColorToHex(speedTextColor);
        _finalDrawText = $"{drawSpeedLabel}<color=#{hexSpeed}>{executionTime:F3}s</color>";

        // --- CORRECTION ICI : GESTION DU SCORE NÉGATIF ---
        // Si l'IA n'a pas bougé (Timestamp <= 0) OU si le joueur a réagi AVANT l'IA (reflexTime < 0)
        if (aiActionTimestamp <= 0 || reflexTime < 0)
        {
            // Cas Anticipation (Texte Gris)
            string hexAntic = ColorToHex(anticipationTextColor);
            _finalReflexText = $"<color=#{hexAntic}>{anticipationLabel}</color>";
        }
        else
        {
            // Cas Normal (Temps Positif)
            Color c = (reflexTime < fastReflexThreshold) ? reflexFastColor : reflexNormalColor;
            string hexReflex = ColorToHex(c);
            _finalReflexText = $"{reflexLabel}<color=#{hexReflex}>{reflexTime:F3}s</color>";
        }
        // -------------------------------------------------

        // --- 2. CRÉATION SÉQUENCE ---
        _currentSeq = DOTween.Sequence().SetUpdate(true);

        // Phase 0: Wait
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
            // --- CORRECTION ICI AUSSI (Même condition) ---
            if (aiActionTimestamp <= 0 || reflexTime < 0)
            {
                // Animation Anticipation (Pas de compteur, juste le texte qui apparait)
                _currentSeq.AppendCallback(() => PlaySound(startTypingSound));
                if (reflexBackground) _currentSeq.Join(DOTween.To(() => 0f, x => reflexBackground.fillAmount = x, 1f, labelTypingDuration).SetEase(Ease.Linear).SetUpdate(true));
                _currentSeq.Append(DOTween.To(() => "", x => reflexText.text = x, _finalReflexText, labelTypingDuration).SetOptions(true, ScrambleMode.None).SetUpdate(true));
            }
            else
            {
                // Animation Normale (Compteur de chiffres)
                Color c = (reflexTime < fastReflexThreshold) ? reflexFastColor : reflexNormalColor;
                _currentSeq.Append(CreateScoreTween(reflexText, reflexBackground, reflexLabel, reflexTime, ColorToHex(c)));
            }
            // ---------------------------------------------
        }

        // FIN ANIMATION
        _currentSeq.OnComplete(() =>
        {
            IsAnimating = false;
            StopHaptics();
        });
    }

    // --- FONCTION SKIP ---
    public void SkipAnimation()
    {
        if (!IsAnimating) return;

        // 1. Tuer l'animation en cours
        _currentSeq.Kill();
        StopHaptics();

        // 2. Forcer l'affichage final (Instantané)
        if (scorePanel) scorePanel.SetActive(true);

        if (drawSpeedText) drawSpeedText.text = _finalDrawText;
        if (drawSpeedBackground) drawSpeedBackground.fillAmount = 1f;

        if (reflexText) reflexText.text = _finalReflexText;
        if (reflexBackground) reflexBackground.fillAmount = 1f;

        // 3. Son de confirmation unique
        PlaySound(skipSound.IsNull ? scoreCountingSound : skipSound); // Fallback si pas de son skip

        // 4. Vibration unique "Toc"
        TriggerHaptic();

        // 5. C'est fini
        IsAnimating = false;
    }

    // --- UTILITAIRES ---
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
        // Label Typing
        s.AppendCallback(() => PlaySound(startTypingSound));
        s.Append(DOTween.To(() => "", x => targetText.text = x, label, labelTypingDuration).SetEase(Ease.Linear).SetUpdate(true));
        if (bgImage != null) s.Join(DOTween.To(() => 0f, x => bgImage.fillAmount = x, 1f, labelTypingDuration).SetEase(Ease.OutQuad).SetUpdate(true));

        if (delayBeforeCounting > 0) s.AppendInterval(delayBeforeCounting);

        // Counting
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