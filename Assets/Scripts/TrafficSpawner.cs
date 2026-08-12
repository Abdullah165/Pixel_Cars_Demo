using System;
using Lean.Pool;
using UnityEngine;

public enum TrafficDifficulty
{
    Easy,
    Medium,
    Hard
}

[Serializable]
public class TrafficDifficultySettings
{
    [Header("Traffic Pace")]
    [Tooltip("All cars use this speed. A shared speed preserves the planned row spacing.")]
    [Min(0.1f)] public float trafficSpeed = 3f;
    [Tooltip("Desired time from one obstacle row to the next.")]
    [Min(0.1f)] public float requestedRowInterval = 1.1f;

    [Header("Guaranteed Player Window")]
    [Tooltip("Extra time granted before the next required lane decision.")]
    [Min(0f)] public float inputReactionTime = 0.25f;
    [Tooltip("Minimum world-space gap between rows. It must cover a full overtake or undertake manoeuvre.")]
    [Min(0.1f)] public float minimumRowWorldGap = 3f;
    [Tooltip("Minimum time between spawning a row and the earliest possible player contact.")]
    [Min(0.1f)] public float minimumSpawnLeadTime = 1.3f;
}

/// <summary>
/// Creates procedural traffic as mathematically spaced one-car obstacle rows.
/// With two lanes, every row blocks exactly one lane, so the other lane is a
/// valid swipe destination. The next row is delayed until the taxi can switch
/// lane and complete a vertical manoeuvre without reaching it.
/// </summary>
public class TrafficSpawner : MonoBehaviour
{
    [Header("Traffic Prefabs")]
    [SerializeField] private GameObject[] vehiclePrefabs;

    [Header("Difficulty (editable for debugging)")]
    [SerializeField] private TrafficDifficulty difficulty = TrafficDifficulty.Medium;
    [SerializeField] private TrafficDifficultySettings easy = new TrafficDifficultySettings
    {
        trafficSpeed = 2.25f,
        requestedRowInterval = 1.5f,
        inputReactionTime = 0.35f,
        minimumRowWorldGap = 3.4f,
        minimumSpawnLeadTime = 1.75f
    };
    [SerializeField] private TrafficDifficultySettings medium = new TrafficDifficultySettings
    {
        trafficSpeed = 3f,
        requestedRowInterval = 1.1f,
        inputReactionTime = 0.25f,
        minimumRowWorldGap = 3f,
        minimumSpawnLeadTime = 1.3f
    };
    [SerializeField] private TrafficDifficultySettings hard = new TrafficDifficultySettings
    {
        trafficSpeed = 3.8f,
        requestedRowInterval = 0.78f,
        inputReactionTime = 0.18f,
        minimumRowWorldGap = 2.9f,
        minimumSpawnLeadTime = 0.95f
    };

    [Header("Two-Lane Road")]
    [Tooltip("The planner uses these exact lane centres for both the taxi and traffic. Changing their distance automatically updates the calculated lane-change time.")]
    [SerializeField] private float[] laneXPositions = { -0.35f, 0.35f };
    [Tooltip("Extra vertical distance reserved before a car can touch the taxi.")]
    [Min(0f)] [SerializeField] private float contactSafetyBuffer = 0.35f;
    [SerializeField] private float despawnYLocation = -6.5f;

    [Header("Lean Pool")]
    [Min(1)] [SerializeField] private int poolSizePerPrefab = 6;

    private LeanGameObjectPool[] pools;
    private PlayerTaxiController playerTaxi;
    private float nextSpawnTime;

    /// <summary>Read-only lane data used by the player taxi to stay on this road.</summary>
    public float[] LaneXPositions => laneXPositions;

    private TrafficDifficultySettings CurrentSettings
    {
        get
        {
            switch (difficulty)
            {
                case TrafficDifficulty.Easy: return easy;
                case TrafficDifficulty.Hard: return hard;
                default: return medium;
            }
        }
    }

    private void Awake()
    {
        CreatePools();
        nextSpawnTime = Time.time + 0.25f;
    }

    private void Update()
    {
        if (Time.time < nextSpawnTime)
        {
            return;
        }

        if (TrySpawnPlannedRow())
        {
            // This interval is derived from the actual taxi controls, not a
            // random density value. It is the minimum time between decisions.
            nextSpawnTime = Time.time + GetGuaranteedRowInterval();
        }
        else
        {
            // Waiting for a valid lead window is safe; forcing a row is not.
            nextSpawnTime = Time.time + 0.1f;
        }
    }

