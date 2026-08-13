using UnityEngine;

/// <summary>
/// Tracks the taxi's own driving speed. It deliberately does not change the
/// traffic, road, or pedestrians: they continue moving while the taxi brakes.
/// </summary>
public class GameSpeedController : MonoBehaviour
{
    [Header("Taxi Speed")]
    [Min(0.1f)] [SerializeField] private float maxTaxiSpeed = 1f;
    [Tooltip("Taxi speed lost per second while the brake is held.")]
    [Min(0.01f)] [SerializeField] private float brakeDeceleration = 1.5f;
    [Tooltip("Taxi speed regained per second after releasing the brake.")]
    [Min(0.01f)] [SerializeField] private float acceleration = 1.2f;
    [Tooltip("Pickups and drop-offs are allowed at or below this speed.")]
    [Min(0f)] [SerializeField] private float stoppedSpeedThreshold = 0.02f;

    private static GameSpeedController instance;
    private bool brakeHeld;
    private float currentTaxiSpeed;

    public static bool IsBrakeHeld => instance != null && instance.brakeHeld;
    public static float CurrentTaxiSpeed => instance != null ? instance.currentTaxiSpeed : 1f;
    public static float NormalizedTaxiSpeed => instance == null || instance.maxTaxiSpeed <= 0f ? 1f : instance.currentTaxiSpeed / instance.maxTaxiSpeed;
    public static bool IsStopped => instance != null && instance.currentTaxiSpeed <= instance.stoppedSpeedThreshold;

    private void Awake()
    {
        instance = this;
        currentTaxiSpeed = maxTaxiSpeed;
    }

    private void Update()
    {
        float targetSpeed = brakeHeld ? 0f : maxTaxiSpeed;
        float changeRate = brakeHeld ? brakeDeceleration : acceleration;
        currentTaxiSpeed = Mathf.MoveTowards(currentTaxiSpeed, targetSpeed, changeRate * Time.deltaTime);
    }

    public static void SetBrakeHeld(bool value)
    {
        if (instance != null)
        {
            instance.brakeHeld = value;
        }
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }
    }
}
