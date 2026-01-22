using UnityEngine;
using DG.Tweening;

public class TutorialUIManager : MonoBehaviour
{
    [Header("--- References ---")]
    public DuelController player;

    [Header("--- Main Container ---")]
    [Tooltip("The parent object holding all tutorial prompts. Used to hide everything during Title Screen.")]
    public CanvasGroup mainCanvasGroup; // <--- NEW REFERENCE

    [Header("--- Individual Prompts ---")]
    public CanvasGroup aimPrompt;
    public CanvasGroup loadPrompt;
    public CanvasGroup firePrompt;

    [Header("--- Settings ---")]
    public float fadeSpeed = 0.2f;

    void Start()
    {
        // Initialize hidden if the player is disabled (Title Screen mode)
        if (player != null && !player.enabled)
        {
            if (mainCanvasGroup != null) mainCanvasGroup.alpha = 0f;
        }

        UpdateUIState();
    }

    void Update()
    {
        if (player == null) return;

        // 1. CHECK: IS THE GAME ACTIVE?
        // If the player script is disabled (Title Screen), hide everything.
        if (!player.enabled)
        {
            SetVisible(mainCanvasGroup, false);
            return;
        }

        // 2. SHOW MAIN CONTAINER
        // If player is enabled, make sure the HUD is visible
        SetVisible(mainCanvasGroup, true);

        // 3. UPDATE INDIVIDUAL PROMPTS
        UpdateUIState();
    }

    void UpdateUIState()
    {
        DuelState state = player.currentState;

        // IDLE -> Show AIM
        if (state == DuelState.Idle)
        {
            SetVisible(aimPrompt, true);
            SetVisible(loadPrompt, false);
            SetVisible(firePrompt, false);
        }
        // DRAWING -> Show AIM + LOAD
        else if (state == DuelState.Drawing)
        {
            SetVisible(aimPrompt, true);
            SetVisible(loadPrompt, true);
            SetVisible(firePrompt, false);
        }
        // COCKED -> Show AIM + LOAD + FIRE
        else if (state == DuelState.Cocked)
        {
            SetVisible(aimPrompt, true);
            SetVisible(loadPrompt, true);
            SetVisible(firePrompt, true);
        }
        // FIRED/DEAD -> Hide ALL
        else
        {
            SetVisible(aimPrompt, false);
            SetVisible(loadPrompt, false);
            SetVisible(firePrompt, false);
        }
    }

    void SetVisible(CanvasGroup group, bool visible)
    {
        if (group == null) return;

        float targetAlpha = visible ? 1f : 0f;

        if (Mathf.Abs(group.alpha - targetAlpha) > 0.01f)
        {
            // Simple approach: move towards target
            group.alpha = Mathf.MoveTowards(group.alpha, targetAlpha, Time.deltaTime / fadeSpeed);
        }
    }
}