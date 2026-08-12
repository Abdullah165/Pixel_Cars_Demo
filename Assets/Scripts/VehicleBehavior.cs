using Lean.Pool;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Movement and lifecycle for a pooled traffic car.</summary>
public class VehicleBehavior : MonoBehaviour
{
    private static readonly HashSet<VehicleBehavior> ActiveVehicles = new HashSet<VehicleBehavior>();

    [SerializeField] private float moveSpeed = 3f;
    [SerializeField] private float despawnYLocation = -6.5f;

    [Header("Traffic Following")]
    [Tooltip("Cars within this horizontal tolerance are treated as being in the same lane.")]
    [SerializeField] private float laneTolerance = 0.12f;
    [Tooltip("Minimum centre-to-centre gap between two traffic cars in one lane.")]
    [SerializeField] private float minimumFollowingDistance = 0.58f;

    public static void GetActiveVehicles(List<VehicleBehavior> results)
    {
        results.Clear();
        foreach (VehicleBehavior vehicle in ActiveVehicles)
        {
            if (vehicle != null && vehicle.isActiveAndEnabled)
            {
                results.Add(vehicle);
            }
        }
    }

    public void Configure(float speed, float despawnY, float followingDistance)
    {
        moveSpeed = speed;
        despawnYLocation = despawnY;
        minimumFollowingDistance = followingDistance;
    }

    private void OnEnable()
    {
        ActiveVehicles.Add(this);
    }

    private void OnDisable()
    {
        ActiveVehicles.Remove(this);
    }

    private void Update()
    {
        // A faster car may only cover the space above the safe gap to the closest
        // car ahead in its lane. This makes overtaking traffic impossible without
        // relying on collision callbacks or frame-order luck.
        float safeMoveDistance = GetSafeMoveDistance();
        float moveDistance = Mathf.Min(moveSpeed * Time.deltaTime, safeMoveDistance);
        transform.Translate(Vector3.down * moveDistance, Space.World);

        if (transform.position.y < despawnYLocation)
        {
            ReturnToPool();
        }
    }

    private float GetSafeMoveDistance()
    {
        float closestVehicleAheadY = float.NegativeInfinity;
        float currentX = transform.position.x;
        float currentY = transform.position.y;

        foreach (VehicleBehavior other in ActiveVehicles)
        {
            if (other == null || other == this)
            {
                continue;
            }

            Vector3 otherPosition = other.transform.position;
            bool isInSameLane = Mathf.Abs(otherPosition.x - currentX) <= laneTolerance;
            bool isAhead = otherPosition.y < currentY;

            if (isInSameLane && isAhead && otherPosition.y > closestVehicleAheadY)
            {
                closestVehicleAheadY = otherPosition.y;
            }
        }

        if (float.IsNegativeInfinity(closestVehicleAheadY))
        {
            return float.PositiveInfinity;
        }

        float currentGap = currentY - closestVehicleAheadY;
        return Mathf.Max(0f, currentGap - minimumFollowingDistance);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        PlayerTaxiController taxi = other.GetComponent<PlayerTaxiController>();
        if (taxi != null)
        {
            taxi.Crash();
        }
    }

    public void ReturnToPool()
    {
        if (LeanPool.Links.ContainsKey(gameObject))
        {
            LeanPool.Despawn(gameObject);
        }
        else
        {
            gameObject.SetActive(false);
        }
    }
}
