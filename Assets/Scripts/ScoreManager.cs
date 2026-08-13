using UnityEngine;
using UnityEngine.UI;

/// <summary>Maintains the player's score and updates the top-left score display.</summary>
public class ScoreManager : MonoBehaviour
{
    [SerializeField] private Text scoreText;
    [SerializeField] private int score;

    public int Score => score;

    private void Awake()
    {
        UpdateScoreText();
    }

    public void AddScore(int amount)
    {
        if (amount <= 0)
        {
            return;
        }

        score += amount;
        UpdateScoreText();
    }

    private void UpdateScoreText()
    {
        if (scoreText == null)
        {
            scoreText = GetComponentInChildren<Text>(true);
        }

        if (scoreText != null)
        {
            scoreText.text = "Score: " + score;
        }
    }
}