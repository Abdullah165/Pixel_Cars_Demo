using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

/// <summary>
/// Controls the taxi with four swipe directions. Left/right change lane; up/down
/// make short forward/backward maneuvers for overtaking and undertaking.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class PlayerTaxiController : MonoBehaviour
{
    [Header("Two-Lane Road")]
    [SerializeField] private float[] laneXPositions = { -0.35f, 0.35f };
    [SerializeField] private int startingLane;

    [Header("Lane Switching")]
    [Tooltip("World units per second. The traffic planner automatically uses this and the lane-centre distance to calculate the guaranteed dodge window.")]
    [Min(0.1f)] [SerializeField] private float laneChangeSpeed = 5f;

    [Header("Overtake / Undertake")]
    [SerializeField] private float maneuverDistance = 1.15f;
    [SerializeField] private float maneuverSpeed = 5f;
    [SerializeField] private float minimumY = -4.6f;
    [SerializeField] private float maximumY = -1.8f;

    [Header("Brake Movement")]
    [Tooltip("How far the taxi visibly rolls back on screen when it reaches a full stop. Traffic continues at its normal speed.")]
    [Min(0f)] [SerializeField] private float maximumBrakeDrift = 1.2f;
    [Tooltip("How quickly the taxi moves toward its speed-derived screen position.")]
    [Min(0.1f)] [SerializeField] private float brakePositionResponse = 4f;

    [Header("Swipe")]
    [SerializeField] private float minimumSwipePixels = 48f;

    [Header("Events")]
    [SerializeField] private UnityEvent onCrash;

    [Header("Crash Restart")]
    [Tooltip("Real-time delay before the frozen level restarts.")]
    [Min(0f)] [SerializeField] private float restartDelay = 2f;

    private int currentLane;
    private float targetY;
    private Vector2 swipeStart;
    private bool swipeInProgress;
    private bool crashed;
    private bool brakeInput;

    public int CurrentLane => currentLane;
    public float LaneChangeSpeed => laneChangeSpeed;
    public float MaximumManeuverDistance => maneuverDistance;
    public bool IsBraking => GameSpeedController.IsBrakeHeld;
    public bool IsStopped => GameSpeedController.IsStopped;
    public float LaneChangeDuration
    {
        get
        {
            if (laneXPositions == null || laneXPositions.Length < 2 || laneChangeSpeed <= 0f)
            {
                return 0f;
            }

            return Mathf.Abs(laneXPositions[1] - laneXPositions[0]) / laneChangeSpeed;
        }
    }

    private void Start()
    {
        TrafficSpawner traffic = FindFirstObjectByType<TrafficSpawner>();
        if (traffic != null && traffic.LaneXPositions != null && traffic.LaneXPositions.Length >= 2)
        {
            laneXPositions = traffic.LaneXPositions;
        }

        currentLane = Mathf.Clamp(startingLane, 0, laneXPositions.Length - 1);
        targetY = transform.position.y;
        transform.position = new Vector3(laneXPositions[currentLane], targetY, transform.position.z);
    }

    private void Update()
    {
        if (crashed)
        {
            return;
        }

        ReadSwipeInput();
        ReadKeyboardInput();

        Vector3 position = transform.position;
        position.x = Mathf.MoveTowards(position.x, laneXPositions[currentLane], laneChangeSpeed * Time.deltaTime);
        // The taxi's speed is represented in world space by its position on the
        // screen: at a full stop it rolls backward, while traffic keeps coming.
        // Releasing the brake moves it back to the driving position.
        float brakeOffset = (1f - GameSpeedController.NormalizedTaxiSpeed) * maximumBrakeDrift;
        float speedAdjustedTargetY = Mathf.Max(minimumY, targetY - brakeOffset);
        float verticalResponse = Mathf.Max(maneuverSpeed, brakePositionResponse);
        position.y = Mathf.MoveTowards(position.y, speedAdjustedTargetY, verticalResponse * Time.deltaTime);
        transform.position = position;
    }

    public void ChangeLaneLeft()
    {
        currentLane = Mathf.Max(0, currentLane - 1);
    }

    public void ChangeLaneRight()
    {
        currentLane = Mathf.Min(laneXPositions.Length - 1, currentLane + 1);
    }

    public void Overtake()
    {
        targetY = Mathf.Min(maximumY, targetY + maneuverDistance);
    }

    public void Undertake()
    {
        targetY = Mathf.Max(minimumY, targetY - maneuverDistance);
    }

    public void SetBrakeInput(bool isBraking)
    {
        if (crashed)
        {
            return;
        }

        brakeInput = isBraking;
        GameSpeedController.SetBrakeHeld(brakeInput);
    }

    public void Crash()
    {
        if (crashed)
        {
            return;
        }

        crashed = true;
        brakeInput = false;
        GameSpeedController.SetBrakeHeld(false);
        onCrash?.Invoke();
        Debug.Log("Taxi crashed into traffic.", this);
        Time.timeScale = 0f;
        StartCoroutine(RestartSceneAfterCrash());
    }

    private IEnumerator RestartSceneAfterCrash()
    {
        yield return new WaitForSecondsRealtime(restartDelay);

        // WaitForSecondsRealtime keeps this timer running while timeScale is 0.
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    private void ReadSwipeInput()
    {
        if (Touchscreen.current != null)
        {
            var touch = Touchscreen.current.primaryTouch;
            if (touch.press.wasPressedThisFrame)
            {
                swipeStart = touch.position.ReadValue();
                swipeInProgress = true;
            }

            if (swipeInProgress && touch.press.wasReleasedThisFrame)
            {
                ProcessSwipe(touch.position.ReadValue() - swipeStart);
                swipeInProgress = false;
            }

            return;
        }

        if (Mouse.current == null)
        {
            return;
        }

        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            swipeStart = Mouse.current.position.ReadValue();
            swipeInProgress = true;
        }

        if (swipeInProgress && Mouse.current.leftButton.wasReleasedThisFrame)
        {
            ProcessSwipe(Mouse.current.position.ReadValue() - swipeStart);
            swipeInProgress = false;
        }
    }

    private void ReadKeyboardInput()
    {
        if (Keyboard.current == null)
        {
            return;
        }

        if (Keyboard.current.leftArrowKey.wasPressedThisFrame || Keyboard.current.aKey.wasPressedThisFrame) ChangeLaneLeft();
        if (Keyboard.current.rightArrowKey.wasPressedThisFrame || Keyboard.current.dKey.wasPressedThisFrame) ChangeLaneRight();
        if (Keyboard.current.upArrowKey.wasPressedThisFrame || Keyboard.current.wKey.wasPressedThisFrame) Overtake();
        if (Keyboard.current.downArrowKey.wasPressedThisFrame || Keyboard.current.sKey.wasPressedThisFrame) Undertake();
        if (Keyboard.current.spaceKey.wasPressedThisFrame) SetBrakeInput(true);
        if (Keyboard.current.spaceKey.wasReleasedThisFrame) SetBrakeInput(false);
    }

    private void ProcessSwipe(Vector2 swipe)
    {
        if (swipe.magnitude < minimumSwipePixels)
        {
            return;
        }

        if (Mathf.Abs(swipe.x) > Mathf.Abs(swipe.y))
        {
            if (swipe.x < 0f) ChangeLaneLeft(); else ChangeLaneRight();
        }
        else
        {
            if (swipe.y > 0f) Overtake(); else Undertake();
        }
    }
}
