using UnityEngine;

public class DuelArbiter : MonoBehaviour
{
    [Header("--- État du Duel ---")]
    [Tooltip("Passe à true quand l'ennemi devient rouge / attaque.")]
    public bool enemyHasStartedAction = false;

    // Cette fonction ne sert plus qu'à logger l'information dans la console.
    // Elle ne décide plus de la vie ou de la mort (c'est le DuelController qui le fait).
    public void RegisterShot(DuelController shooter, float accuracyTime)
    {
        // CAS 1 : Tir Honorable
        if (enemyHasStartedAction)
        {
            Debug.Log($"ARBITRE : Tir validé (Honorable). Focus: {accuracyTime:F2}s");
        }
        // CAS 2 : Tir Déshonorant
        else
        {
            Debug.Log("ARBITRE : Tir anticipé (Déshonorant).");
        }
    }
}