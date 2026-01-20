using UnityEngine;

public class EnemyAnimationRelay : MonoBehaviour
{
    [Header("Link")]
    public EnemyDuelAI mainAI; // Drag the parent object with EnemyDuelAI here

    // 1. REFLEX EVENT
    // Set this Event in the Animation window at the moment the hand grabs the gun
    public void TriggerDrawMoment()
    {
        if (mainAI != null)
        {
            mainAI.RegisterDrawAction();
        }
    }

    // 2. FIRE EVENT (Optional)
    // You can use this if you want the shot to be perfectly synced with the animation frame
    // instead of the mathematical timer.
    public void TriggerFireMoment()
    {
        // Currently handled by the Coroutine timer in EnemyDuelAI, 
        // but useful to have for polish later.
    }
}