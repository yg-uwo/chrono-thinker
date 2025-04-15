using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    // These public fields will be shown in inspector but will use GameSettings values by default
    public float moveSpeed = 5f;
    
    // Define your level boundaries
    public float minX = -4f;
    public float maxX = 4f;
    public float minY = -4f;
    public float maxY = 4f;
    
    // Option to freeze rotation
    public bool freezeRotation = true;
    
    private Rigidbody2D rb;
    private Vector2 movementInput;
    private Vector2 colliderExtents;  // We'll compute this from the BoxCollider2D
    
    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        if (rb != null && freezeRotation)
        {
            rb.freezeRotation = true;
        }
        
        // If you use a BoxCollider2D, calculate its extents (half the size, taking scale into account)
        BoxCollider2D box = GetComponent<BoxCollider2D>();
        if (box != null)
        {
            // Calculate extents in world space using local scale
            colliderExtents = new Vector2(box.size.x * Mathf.Abs(transform.localScale.x) / 2f,
                                          box.size.y * Mathf.Abs(transform.localScale.y) / 2f);
        }
        else
        {
            colliderExtents = Vector2.zero;
            Debug.LogWarning("BoxCollider2D not found on player!");
        }
        
        Debug.Log("PlayerMovement started. Collider extents: " + colliderExtents);
    }
    
    void Update()
    {
        float horizontalInput = Input.GetAxis("Horizontal");
        float verticalInput = Input.GetAxis("Vertical");
        movementInput = new Vector2(horizontalInput, verticalInput);
        
        if (movementInput.magnitude > 1)
        {
            movementInput.Normalize();
        }
    }
    
    void FixedUpdate()
    {
        if (rb != null)
        {
            // Calculate target position using physics-based movement
            Vector2 targetPosition = rb.position + movementInput * moveSpeed * Time.fixedDeltaTime;
            
            // Clamp the target so that the full collider remains inside the level boundaries.
            // We add colliderExtents.x/y to the min values and subtract from the max values.
            targetPosition.x = Mathf.Clamp(targetPosition.x, minX + colliderExtents.x, maxX - colliderExtents.x);
            targetPosition.y = Mathf.Clamp(targetPosition.y, minY + colliderExtents.y, maxY - colliderExtents.y);
            
            rb.MovePosition(targetPosition);
        }
    }
}
