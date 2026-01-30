using Dan.Main;
using Dan.Models;
using DG.Tweening;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class LeaderboardManager : MonoBehaviour
{
    [Header("--- Configuration ---")]
    [Tooltip("Paste your Public Key from the Leaderboard Creator Dashboard here.")]
    public string publicKey;
    public float Take;

    [Header("--- UI References ---")]
    public GameObject leaderboardPanel;
    public Transform entriesContainer;
    public GameObject entryPrefab;
    public CanvasGroup panelCanvasGroup;
    public GameObject loadingSpinner;

    [Header("--- Input ---")]
    public TMP_InputField nameInputField;
    public GameObject submitButton;
    public GameObject restartButton; // Button to trigger restart after viewing scores

    private float _pendingScore;

    // --- PROPERTY: USED BY END MANAGER TO BLOCK INPUTS ---
    public bool IsTyping
    {
        get
        {
            return nameInputField != null && nameInputField.isFocused;
        }
    }
    // ----------------------------------------------------

    void Start()
    {
        if (leaderboardPanel) leaderboardPanel.SetActive(false);
        if (panelCanvasGroup) panelCanvasGroup.alpha = 0f;
    }

    // Called by FinalScoreManager
    public void OpenLeaderboard(float finalScore)
    {
        _pendingScore = finalScore;

        if (leaderboardPanel)
        {
            leaderboardPanel.SetActive(true);
            if (panelCanvasGroup) panelCanvasGroup.DOFade(1f, 0.5f).SetUpdate(true);
        }

        // Auto-focus the input field so player can type immediately
        if (nameInputField) nameInputField.Select();

        // Fetch current high scores immediately
        FetchLeaderboard();
    }

    public void SubmitScore()
    {
        if (string.IsNullOrEmpty(nameInputField.text)) return;

        if (loadingSpinner) loadingSpinner.SetActive(true);
        if (submitButton) submitButton.SetActive(false);
        if (nameInputField) nameInputField.interactable = false;

        // Convert Float Score (e.g. 1.234s) to Int Milliseconds (1234) for precision
        int scoreInMilli = Mathf.FloorToInt(_pendingScore * 1000);

        LeaderboardCreator.UploadNewEntry(publicKey, nameInputField.text, scoreInMilli, (msg) =>
        {
            // Reload the board to show the new entry
            FetchLeaderboard();
        });
    }

    private void FetchLeaderboard()
    {
        // Dans ce package, les propriétés sont souvent nommées différemment :
        // 'Take' au lieu de 'Limit'
        // 'IsDescending' peut ne pas exister directement dans la requête, 
        // car l'ordre est souvent géré dans le Dashboard Unity ou via une autre propriété.

        LeaderboardSearchQuery query = new LeaderboardSearchQuery
        {
            Take = 100, // Utilisez 'Take' pour définir le nombre d'entrées
            Skip = 0   // Optionnel : utile pour la pagination
        };

        LeaderboardCreator.GetLeaderboard(publicKey, query, (entries) =>
        {
            if (loadingSpinner) loadingSpinner.SetActive(false);
            UpdateUI(entries);
        });
    }

    private void UpdateUI(Dan.Models.Entry[] entries)
    {
        // 1. Clear old entries
        foreach (Transform child in entriesContainer)
        {
            Destroy(child.gameObject);
        }

        // 2. Spawn new entries
        for (int i = 0; i < entries.Length; i++)
        {
            GameObject newRow = Instantiate(entryPrefab, entriesContainer);
            LeaderboardEntry entryScript = newRow.GetComponent<LeaderboardEntry>();

            // Convert back: 1234ms -> "1.234s"
            float scoreSec = entries[i].Score / 1000f;
            string formattedScore = $"{scoreSec:F3}s";

            if (entryScript)
            {
                entryScript.SetEntry(entries[i].Rank, entries[i].Username, formattedScore);
            }
        }

        // Show Restart Button now that interaction is done
        if (restartButton) restartButton.SetActive(true);
    }
}