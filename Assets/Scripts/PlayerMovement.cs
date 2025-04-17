using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    // These public fields will be shown in inspector but will use GameSettings values by default
    public float moveSpeed = 5f;
    
    // Option to freeze rotation
    public bool freezeRotation = true;
    
    private Rigidbody2D rb;
    private Vector2 movementInput;
    private Vector2 colliderExtents;  // We'll compute this from the BoxCollider2D
    private Bounds groundBounds;      // The bounds of the ground object
    
    // Knockback handling
    private bool isKnockedBack = false;
    private float knockbackRecoveryTime = 0.5f;
    private float knockbackTimer = 0f;
    
    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        if (rb != null && freezeRotation)
        {
            rb.freezeRotation = true;
        }
        
        // Set collision detection mode to continuous for better physics
        if (rb != null)
        {
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
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
        
        // Find and store the Ground object bounds
        FindGroundBounds();
        
        Debug.Log("PlayerMovement started. Collider extents: " + colliderExtents);
    }
    
    private void FindGroundBounds()
    {
        // Find the ground object in the scene
        GameObject ground = GameObject.Find("Ground");
        if (ground == null)
        {
            Debug.LogWarning("No 'Ground' object found in the scene! Using default level boundaries.");
            return;
        }
        
        // Get the collider or renderer bounds
        Collider2D groundCollider = ground.GetComponent<Collider2D>();
        if (groundCollider != null)
        {
            groundBounds = groundCollider.bounds;
            Debug.Log($"Found Ground object with bounds: {groundBounds}");
        }
        else 
        {
            SpriteRenderer renderer = ground.GetComponent<SpriteRenderer>();
            if (renderer != null)
            {
                groundBounds = renderer.bounds;
                Debug.Log($"Found Ground object with renderer bounds: {groundBounds}");
            }
            else
            {
                Debug.LogWarning("Ground object has no collider or renderer! Cannot determine bounds.");
            }
        }
    }
    
    void Update()
    {
        // Only process input if not knocked back
        if (!isKnockedBack)
        {
            float horizontalInput = Input.GetAxis("Horizontal");
            float verticalInput = Input.GetAxis("Vertical");
            movementInput = new Vector2(horizontalInput, verticalInput);
            
            if (movementInput.magnitude > 1)
            {
                movementInput.Normalize();
            }
        }
        else
        {
            // During knockback, don't accept user input
            knockbackTimer += Time.deltaTime;
            if (knockbackTimer >= knockbackRecoveryTime)
            {
                isKnockedBack = false;
            }
        }
    }
    
    void FixedUpdate()
    {
        if (rb != null)
        {
            // If knocked back, movement is controlled by physics forces
            if (!isKnockedBack)
            {
                // Calculate target position using physics-based movement
                Vector2 targetPosition = rb.position + movementInput * moveSpeed * Time.fixedDeltaTime;
                
                // Clamp the target so that the full collider remains inside the ground boundaries
                if (groundBounds.size != Vector3.zero)
                {
                    // Adjust the bounds to account for the player's size
                    Bounds adjustedBounds = new Bounds(groundBounds.center, groundBounds.size - new Vector3(colliderExtents.x * 2, colliderExtents.y * 2, 0));
                    
                    targetPosition.x = Mathf.Clamp(targetPosition.x, adjustedBounds.min.x, adjustedBounds.max.x);
                    targetPosition.y = Mathf.Clamp(targetPosition.y, adjustedBounds.min.y, adjustedBounds.max.y);
                }
                
                rb.MovePosition(targetPosition);
            }
            else
            {
                // During knockback, movement is handled by the applied knockback force
                // Just enforce level boundaries
                Vector2 clampedPosition = rb.position;
                
                if (groundBounds.size != Vector3.zero)
                {
                    // Adjust bounds for the player's size
                    Bounds adjustedBounds = new Bounds(groundBounds.center, groundBounds.size - new Vector3(colliderExtents.x * 2, colliderExtents.y * 2, 0));
                    
                    clampedPosition.x = Mathf.Clamp(clampedPosition.x, adjustedBounds.min.x, adjustedBounds.max.x);
                    clampedPosition.y = Mathf.Clamp(clampedPosition.y, adjustedBounds.min.y, adjustedBounds.max.y);
                }
                
                rb.position = clampedPosition;
            }
        }
    }
    
    // Public method to apply knockback from external sources (like the punching bag)
    public void ApplyKnockback(Vector2 force)
    {
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero; // Clear existing velocity
            rb.AddForce(force, ForceMode2D.Impulse);
            isKnockedBack = true;
            knockbackTimer = 0f;
        }
    }
    
    // Handle wall collisions with proper sliding behavior
    void OnCollisionEnter2D(Collision2D collision)
    {
        // Check if we're colliding with a wall, boundary, or obstacle
        bool isObstacle = collision.gameObject.layer == LayerMask.NameToLayer("Obstacle") ||
                          collision.gameObject.name.Contains("Ground") ||
                          collision.gameObject.name.Contains("Wall");
                          
        if (isObstacle)
        {
            // Get the contact point and normal
            ContactPoint2D contact = collision.GetContact(0);
            Vector2 normal = contact.normal;
            
            // Calculate the component of velocity along the normal
            float dotProduct = Vector2.Dot(rb.linearVelocity, normal);
            
            // Only zero out the velocity component in the direction of the wall
            if (dotProduct < 0) // Only if moving toward the wall
            {
                // Cancel out the velocity component towards the wall
                // This allows sliding along walls
                Vector2 cancelVelocity = normal * dotProduct;
                rb.linearVelocity -= cancelVelocity;
                
                // Add a slight velocity reduction on collision
                rb.linearVelocity *= 0.9f;
                
                Debug.Log($"Player hit obstacle/boundary - adjusted velocity: {rb.linearVelocity}");
            }
        }
    }
    
    // Handle continued contact with walls for smooth sliding
    void OnCollisionStay2D(Collision2D collision)
    {
        // Apply the same logic for continuous collisions
        OnCollisionEnter2D(collision);
    }
    
    // Handle leaving the ground
    void OnTriggerExit2D(Collider2D other)
    {
        // If we're exiting the ground, make sure we stay inside
        if (other.gameObject.name.Contains("Ground"))
        {
            // Force position update
            Vector3 position = transform.position;
            
            if (groundBounds.size != Vector3.zero)
            {
                // Adjust bounds for the player's size
                Bounds adjustedBounds = new Bounds(groundBounds.center, groundBounds.size - new Vector3(colliderExtents.x * 2, colliderExtents.y * 2, 0));
                
                position.x = Mathf.Clamp(position.x, adjustedBounds.min.x, adjustedBounds.max.x);
                position.y = Mathf.Clamp(position.y, adjustedBounds.min.y, adjustedBounds.max.y);
                
                // If the position was changed, adjust it
                if (position != transform.position)
                {
                    transform.position = position;
                    rb.linearVelocity = Vector2.zero; // Stop velocity to prevent bouncing off invisible walls
                    Debug.Log($"Player constrained within ground boundaries at {position}");
                }
            }
        }
    }
}
