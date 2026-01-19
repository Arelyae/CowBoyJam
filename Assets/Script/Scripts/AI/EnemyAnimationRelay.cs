using UnityEngine;

public class EnemyAnimationRelay : MonoBehaviour
{
    [Header("Link")]
    public EnemyDuelAI mainAI; // Référence au script principal de l'IA

    // Cette fonction sera appelée par l'Event d'Animation
    public void TriggerDrawMoment()
    {
        if (mainAI != null)
        {
            mainAI.RegisterDrawAction();
        }
    }

    // Optionnel : Si vous voulez aussi synchroniser le tir de l'IA (le feu)
    public void TriggerFireMoment()
    {
        // Logique de tir de l'IA si besoin
    }
}