using UnityEngine;

public enum PassengerState
{
    Walking,
    Waiting
}

/// <summary>A pooled pedestrian who can be collected only while the taxi is braking at the curb.</summary>
public class PassengerBehavior : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float sceneryScrollSpeed = 2f;
    [SerializeField] private float walkSpeed = 1.5f;
    [SerializeField] private float walkDuration = 4f;
    [SerializeField] private float despawnYLocation = -6f;

    private PassengerState currentState;
    private float stateTimer;
    private Animator animator;
    private PlayerTaxiController playerTaxi;
    private FareManager fareManager;

    private void Awake()
    {
        animator = GetComponent<Animator>();
    }

    private void OnEnable()
    {
        currentState = PassengerState.Walking;
        stateTimer = 0f;
        if (animator != null)
        {
            animator.SetBool("isWalking", true);
        }
    }

    private void Update()
    {
        bool worldStopped = GameSpeedController.IsWorldStopped;

        if (playerTaxi == null)
        {
            playerTaxi = FindFirstObjectByType<PlayerTaxiController>();
            fareManager = FindFirstObjectByType<FareManager>();
        }

        if (!worldStopped)
        {
            float currentSpeed = sceneryScrollSpeed;
            if (currentState == PassengerState.Walking)
            {
                currentSpeed -= walkSpeed;
                stateTimer += Time.deltaTime;

                if (stateTimer >= walkDuration)
                {
                    currentState = PassengerState.Waiting;
                    if (animator != null)
                    {
                        animator.SetBool("isWalking", false);
                    }
                }
            }

            transform.Translate(Vector3.down * currentSpeed * Time.deltaTime, Space.World);
        }

        if (currentState == PassengerState.Waiting && fareManager != null && playerTaxi != null)
        {
            fareManager.TryPickUp(this, playerTaxi);
        }

        if (transform.position.y < despawnYLocation)
        {
            ReturnToSpawnPool();
        }
    }

    public void ReturnToSpawnPool()
    {
        gameObject.SetActive(false);
    }
}
