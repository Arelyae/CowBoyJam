using UnityEngine;
using TMPro;

public class LeaderboardEntry : MonoBehaviour
{
    public TextMeshProUGUI rankText;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI scoreText;

    public void SetEntry(int rank, string username, string score)
    {
        if (rankText) rankText.text = $"#{rank}";
        if (nameText) nameText.text = username;
        if (scoreText) scoreText.text = score;
    }
}