
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
        
    }
}