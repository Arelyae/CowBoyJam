using UnityEngine;
using FMODUnity; // Nécessite l'intégration FMOD

public class DuelAudioHandler : MonoBehaviour
{
    [Header("Références")]
    [Tooltip("Le script DuelController à écouter")]
    public DuelController controller;

    [Header("FMOD Events")]
    public EventReference sfxDraw;   // Bruit de cuir / frottement
    public EventReference sfxLoad;   // Le "Clic" du chien
    public EventReference sfxFire;   // Le coup de feu (fort)
    public EventReference sfxFeint;  // Bruit de pas / tissu
    public EventReference sfxFumble; // Bruit mécanique cassé / "Clunk"
    public EventReference sfxDeath;  // Impact / Chute

    private void OnEnable()
    {
        if (controller == null)
            controller = GetComponent<DuelController>();

        // Abonnement aux événements
        controller.OnDraw += PlayDraw;
        controller.OnLoad += PlayLoad;
        controller.OnFire += PlayFire;
        controller.OnFeint += PlayFeint;
        controller.OnFumble += PlayFumble;
        controller.OnDeath += PlayDeath;
    }

    private void OnDisable()
    {
        // Désabonnement OBLIGATOIRE pour éviter les erreurs de mémoire
        if (controller != null)
        {
            controller.OnDraw -= PlayDraw;
            controller.OnLoad -= PlayLoad;
            controller.OnFire -= PlayFire;
            controller.OnFeint -= PlayFeint;
            controller.OnFumble -= PlayFumble;
            controller.OnDeath -= PlayDeath;
        }
    }

    // --- Fonctions de Lecture ---

    void PlayDraw()
    {
        PlaySound(sfxDraw);
    }

    void PlayLoad()
    {
        PlaySound(sfxLoad);
    }

    void PlayFire()
    {
        PlaySound(sfxFire);
    }

    void PlayFeint()
    {
        PlaySound(sfxFeint);
    }

    void PlayFumble()
    {
        // Le son du Fumble est critique pour comprendre l'erreur
        PlaySound(sfxFumble);
    }

    void PlayDeath()
    {
        PlaySound(sfxDeath);
    }

    // Helper générique FMOD
    void PlaySound(EventReference soundEvent)
    {
        if (!soundEvent.IsNull)
        {
            RuntimeManager.PlayOneShot(soundEvent, transform.position);
        }
        else
        {
            Debug.LogWarning($"[DuelAudio] Event FMOD manquant pour une action sur {gameObject.name}");
        }
    }
}