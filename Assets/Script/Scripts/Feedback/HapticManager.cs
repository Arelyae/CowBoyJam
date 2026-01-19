using UnityEngine;
using UnityEngine.InputSystem; // Nécessaire pour accéder à la manette
using System.Collections;

public class HapticManager : MonoBehaviour
{
    [Header("--- Connexion ---")]
    [Tooltip("Le contrôleur du joueur pour écouter les événements")]
    public DuelController playerController;

    [Header("--- Profils de Vibrations ---")]
    public HapticProfile drawEffect = new HapticProfile(0.1f, 0.5f, 0.1f);   // Léger clic (Gâchette gauche)
    public HapticProfile loadEffect = new HapticProfile(0.2f, 0.8f, 0.15f);  // Clic mécanique sec (Chien)
    public HapticProfile fireEffect = new HapticProfile(1.0f, 1.0f, 0.3f);   // Recul violent (Tir)
    public HapticProfile feintEffect = new HapticProfile(0.2f, 0.2f, 0.1f);  // Petit retour
    public HapticProfile fumbleEffect = new HapticProfile(0.8f, 0.2f, 0.4f); // Vibration "sale" et longue
    public HapticProfile deathEffect = new HapticProfile(1.0f, 0.0f, 1.0f);  // Sourd et long

    private Coroutine currentRumble;

    // --- SETUP DES EVENTS ---
    private void OnEnable()
    {
        if (playerController == null) return;

        playerController.OnDraw += PlayDraw;
        playerController.OnLoad += PlayLoad;
        playerController.OnFire += PlayFire;
        playerController.OnFeint += PlayFeint;
        playerController.OnFumble += PlayFumble;
        playerController.OnDeath += PlayDeath;
    }

    private void OnDisable()
    {
        if (playerController == null) return;

        playerController.OnDraw -= PlayDraw;
        playerController.OnLoad -= PlayLoad;
        playerController.OnFire -= PlayFire;
        playerController.OnFeint -= PlayFeint;
        playerController.OnFumble -= PlayFumble;
        playerController.OnDeath -= PlayDeath;

        StopHaptics(); // Sécurité : on coupe tout si le script se désactive
    }

    // --- FONCTIONS RELAIS ---
    void PlayDraw() => TriggerHaptic(drawEffect);
    void PlayLoad() => TriggerHaptic(loadEffect);
    void PlayFire() => TriggerHaptic(fireEffect);
    void PlayFeint() => TriggerHaptic(feintEffect);
    void PlayFumble() => TriggerHaptic(fumbleEffect);
    void PlayDeath() => TriggerHaptic(deathEffect);

    // --- LOGIQUE HAPTIQUE ---
    public void TriggerHaptic(HapticProfile profile)
    {
        // On vérifie s'il y a une manette connectée
        if (Gamepad.current == null) return;

        // Si une vibration est déjà en cours, on l'arrête pour jouer la nouvelle
        if (currentRumble != null) StopCoroutine(currentRumble);

        currentRumble = StartCoroutine(HapticRoutine(profile));
    }

    IEnumerator HapticRoutine(HapticProfile p)
    {
        // Low Frequency = Moteur gauche (Lourd / Sourd)
        // High Frequency = Moteur droit (Aigu / Subtil)
        Gamepad.current.SetMotorSpeeds(p.lowFreq, p.highFreq);

        yield return new WaitForSeconds(p.duration);

        // Arrêt
        Gamepad.current.ResetHaptics();
        currentRumble = null;
    }

    void StopHaptics()
    {
        if (Gamepad.current != null) Gamepad.current.ResetHaptics();
    }
}

// Petite classe utilitaire pour l'Inspecteur
[System.Serializable]
public class HapticProfile
{
    [Range(0, 1)] public float lowFreq;  // Grondement (Explosions, Chocs)
    [Range(0, 1)] public float highFreq; // Buzz (Clics, Mécanismes)
    public float duration;               // Temps en secondes

    public HapticProfile(float low, float high, float time)
    {
        lowFreq = low;
        highFreq = high;
        duration = time;
    }
}