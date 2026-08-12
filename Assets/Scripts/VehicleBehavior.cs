using UnityEngine;

public class VehicleBehavior : MonoBehaviour
{
    public float moveSpeed = 6f;
    public float despawnYLocation = -10f; // Y position where the car is considered off-screen

    void Update()
    {
        // Move downwards
        transform.Translate(Vector3.down * moveSpeed * Time.deltaTime);

        // Deactivate and return to pool if off-screen
        if (transform.position.y < despawnYLocation)
        {
            gameObject.SetActive(false);
        }
    }
}