using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using DG.Tweening;
using System.Collections;

public class SceneTransitionManager : MonoBehaviour
{
    public static SceneTransitionManager Instance;

    [Header("--- Settings ---")]
    public string mainGameSceneName = "DuelScene";
    public float fadeDuration = 1.0f;

    [Header("--- UI References ---")]
    public CanvasGroup faderCanvasGroup;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // FORCE BLACK IMMEDIATELY
        if (faderCanvasGroup != null)
        {
            faderCanvasGroup.alpha = 1f;
            faderCanvasGroup.blocksRaycasts = true;
        }
    }

    private void Start()
    {
        // --- FIX FOR PLAY MODE ---
        // If we just pressed Play, OnSceneLoaded won't fire for the current scene.
        // We must manually trigger the first fade-in here.
        FadeIn();
        faderCanvasGroup.alpha = 0f;
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Triggered automatically when a NEW scene loads
        FadeIn();
    }

    // Shared Fade-In Logic
    private void FadeIn()
    {
        if (faderCanvasGroup != null)
        {
            faderCanvasGroup.alpha = 1f; // Ensure start at black
            faderCanvasGroup.blocksRaycasts = true;

            faderCanvasGroup.DOFade(0f, fadeDuration)
                .SetEase(Ease.InOutSine)
                .OnComplete(() =>
                {
                    faderCanvasGroup.blocksRaycasts = false; // Input unlocked
                });
        }
    }

    // --- PUBLIC FUNCTIONS ---

    public void LoadGameScene()
    {
        StartCoroutine(TransitionRoutine(mainGameSceneName));
    }

    public void LoadSpecificScene(string sceneName)
    {
        StartCoroutine(TransitionRoutine(sceneName));
    }

    private IEnumerator TransitionRoutine(string sceneName)
    {
        // 1. Fade OUT (To Black)
        if (faderCanvasGroup != null)
        {
            faderCanvasGroup.blocksRaycasts = true;
            yield return faderCanvasGroup.DOFade(1f, fadeDuration)
                .SetEase(Ease.InOutSine)
                .WaitForCompletion();
        }

        // 2. Load Scene
        SceneManager.LoadScene(sceneName);
    }
}