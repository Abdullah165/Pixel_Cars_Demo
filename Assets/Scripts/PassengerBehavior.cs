using UnityEngine;

public enum PassengerState 
{ 
    Walking, 
    Waiting, 
    PickedUp 
}

public class PassengerBehavior : MonoBehaviour
{
    [Header("Movement Settings")]
    public float sceneryScrollSpeed = 5f; 
    public float walkSpeed = 1.5f; 
    public float walkDuration = 2f; 
    public float despawnYLocation = -10f;

    private PassengerState currentState;
    private float stateTimer;
    private Animator animator;

    void Awake()
    {
        animator = GetComponent<Animator>();
    }

    // OnEnable runs the moment the passenger is spawned by your object pool
    void OnEnable()
    {
        currentState = PassengerState.Walking;
        stateTimer = 0f;
        
        // 1. AUTOMATICALLY start the Walking animation
        if (animator != null)
        {
            animator.SetBool("isWalking", true);
        }
    }

    void Update()
    {
        if (currentState == PassengerState.PickedUp) return;

        float currentSpeed = sceneryScrollSpeed;

        if (currentState == PassengerState.Walking)
        {
            // Subtract walk speed so they walk down the screen slower than the trees
            currentSpeed -= walkSpeed; 
            
            stateTimer += Time.deltaTime;
            
            // When the walk duration timer finishes...
            if (stateTimer >= walkDuration)
            {
                // Switch the internal state
                currentState = PassengerState.Waiting;
                
                // 2. AUTOMATICALLY switch to the Waiting animation
                if (animator != null)
                {
                    animator.SetBool("isWalking", false);
                }
            }
        }

        // Move the passenger downward
        transform.Translate(Vector3.down * currentSpeed * Time.deltaTime);

        // Despawn and return to the pool when they go off the bottom of the screen
        if (transform.position.y < despawnYLocation)
        {
            gameObject.SetActive(false);
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player") && currentState == PassengerState.Waiting)
        {
            HandlePickup();
        }
    }

    void HandlePickup()
    {
        currentState = PassengerState.PickedUp;
        gameObject.SetActive(false); 
    }
}