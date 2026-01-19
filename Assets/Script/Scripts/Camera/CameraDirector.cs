using UnityEngine;
using System.Collections.Generic;

public class CameraDirector : MonoBehaviour
{
    [Header("--- Connexions ---")]
    public Camera mainCamera;
    public EndManager endManager; // Pour savoir quand c'est la victoire
    public Transform enemyTransform; // La cible (L'ennemi qui meurt)

    [Header("--- Profils ---")]
    [Tooltip("Liste des angles possibles. Le script en choisira un au hasard.")]
    public List<KillCamProfile> killCamAngles;

    void Start()
    {
        if (mainCamera == null) mainCamera = Camera.main;
    }

    // Fonction publique appelée par l'EndManager
    public void TriggerKillCam()
    {
        if (killCamAngles.Count == 0) return;

        // 1. Choisir un angle au hasard
        KillCamProfile chosenProfile = killCamAngles[Random.Range(0, killCamAngles.Count)];

        // 2. Appliquer les réglages
        ApplyProfile(chosenProfile);
    }

    void ApplyProfile(KillCamProfile profile)
    {
        // Appliquer le FOV
        mainCamera.fieldOfView = profile.fieldOfView;

        if (profile.lookAtEnemy && enemyTransform != null)
        {
            // MODE RELATIF : On se place par rapport à l'ennemi
            // On prend la position de l'ennemi + l'offset défini dans le profil
            // (On ignore la 'targetPosition' absolue du profil dans ce cas)
            Vector3 finalPos = enemyTransform.position + profile.offsetFromEnemy;
            mainCamera.transform.position = finalPos;

            // On regarde le centre de l'ennemi (ou sa tête si on ajuste l'offset)
            mainCamera.transform.LookAt(enemyTransform.position + Vector3.up * 1.0f);
        }
        else
        {
            // MODE ABSOLU : On téléporte la caméra à la position enregistrée
            mainCamera.transform.position = profile.targetPosition;
            mainCamera.transform.eulerAngles = profile.targetRotation;
        }

        Debug.Log($"KillCam activée : {profile.name}");
    }
}