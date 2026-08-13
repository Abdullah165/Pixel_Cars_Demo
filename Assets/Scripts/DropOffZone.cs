using UnityEngine;

/// <summary>A trigger-based destination that converts the taxi's carried passengers into score.</summary>
[RequireComponent(typeof(BoxCollider2D))]
public class DropOffZone : MonoBehaviour
{
    [Min(1)] [SerializeField] private int scorePerPassenger = 10;
    private ScoreManager scoreManager;
    private FareManager fareManager;

    private void Awake()
    {
        GetComponent<BoxCollider2D>().isTrigger = true;
        scoreManager = FindFirstObjectByType<ScoreManager>();
        fareManager = FindFirstObjectByType<FareManager>();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        PlayerTaxiController taxi = other.GetComponent<PlayerTaxiController>();
        if (taxi == null || taxi.PassengerCount == 0) return;
        int droppedOff = taxi.DropOffAllPassengers();
        if (droppedOff <= 0) return;
        if (scoreManager == null) scoreManager = FindFirstObjectByType<ScoreManager>();
        if (fareManager == null) fareManager = FindFirstObjectByType<FareManager>();
        scoreManager?.AddScore(droppedOff * scorePerPassenger);
        fareManager?.CompleteFareFromDropOff();
        gameObject.SetActive(false);
    }
}