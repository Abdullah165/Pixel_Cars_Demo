using UnityEngine;

/// <summary>
/// Runs the Subway-Surfers-style taxi loop: stop beside a waiting pedestrian,
/// carry them, then stop beside the pooled drop-off marker to finish the fare.
/// </summary>
public class FareManager : MonoBehaviour
{
    [Header("Pickup / Drop-off")]
    [Min(0.1f)] [SerializeField] private float curbReach = 1f;
    [Min(0.1f)] [SerializeField] private float stopVerticalReach = 0.45f;
    [Min(0.1f)] [SerializeField] private float destinationSpawnY = 6.2f;
    [Min(0.1f)] [SerializeField] private float destinationScrollSpeed = 2f;
    [Min(0f)] [SerializeField] private float fareCooldown = 1.25f;

    private PlayerTaxiController playerTaxi;
    private GameObject destinationMarker;
    private TextMesh destinationText;
    private bool passengerOnBoard;
    private int faresCompleted;
    private float nextFareAvailableTime;

    public bool PassengerOnBoard => passengerOnBoard;
    public int FaresCompleted => faresCompleted;
    public string StatusText => passengerOnBoard ? "PASSENGER ON BOARD\nSTOP AT DROP OFF" : "STOP BY A WAITING PASSENGER";

    private void Awake()
    {
        CreateReusableDestinationMarker();
    }

    private void Update()
    {
        if (playerTaxi == null)
        {
            playerTaxi = FindFirstObjectByType<PlayerTaxiController>();
            return;
        }

        if (!passengerOnBoard)
        {
            return;
        }

        if (GameSpeedController.IsWorldStopped)
        {
            return;
        }

        destinationMarker.transform.Translate(Vector3.down * destinationScrollSpeed * Time.deltaTime, Space.World);

        if (destinationMarker.transform.position.y < -6.5f)
        {
            // A missed stop simply returns to the next passenger opportunity.
            CancelFare();
        }
    }

    public bool TryPickUp(PassengerBehavior passenger, PlayerTaxiController taxi)
    {
        if (taxi == null)
        {
            return false;
        }

        playerTaxi = taxi;
        if (passengerOnBoard || Time.time < nextFareAvailableTime || !taxi.IsStopped || !IsAtCurb(passenger.transform.position))
        {
            return false;
        }

        passengerOnBoard = true;
        taxi.AddPassenger();
        passenger.ReturnToSpawnPool();
        SpawnDestination(passenger.transform.position.x);
        return true;
    }

    private bool IsAtCurb(Vector3 curbPosition)
    {
        float horizontalDistance = Mathf.Abs(curbPosition.x - playerTaxi.transform.position.x);
        float verticalDistance = Mathf.Abs(curbPosition.y - playerTaxi.transform.position.y);
        return horizontalDistance <= curbReach && verticalDistance <= stopVerticalReach;
    }

    private void SpawnDestination(float passengerSideX)
    {
        float sideX = Mathf.Sign(passengerSideX) * Mathf.Abs(passengerSideX);
        if (Mathf.Approximately(sideX, 0f))
        {
            sideX = 1.15f;
        }

        destinationMarker.transform.position = new Vector3(sideX, destinationSpawnY, 0f);
        destinationMarker.SetActive(true);
    }

    public void CompleteFareFromDropOff()
    {
        if (!passengerOnBoard) return;
        passengerOnBoard = false;
        faresCompleted++;
        nextFareAvailableTime = Time.time + fareCooldown;
        destinationMarker.SetActive(false);
    }

    private void CancelFare()
    {
        playerTaxi?.DropOffAllPassengers();
        passengerOnBoard = false;
        nextFareAvailableTime = Time.time + fareCooldown;
        destinationMarker.SetActive(false);
    }

    private void CreateReusableDestinationMarker()
    {
        destinationMarker = new GameObject("Pooled Drop Off Marker");
        destinationMarker.transform.SetParent(transform, false);
        destinationText = destinationMarker.AddComponent<TextMesh>();
        destinationText.text = "DROP\nOFF";
        destinationText.anchor = TextAnchor.MiddleCenter;
        destinationText.alignment = TextAlignment.Center;
        destinationText.fontSize = 64;
        destinationText.characterSize = 0.055f;
        destinationText.color = new Color(1f, 0.8f, 0.05f);
        destinationText.GetComponent<MeshRenderer>().sortingOrder = 4;
        BoxCollider2D collider = destinationMarker.AddComponent<BoxCollider2D>();
        collider.isTrigger = true;
        collider.size = new Vector2(1.4f, 0.9f);
        destinationMarker.AddComponent<DropOffZone>();
        destinationMarker.SetActive(false);
    }
}