    private void CreatePools()
    {
        if (vehiclePrefabs == null || vehiclePrefabs.Length == 0)
        {
            return;
        }

        pools = new LeanGameObjectPool[vehiclePrefabs.Length];
        for (int i = 0; i < vehiclePrefabs.Length; i++)
        {
            GameObject prefab = vehiclePrefabs[i];
            if (prefab == null)
            {
                continue;
            }

            GameObject poolObject = new GameObject($"Traffic Pool - {prefab.name}");
            poolObject.transform.SetParent(transform, false);

            LeanGameObjectPool pool = poolObject.AddComponent<LeanGameObjectPool>();
            pool.Prefab = prefab;
            pool.Preload = poolSizePerPrefab;
            pool.Capacity = poolSizePerPrefab;
            pool.Recycle = false;
            pool.Warnings = false;
            pool.PreloadAll();
            pools[i] = pool;
        }
    }

    private bool TrySpawnPlannedRow()
    {
        if (pools == null || pools.Length == 0 || laneXPositions == null || laneXPositions.Length != 2)
        {
            return false;
        }

        if (playerTaxi == null)
        {
            playerTaxi = FindFirstObjectByType<PlayerTaxiController>();
        }

        if (playerTaxi == null || !HasGuaranteedSpawnLead())
        {
            return false;
        }

        LeanGameObjectPool pool = GetAvailablePool();
        if (pool == null)
        {
            return false;
        }

        // A row contains exactly one blocker. It targets the taxi's current lane,
        // making the opposite lane the deterministic correct swipe response.
        int blockedLane = Mathf.Clamp(playerTaxi.CurrentLane, 0, laneXPositions.Length - 1);
        float laneX = laneXPositions[blockedLane];
        GameObject vehicle = LeanPool.Spawn(pool.Prefab, new Vector3(laneX, transform.position.y, 0f), Quaternion.identity, transform);
        if (vehicle == null)
        {
            return false;
        }

        TrafficDifficultySettings settings = CurrentSettings;
        VehicleBehavior behavior = vehicle.GetComponent<VehicleBehavior>();
        if (behavior != null)
        {
            behavior.Configure(settings.trafficSpeed, despawnYLocation, GetRequiredFollowingDistance());
        }

        return true;
    }

    private bool HasGuaranteedSpawnLead()
    {
        TrafficDifficultySettings settings = CurrentSettings;

        // The taxi may immediately overtake, reducing the distance to the row.
        // Calculate against that worst legal manoeuvre, then only spawn when the
        // response window still satisfies this difficulty's lead-time rule.
        float highestReachableTaxiY = playerTaxi.transform.position.y + playerTaxi.MaximumManeuverDistance;
        float availableDistance = transform.position.y - highestReachableTaxiY - contactSafetyBuffer;
        float leadTime = availableDistance / settings.trafficSpeed;

        return leadTime >= settings.minimumSpawnLeadTime;
    }

    private float GetGuaranteedRowInterval()
    {
        TrafficDifficultySettings settings = CurrentSettings;

        // Crossing one lane plus input reaction is the horizontal decision cost.
        float laneDecisionTime = playerTaxi.LaneChangeDuration + settings.inputReactionTime;

        // A full up/down move can shift the taxi by maneuverDistance. Reserve
        // two such moves plus collision clearance between rows, so a vertical
        // manoeuvre cannot skip from one row straight into the next one.
        float requiredWorldGap = Mathf.Max(
            settings.minimumRowWorldGap,
            (playerTaxi.MaximumManeuverDistance * 2f) + contactSafetyBuffer);
        float verticalDecisionTime = requiredWorldGap / settings.trafficSpeed;

        return Mathf.Max(settings.requestedRowInterval, laneDecisionTime, verticalDecisionTime);
    }

    private float GetRequiredFollowingDistance()
    {
        TrafficDifficultySettings settings = CurrentSettings;
        return Mathf.Max(contactSafetyBuffer, settings.minimumRowWorldGap * 0.2f);
    }

    private LeanGameObjectPool GetAvailablePool()
    {
        int firstPool = UnityEngine.Random.Range(0, pools.Length);
        for (int attempt = 0; attempt < pools.Length; attempt++)
        {
            LeanGameObjectPool pool = pools[(firstPool + attempt) % pools.Length];
            if (pool != null && pool.Spawned < pool.Capacity)
            {
                return pool;
            }
        }

        return null;
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (laneXPositions == null)
        {
            return;
        }

        Gizmos.color = Color.cyan;
        foreach (float laneX in laneXPositions)
        {
            Gizmos.DrawLine(new Vector3(laneX, -5f, 0f), new Vector3(laneX, transform.position.y, 0f));
        }
    }
#endif
}
