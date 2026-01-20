using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using DG.Tweening;
using System.Collections;


public class EndManager : MonoBehaviour
{
    [Header("--- Input ---")]
    public InputActionReference reloadAction;
    public ScoreManager scoreManager; 


    [Header("--- Paramètres Victoire (Slow Motion) ---")]
    public float delayBeforeSlowMo = 0.1f;
    public float targetTimeScale = 0.1f;
    public float slowMoDuration = 1.5f;
    public Ease slowMoEase = Ease.OutExpo;

    [Header("--- Paramètres Défaite ---")]
    [Tooltip("Attente en secondes avant de figer le jeu (Laisse le temps d'entendre le 'Clic' ou de voir la mort)")]
    public float defeatDelay = 0.8f; // <--- NOUVEAU : 0.8s est souvent idéal

    [Header("--- État ---")]
    private bool gameIsOver = false;

    [Header("--- Cinématique ---")]
    public CameraDirector cameraDirector;
    public DuelController playerController;
    public EnemyDuelAI enemyAI;

    private void OnEnable()
    {
        if (reloadAction != null) reloadAction.action.Enable();
    }
    private void OnDisable()
    {
        if (reloadAction != null) reloadAction.action.Disable();
    }

    void Start()
    {
        Time.timeScale = 1f;
        Time.fixedDeltaTime = 0.02f;
        gameIsOver = false;
        DOTween.KillAll();
    }

    void Update()
    {
        if (reloadAction != null && reloadAction.action.WasPressedThisFrame())
        {
            RestartGame();
        }
    }

    // --- VICTOIRE (inchangé) ---
    public void TriggerVictory(string details)
    {
        if (gameIsOver) return;
        gameIsOver = true;

        // --- AFFICHAGE DU SCORE ---
        if (scoreManager != null)
        {
            scoreManager.DisplayScore();
        }
        // --------------------------

        if (cameraDirector != null) cameraDirector.TriggerKillCam();
        StartCoroutine(SlowMotionSequence());
    }

    IEnumerator SlowMotionSequence()
    {
        if (delayBeforeSlowMo > 0) yield return new WaitForSeconds(delayBeforeSlowMo);

        Time.fixedDeltaTime = 0.02f * targetTimeScale;

        DOTween.To(() => Time.timeScale, x => Time.timeScale = x, targetTimeScale, slowMoDuration)
            .SetUpdate(true)
            .SetEase(slowMoEase);
    }

    // --- DÉFAITE (MODIFIÉ) ---
    public void TriggerDefeat(string reason)
    {
        if (gameIsOver) return;
        gameIsOver = true;

        Debug.Log($"DÉFAITE ({reason}) - Attente de {defeatDelay}s avant freeze...");

        // On utilise DOTween pour gérer le délai proprement
        // 1. On attend 'defeatDelay' secondes (temps réel)
        // 2. On passe le TimeScale à 0 en 0.2 secondes
        DOTween.To(() => Time.timeScale, x => Time.timeScale = x, 0f, 0.2f)
            .SetDelay(defeatDelay) // <--- C'EST ICI QUE LA MAGIE OPÈRE
            .SetUpdate(true)       // Important : Ignore le TimeScale actuel pour le délai
            .SetEase(Ease.OutQuart);
    }

    public void RestartGame()
    {
        // 1. Nettoyage Global
        DOTween.KillAll(); // Arrête tous les tweens (Camera, Textes, TimeScale)

        // 2. Remettre le temps normal
        Time.timeScale = 1f;
        Time.fixedDeltaTime = 0.02f;
        gameIsOver = false;

        // 3. APPELER LES RESETS INDIVIDUELS

        // UI
        if (scoreManager) scoreManager.ResetScore();

        // Caméra
        if (cameraDirector) cameraDirector.ResetCamera();

        // Joueur
        if (playerController) playerController.ResetPlayer();

        // Ennemi
        if (enemyAI) enemyAI.ResetEnemy();

        Debug.Log("--- JEU REDÉMARRÉ (SOFT RESET) ---");
    }


}