using UnityEngine;
using DG.Tweening; // For smooth fade in/out

public class TutorialUIManager : MonoBehaviour
{
    [Header("--- References ---")]
    public DuelController player;

    [Header("--- UI Prompts (Assign GameObjects) ---")]
    public CanvasGroup aimPrompt;  // The "Draw" input UI
    public CanvasGroup loadPrompt; // The "Cock Hammer" input UI
    public CanvasGroup firePrompt; // The "Fire" input UI

    [Header("--- Settings ---")]
    public float fadeSpeed = 0.2f;

    void Start()
    {
        // Initial Reset
        UpdateUIState();
    }

    void Update()
    {
        if (player == null) return;

        // We check the player's state every frame to sync the UI
        UpdateUIState();
    }

    void UpdateUIState()
    {
        DuelState state = player.currentState;

        // 1. STATE: IDLE (Start)
        // Show AIM. Hide others.
        if (state == DuelState.Idle)
        {
            SetVisible(aimPrompt, true);
            SetVisible(loadPrompt, false);
            SetVisible(firePrompt, false);
        }
        // 2. STATE: DRAWING (Player is aiming)
        // Keep AIM visible. Show LOAD.
        else if (state == DuelState.Drawing)
        {
            SetVisible(aimPrompt, true);
            SetVisible(loadPrompt, true);
            SetVisible(firePrompt, false);
        }
        // 3. STATE: COCKED (Hammer is back)
        // Keep AIM & LOAD visible. Show FIRE.
        else if (state == DuelState.Cocked)
        {
            SetVisible(aimPrompt, true);
            SetVisible(loadPrompt, true);
            SetVisible(firePrompt, true);
        }
        // 4. STATE: FIRED / DEAD / FEINTING
        // Hide EVERYTHING.
        else
        {
            SetVisible(aimPrompt, false);
            SetVisible(loadPrompt, false);
            SetVisible(firePrompt, false);
        }
    }

    // Helper to fade UI in/out nicely
    void SetVisible(CanvasGroup group, bool visible)
    {
        if (group == null) return;

        float targetAlpha = visible ? 1f : 0f;

        // Only tween if we aren't already there (optimization)
        if (Mathf.Abs(group.alpha - targetAlpha) > 0.01f)
        {
            // Use DOTween if available, otherwise snap
            group.alpha = Mathf.MoveTowards(group.alpha, targetAlpha, Time.deltaTime / fadeSpeed);
        }
    }
}