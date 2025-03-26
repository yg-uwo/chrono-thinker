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
    private Vector2 movementInput;
    
    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        
        if (rb != null && freezeRotation)
        {
            rb.freezeRotation = true;
        }
        
        Debug.Log("SimplePlayerMovement script started");
    }
    
    void Update()
    {
        // Read input in Update
        float horizontalInput = Input.GetAxis("Horizontal");
        float verticalInput = Input.GetAxis("Vertical");
        
        movementInput = new Vector2(horizontalInput, verticalInput);
        
        // Normalize if moving diagonally
        if (movementInput.magnitude > 1)
        {
            movementInput.Normalize();
        }
    }
    
    void FixedUpdate()
    {
        if (rb != null)
        {
            // Calculate the target position based on input
            Vector2 targetPosition = rb.position + movementInput * moveSpeed * Time.fixedDeltaTime;
            
            // Clamp the position within the defined boundaries
            targetPosition.x = Mathf.Clamp(targetPosition.x, minX, maxX);
            targetPosition.y = Mathf.Clamp(targetPosition.y, minY, maxY);
            
            // Move the Rigidbody smoothly using MovePosition
            rb.MovePosition(targetPosition);
        }
    }
}
