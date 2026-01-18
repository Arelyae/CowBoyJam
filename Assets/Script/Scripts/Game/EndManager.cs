using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using DG.Tweening; // <--- INDISPENSABLE

public class EndManager : MonoBehaviour
{
    [Header("--- Paramètres Slow Motion ---")]
    [Tooltip("Délai initial à vitesse normale avant de lancer le ralenti (pour bien sentir l'impact)")]
    public float delayBeforeSlowMo = 0.1f;

    [Tooltip("La vitesse cible (0 = Arrêt, 0.1 = Matrix, 1 = Normal)")]
    public float targetTimeScale = 0.1f;

    [Tooltip("La durée de la transition (Combien de temps pour passer de 1 à 0.1)")]
    public float slowMoDuration = 1.5f;

    [Tooltip("La courbe de ralentissement (OutExpo est très cinématographique)")]
    public Ease slowMoEase = Ease.OutExpo;

    [Header("--- État ---")]
    private bool gameIsOver = false;

    void Start()
    {
        // RESET IMPORTANT : 
        // Si on vient d'une scène au ralenti, on remet tout à 1
        Time.timeScale = 1f;
        gameIsOver = false;

        // Sécurité DOTween : On tue les tweens qui traînent
        DOTween.KillAll();
    }

    void Update()
    {
        // Restart avec 'R'
        if (gameIsOver && Input.GetKeyDown(KeyCode.R))
        {
            RestartGame();
        }
    }

    // --- VICTOIRE ---
    public void TriggerVictory(string details)
    {
        if (gameIsOver) return;
        gameIsOver = true;

        Debug.Log($"VICTOIRE ({details}) - Lancement séquence SlowMo...");
        StartCoroutine(SlowMotionSequence());
    }

    IEnumerator SlowMotionSequence()
    {
        // 1. Petit délai à vitesse réelle (1.0) 
        // Pour que l'impact de la balle soit violent et instantané
        if (delayBeforeSlowMo > 0)
            yield return new WaitForSeconds(delayBeforeSlowMo);

        // 2. TWEEN DU TIMESCALE
        // On passe de la vitesse actuelle à 'targetTimeScale' en 'slowMoDuration' secondes
        DOTween.To(() => Time.timeScale, x => Time.timeScale = x, targetTimeScale, slowMoDuration)
            .SetUpdate(true) // IMPORTANT : Ignore le ralentissement qu'il est en train de créer
            .SetEase(slowMoEase); // Courbe esthétique

        Debug.Log("Jeu au ralenti (Appuyez sur 'R' pour relancer)");
    }

    // --- DÉFAITE ---
    public void TriggerDefeat(string reason)
    {
        if (gameIsOver) return;
        gameIsOver = true;

        Debug.Log($"DÉFAITE ({reason})");

        // Pour la défaite, on peut aussi faire un ralenti, mais plus rapide
        // Ou un arrêt brutal. Ici, je mets un ralenti rapide pour l'effet dramatique.
        DOTween.To(() => Time.timeScale, x => Time.timeScale = x, 0f, 0.5f)
            .SetUpdate(true)
            .SetEase(Ease.OutQuart);
    }

    public void RestartGame()
    {
        // 1. On tue tous les tweens en cours (pour ne pas qu'ils continuent dans la scène suivante)
        DOTween.KillAll();

        // 2. On remet la vitesse normale
        Time.timeScale = 1f;

        // 3. Reload
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}