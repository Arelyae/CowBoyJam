using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem; // <--- NÉCESSAIRE
using DG.Tweening;
using System.Collections; // <--- AJOUTEZ CETTE LIGNE

public class EndManager : MonoBehaviour
{
    [Header("--- Input ---")]
    [Tooltip("L'action pour recharger la scène (ex: North Button / Touche R)")]
    public InputActionReference reloadAction; // <--- NOUVEAU

    [Header("--- Paramètres Slow Motion ---")]
    public float delayBeforeSlowMo = 0.1f;
    public float targetTimeScale = 0.1f;
    public float slowMoDuration = 1.5f;
    public Ease slowMoEase = Ease.OutExpo;

    [Header("--- État ---")]
    private bool gameIsOver = false;

    // --- GESTION DE L'INPUT SYSTEM ---
    private void OnEnable()
    {
        if (reloadAction != null) reloadAction.action.Enable();
    }

    private void OnDisable()
    {
        if (reloadAction != null) reloadAction.action.Disable();
    }
    // ---------------------------------

    void Start()
    {
        Time.timeScale = 1f;
        gameIsOver = false;
        DOTween.KillAll();

        // Sécurité physique
        Time.fixedDeltaTime = 0.02f;
    }

    void Update()
    {
        // 1. INPUT SYSTEM (Manette "North" ou autre binding)
        if (reloadAction != null && reloadAction.action.WasPressedThisFrame())
        {
            RestartGame();
        }

        // 2. DEBUG CLAVIER (Touche R - Hardcodée pour le test rapide)
        if (Input.GetKeyDown(KeyCode.R))
        {
            RestartGame();
        }
    }

    public void TriggerVictory(string details)
    {
        if (gameIsOver) return;
        gameIsOver = true;
        StartCoroutine(SlowMotionSequence());
    }

    IEnumerator SlowMotionSequence()
    {
        if (delayBeforeSlowMo > 0) yield return new WaitForSeconds(delayBeforeSlowMo);

        // Ajustement physique pour la fluidité au ralenti
        Time.fixedDeltaTime = 0.02f * targetTimeScale;

        DOTween.To(() => Time.timeScale, x => Time.timeScale = x, targetTimeScale, slowMoDuration)
            .SetUpdate(true)
            .SetEase(slowMoEase);
    }

    public void TriggerDefeat(string reason)
    {
        if (gameIsOver) return;
        gameIsOver = true;

        DOTween.To(() => Time.timeScale, x => Time.timeScale = x, 0f, 0.5f)
            .SetUpdate(true)
            .SetEase(Ease.OutQuart);
    }

    public void RestartGame()
    {
        Debug.Log("RESTART DEMANDÉ");

        // Nettoyage complet
        DOTween.KillAll();
        Time.timeScale = 1f;
        Time.fixedDeltaTime = 0.02f;

        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}