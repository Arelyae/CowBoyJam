using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

[CreateAssetMenu(fileName = "NewKillCam", menuName = "Duel/Kill Cam Profile")]
public class KillCamProfile : ScriptableObject
{
    [Header("Paramètres de la Caméra")]
    public Vector3 targetPosition;
    public Vector3 targetRotation;
    public float fieldOfView = 60f;

    [Header("Comportement")]
    [Tooltip("Si vrai, la caméra regardera l'ennemi (LookAt) au lieu d'utiliser la rotation stricte ci-dessus.")]
    public bool lookAtEnemy = true;

    [Tooltip("Le décalage par rapport à l'ennemi (si on utilise LookAt).")]
    public Vector3 offsetFromEnemy = new Vector3(2, 1, 2);

    // --- OUTIL ÉDITEUR POUR CAPTURER LA VUE ---
#if UNITY_EDITOR
    [ContextMenu("Capture Main Camera View")]
    public void CaptureView()
    {
        Camera cam = Camera.main;
        if (cam != null)
        {
            targetPosition = cam.transform.position;
            targetRotation = cam.transform.eulerAngles;
            fieldOfView = cam.fieldOfView;
            Debug.Log($"Vue capturée depuis la Main Camera !");
            EditorUtility.SetDirty(this); // Sauvegarde
        }
    }
#endif
}