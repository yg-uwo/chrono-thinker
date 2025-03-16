// using UnityEngine;

// public class PlayerMovement : MonoBehaviour
// {
//     public float moveSpeed = 5f; // Speed of the player
//     public Vector2 boundaryMin; // Minimum boundary (bottom-left corner)
//     public Vector2 boundaryMax; // Maximum boundary (top-right corner)
    
//     private void Update()
//     {
//         // Get input from the player
//         float moveX = Input.GetAxis("Horizontal") * moveSpeed * Time.deltaTime;
//         float moveY = Input.GetAxis("Vertical") * moveSpeed * Time.deltaTime;
        
//         // Calculate new position - using Vector3 to match transform.position type
//         Vector3 newPosition = transform.position + new Vector3(moveX, moveY, 0);
        
//         // Clamp the position within boundaries
//         newPosition.x = Mathf.Clamp(newPosition.x, boundaryMin.x, boundaryMax.x);
//         newPosition.y = Mathf.Clamp(newPosition.y, boundaryMin.y, boundaryMax.y);
        
//         // Move the player
//         transform.position = newPosition;
//     }
// }

// using UnityEngine;

// public class SimplePlayerMovement : MonoBehaviour
// {
//     public float moveSpeed = 5f;
    
//     // Debug variables
//     public bool showDebugLogs = true;
    
//     void Update()
//     {
//         // Get direct keyboard input instead of Input axes
//         float horizontalInput = 0f;
//         float verticalInput = 0f;
        
//         // Check direct key presses
//         if (Input.GetKey(KeyCode.RightArrow) || Input.GetKey(KeyCode.D))
//             horizontalInput = 1f;
//         else if (Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.A))
//             horizontalInput = -1f;
            
//         if (Input.GetKey(KeyCode.UpArrow) || Input.GetKey(KeyCode.W))
//             verticalInput = 1f;
//         else if (Input.GetKey(KeyCode.DownArrow) || Input.GetKey(KeyCode.S))
//             verticalInput = -1f;
        
//         // Show debug logs if enabled
//         if (showDebugLogs && (horizontalInput != 0 || verticalInput != 0))
//         {
//             Debug.Log($"Input detected - Horizontal: {horizontalInput}, Vertical: {verticalInput}");
//         }
        
//         // Calculate movement
//         Vector3 movement = new Vector3(horizontalInput, verticalInput, 0f) * moveSpeed * Time.deltaTime;
        
//         // Apply movement directly to transform
//         transform.position += movement;
        
//         // Debug position after movement
//         if (showDebugLogs && movement.magnitude > 0)
//         {
//             Debug.Log($"New position: {transform.position}");
//         }
//     }
// }


using UnityEngine;

public class SimplePlayerMovement : MonoBehaviour
{
    public float moveSpeed = 5f;
    
    // Set these values in the Inspector based on your ground size
    public float minX = -4.5f;
    public float maxX = 4.5f;
    public float minY = -4.5f;
    public float maxY = 4.5f;
    
    // Freeze rotation flag
    public bool freezeRotation = true;
    
    private Rigidbody2D rb;
    
    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        
        // Disable rigidbody rotation if requested
        if (rb != null && freezeRotation)
        {
            rb.freezeRotation = true;
        }
        
        // Print a debug message to confirm script is running
        Debug.Log("SimplePlayerMovement script started");
    }
    
    void Update()
    {
        // Get input
        float horizontalInput = Input.GetAxis("Horizontal");
        float verticalInput = Input.GetAxis("Vertical");
        
        // Create movement vector
        Vector3 movement = new Vector3(horizontalInput, verticalInput, 0);
        
        // Normalize if moving diagonally
        if (movement.magnitude > 1)
        {
            movement.Normalize();
        }
        
        // Apply movement
        transform.position += movement * moveSpeed * Time.deltaTime;
        
        // Clamp position to stay within bounds
        float newX = Mathf.Clamp(transform.position.x, minX, maxX);
        float newY = Mathf.Clamp(transform.position.y, minY, maxY);
        transform.position = new Vector3(newX, newY, transform.position.z);
        
        // Debug output when moving
        if (horizontalInput != 0 || verticalInput != 0)
        {
            Debug.Log($"Input: H={horizontalInput}, V={verticalInput}, Position: {transform.position}");
        }
    }
}