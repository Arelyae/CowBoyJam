using UnityEngine;

public class DuelAnimationRelay : MonoBehaviour
{
    [Header("--- Connection ---")]
    [Tooltip("Drag the main DuelController script here (Parent or Manager)")]
    public DuelController mainController;

    // --- ANIMATION EVENTS ---

    // This is the function name you type in the Animation Window
    public void SpawnShotEffects()
    {
        if (mainController != null)
        {
            mainController.SpawnShotEffects();
        }
        else
        {
            Debug.LogWarning("Animation Relay: No DuelController assigned!");
        }
    }

    // You can add more events here later (e.g., Footsteps, Reload Sounds)
    public void PlayFootstep()
    {
        // Example: if (mainController) mainController.PlayStepSound();
    }
}