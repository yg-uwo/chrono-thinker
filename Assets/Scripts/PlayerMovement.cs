using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    public float moveSpeed = 5f; // Speed of the player
    public Vector2 boundaryMin; // Minimum boundary (bottom-left corner)
    public Vector2 boundaryMax; // Maximum boundary (top-right corner)

    private void Update()
    {
        // Get input from the player
        float moveX = Input.GetAxis("Horizontal") * moveSpeed * Time.deltaTime;
        float moveY = Input.GetAxis("Vertical") * moveSpeed * Time.deltaTime;

        // Calculate new position
        Vector2 newPosition = transform.position + new Vector3(moveX, moveY, 0);

        // Clamp the position within boundaries
        newPosition.x = Mathf.Clamp(newPosition.x, boundaryMin.x, boundaryMax.x);
        newPosition.y = Mathf.Clamp(newPosition.y, boundaryMin.y, boundaryMax.y);

        // Move the player
        transform.position = newPosition;
    }
}