using UnityEngine;

public class RoadScroller : MonoBehaviour
{
    [Header("Settings")]
    public float scrollSpeed = 5f;
    public float roadHeight = 10f; // Set this to the exact vertical size of your road sprite

    [Header("References")]
    public Transform road1;
    public Transform road2;

    void Update()
    {
        if (GameSpeedController.IsWorldStopped)
        {
            return;
        }

        // Move both road parts down
        road1.Translate(Vector3.down * scrollSpeed * Time.deltaTime);
        road2.Translate(Vector3.down * scrollSpeed * Time.deltaTime);

        // Reposition road1 to the top when it goes off-screen
        if (road1.position.y < -roadHeight)
        {
            road1.position = new Vector2(road1.position.x, road2.position.y + roadHeight);
        }

        // Reposition road2 to the top when it goes off-screen
        if (road2.position.y < -roadHeight)
        {
            road2.position = new Vector2(road2.position.x, road1.position.y + roadHeight);
        }
    }
}
