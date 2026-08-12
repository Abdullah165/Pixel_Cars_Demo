using UnityEngine;

/// <summary>Creates the player taxi once when the gameplay scene loads.</summary>
public class TaxiGameBootstrap : MonoBehaviour
{
    [SerializeField] private GameObject playerTaxiPrefab;
    [SerializeField] private Vector2 playerStartPosition = new Vector2(-0.35f, -3.4f);

    private void Awake()
    {
        if (playerTaxiPrefab == null || FindFirstObjectByType<PlayerTaxiController>() != null)
        {
            return;
        }

        Instantiate(playerTaxiPrefab, playerStartPosition, Quaternion.identity);
    }
}
