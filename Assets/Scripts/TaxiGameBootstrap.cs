using UnityEngine;

/// <summary>Creates the player taxi once when the gameplay scene loads.</summary>
public class TaxiGameBootstrap : MonoBehaviour
{
    [SerializeField] private GameObject playerTaxiPrefab;
    [SerializeField] private Vector2 playerStartPosition = new Vector2(-0.35f, -3.4f);

    private void Awake()
    {
        // Restore normal gameplay speed after a crash reload, then create the
        // shared systems once for this scene.
        Time.timeScale = 1f;
        if (GetComponent<GameSpeedController>() == null) gameObject.AddComponent<GameSpeedController>();
        if (GetComponent<FareManager>() == null) gameObject.AddComponent<FareManager>();
        if (GetComponent<TaxiGameUI>() == null) gameObject.AddComponent<TaxiGameUI>();

        if (playerTaxiPrefab == null || FindFirstObjectByType<PlayerTaxiController>() != null)
        {
            return;
        }

        Instantiate(playerTaxiPrefab, playerStartPosition, Quaternion.identity);
    }
}
