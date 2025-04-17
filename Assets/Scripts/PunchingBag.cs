using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class PunchingBag : MonoBehaviour
{
    [Header("Physics Settings")]
    public float punchForce = 10f;
    public float returnSpeed = 5f;
    public float bagSize = 3.5f;
    public float bounceEnergyLoss = 0f;     // Changed to 0 - no energy loss when bouncing
    
    [Header("Combat Settings")]
    public float enemyDamage = 20f;            // Damage applied to enemies
    public float playerKnockbackForce = 5f;    // Force applied to player on hit
    public float enemyKnockbackForce = 8f;     // Force applied to enemies on hit
    public float maxPunchDistance = 2f;        // Maximum distance player can be to punch the bag
    
    [Header("Setup")]
    public Transform anchorPoint;              // Reference to the anchor point
    
    [Header("Visuals")]
    public Color bagColor = Color.red;         // Color of the punching bag
    public int sortingOrder = 10;              // Sorting order to ensure visibility
    
    private Rigidbody2D rb;
    private Vector3 anchorPos;
    private bool isPunched = false;
    private bool hasPassedAnchor = false;
    private HashSet<Collider2D> hitColliders = new HashSet<Collider2D>();
    
    // Collision detection 
    private float levelBoundPadding = 0.5f;    // Padding to keep from edge of level
    private Vector2 levelMin;                  // Min coordinates of level boundaries
    private Vector2 levelMax;                  // Max coordinates of level boundaries
    private bool boundsInitialized = false;    // Track if bounds are initialized
    
    // Variables to track collision state
    private Vector2 preCollisionVelocity;
    private bool isHandlingCollision = false;
    
    void Awake()
    {
        Debug.Log("PunchingBag Awake on " + gameObject.name);
        rb = GetComponent<Rigidbody2D>();
    }
    
    void Start()
    {
        Debug.Log("PunchingBag Start on " + gameObject.name);
        
        // Make sure we have a rigidbody
        if (rb == null)
        {
            rb = GetComponent<Rigidbody2D>();
        }
        
        // Basic physics setup
        rb.gravityScale = 0;
        rb.constraints = RigidbodyConstraints2D.FreezeAll; // Start fully frozen to prevent being pushed
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        
        // Store anchor position
        if (anchorPoint != null)
        {
            anchorPos = anchorPoint.position;
        }
        else
        {
            anchorPos = transform.position;
        }
        
        // Set initial position exactly at anchor
        transform.position = anchorPos;
        rb.linearVelocity = Vector2.zero;
        
        // Set scale
        transform.localScale = Vector3.one * bagSize;
        
        // Configure sprite and collider
        ConfigureColliders();
        MakeSpriteVisible();
        
        // Ensure it's on the Punchable layer
        if (LayerMask.NameToLayer("Punchable") != -1)
        {
            gameObject.layer = LayerMask.NameToLayer("Punchable");
        }
        
        // Initialize level bounds
        InitializeLevelBounds();
        
        // Unfreeze after a delay to ensure everything is set up properly
        StartCoroutine(UnfreezeAfterDelay());
    }
    
    private void InitializeLevelBounds()
    {
        // Try to find level bounds from GameManager, LevelManager, or similar
        // If not found, use a default area around the anchor
        boundsInitialized = true;
        
        // Use camera bounds directly since Wall tag is not defined
        Camera mainCamera = Camera.main;
        if (mainCamera != null)
        {
            float height = 2f * mainCamera.orthographicSize;
            float width = height * mainCamera.aspect;
            
            Vector3 camPos = mainCamera.transform.position;
            levelMin = new Vector2(camPos.x - width/2 + levelBoundPadding, camPos.y - height/2 + levelBoundPadding);
            levelMax = new Vector2(camPos.x + width/2 - levelBoundPadding, camPos.y + height/2 - levelBoundPadding);
            
            Debug.Log($"PunchingBag level bounds from camera: Min({levelMin.x}, {levelMin.y}), Max({levelMax.x}, {levelMax.y})");
        }
        else
        {
            // Last fallback - just use a reasonable area around the anchor
            levelMin = new Vector2(anchorPos.x - 10f, anchorPos.y - 10f);
            levelMax = new Vector2(anchorPos.x + 10f, anchorPos.y + 10f);
            Debug.Log($"PunchingBag using fallback level bounds around anchor: Min({levelMin.x}, {levelMin.y}), Max({levelMax.x}, {levelMax.y})");
        }
    }
    
    private IEnumerator UnfreezeAfterDelay()
    {
        // Wait for physics to settle
        yield return new WaitForSeconds(0.2f);
        
        // Only unfreeze rotation but keep position frozen until punched
        rb.constraints = RigidbodyConstraints2D.FreezePosition | RigidbodyConstraints2D.FreezeRotation;
    }
    
    private void ConfigureColliders()
    {
        // Get the sprite renderer to match collider size with sprite
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        float spriteRadius = 0.5f; // Default radius if no sprite
        
        // If we have a sprite, use its size to determine collider size
        if (sr != null && sr.sprite != null)
        {
            // Use the smaller dimension of the sprite
            spriteRadius = Mathf.Min(sr.sprite.bounds.extents.x, sr.sprite.bounds.extents.y);
        }
        
        Collider2D[] colliders = GetComponents<Collider2D>();
        
        // If we already have colliders, ensure they're not triggers
        if (colliders.Length > 0)
        {
            foreach (Collider2D col in colliders)
            {
                col.isTrigger = false;
                
                // Adjust size if it's a circle collider
                if (col is CircleCollider2D circleCol)
                {
                    // Match the sprite size exactly
                    circleCol.radius = spriteRadius;
                }
            }
        }
        else
        {
            // Add a circle collider if none exists
            CircleCollider2D newCollider = gameObject.AddComponent<CircleCollider2D>();
            newCollider.isTrigger = false;
            newCollider.radius = spriteRadius;
        }
    }
    
    private void MakeSpriteVisible()
    {
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            // Make sure color is opaque
            if (sr.color.a < 0.9f)
            {
                sr.color = new Color(bagColor.r, bagColor.g, bagColor.b, 1f);
            }
            
            // Set sorting order high
            sr.sortingOrder = sortingOrder;
        }
        else
        {
            // Add sprite renderer if missing
            sr = gameObject.AddComponent<SpriteRenderer>();
            sr.color = bagColor;
            sr.sortingOrder = sortingOrder;
            
            // Create a basic circle sprite if needed
            Sprite circleSprite = CreateCircleSprite();
            sr.sprite = circleSprite;
        }
    }
    
    private Sprite CreateCircleSprite()
    {
        // Create a texture for a simple circle
        int textureSize = 128;
        Texture2D texture = new Texture2D(textureSize, textureSize);
        
        // Fill with transparent pixels
        Color[] pixels = new Color[texture.width * texture.height];
        for (int i = 0; i < pixels.Length; i++)
        {
            pixels[i] = Color.clear;
        }
        
        // Draw a circle
        float radius = textureSize / 2f;
        Vector2 center = new Vector2(textureSize / 2f, textureSize / 2f);
        
        for (int y = 0; y < textureSize; y++)
        {
            for (int x = 0; x < textureSize; x++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), center);
                if (distance <= radius)
                {
                    pixels[y * textureSize + x] = Color.white;
                }
            }
        }
        
        texture.SetPixels(pixels);
        texture.Apply();
        
        // Create sprite from the texture
        return Sprite.Create(
            texture,
            new Rect(0, 0, textureSize, textureSize),
            new Vector2(0.5f, 0.5f), // Center pivot
            100f
        );
    }
    
    // Checks if the player is close enough to punch the bag
    public bool IsPlayerInPunchRange(Vector2 playerPosition)
    {
        float distance = Vector2.Distance(playerPosition, transform.position);
        return distance <= maxPunchDistance;
    }
    
    public void ApplyPunchForce(Vector2 direction, float power)
    {
        // Reset state and cancel any existing return coroutines
        StopAllCoroutines();
        isPunched = true;
        hasPassedAnchor = false;
        hitColliders.Clear();
            
        // Ensure we're at a valid position before punching
        if (Vector3.Distance(transform.position, anchorPos) > 1.0f)
        {
            transform.position = anchorPos;
        }
        
        // Enable physics and reset constraints to allow movement
        rb.isKinematic = false;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation; 
        
        // Log initial punch details
        Debug.Log($"[DIAGNOSTICS] Applying punch: direction={direction}, power={power}, position={transform.position}, anchor={anchorPos}");
        
        // Reduce the initial punch speed significantly to keep within level bounds
        float limitedPower = Mathf.Min(power, 5f); // Significantly reduced from 7f
        float limitedForce = Mathf.Min(punchForce, 5f); // Significantly reduced from 7f
        
        // Reset velocity and apply punch force
        rb.linearVelocity = Vector2.zero;
        rb.AddForce(direction * limitedPower * limitedForce, ForceMode2D.Impulse);
        
        // Start the returning process with bungee cord effect
        StartCoroutine(ReturnToAnchor());
    }
    
    // This is a stripped-down, simplified implementation for the punching bag
    // It trades some physics complexity for performance and stability
    private IEnumerator ReturnToAnchor()
    {
        // Wait a moment to let the bag move away from anchor
        yield return new WaitForSeconds(0.1f);
        
        float startTime = Time.time;
        bool hasOvershot = false;
        
        // Main movement loop with simplified physics
        while (isPunched && Time.time - startTime < 5f) // Shorter timeout for safety
        {
            // Simplified bounds enforcement
            Vector3 pos = transform.position;
            Camera mainCamera = Camera.main;
            if (mainCamera != null)
            {
                float halfHeight = mainCamera.orthographicSize;
                float halfWidth = halfHeight * mainCamera.aspect;
                
                // Simple bounds check with camera viewport
                pos.x = Mathf.Clamp(pos.x, -halfWidth + 0.5f, halfWidth - 0.5f);
                pos.y = Mathf.Clamp(pos.y, -halfHeight + 0.5f, halfHeight - 0.5f);
                transform.position = pos;
            }
            
            // Calculate direction to anchor
            Vector2 dirToAnchor = (Vector2)anchorPos - (Vector2)transform.position;
            float distToAnchor = dirToAnchor.magnitude;
            
            // Simple state detection for overshoot
            if (!hasOvershot && distToAnchor < 0.5f && 
                Vector2.Dot(rb.linearVelocity.normalized, dirToAnchor.normalized) < -0.5f)
            {
                hasOvershot = true;
                hasPassedAnchor = true;
                
                // Give a push in the current direction to ensure we overshoot properly
                rb.AddForce(rb.linearVelocity.normalized * 2f, ForceMode2D.Impulse);
                yield return new WaitForSeconds(0.1f); // Short wait to let it move past anchor
            }
            
            // Apply appropriate forces based on state
            if (hasOvershot)
            {
                // After overshoot, apply force toward anchor - stronger with distance
                float returnForce = 12f + distToAnchor * 6f;
                
                // Mix current velocity with desired direction for smoother transition
                Vector2 currentDir = rb.linearVelocity.normalized;
                Vector2 targetDir = dirToAnchor.normalized;
                
                // Calculate alignment with anchor direction
                float alignment = Vector2.Dot(currentDir, targetDir);
                
                // Use stronger correction when misaligned (reduces oscillation)
                float blendFactor = alignment < 0.8f ? 0.6f : 0.3f;
                
                // Apply damping to reduce oscillation
                rb.linearVelocity *= 0.9f;
                
                // Apply direct force toward anchor, stronger when misaligned
                rb.AddForce(targetDir * returnForce * (2.0f - alignment), ForceMode2D.Force);
                
                // If we're not moving fast enough or in wrong direction, correct more aggressively
                if (rb.linearVelocity.magnitude < 6f || alignment < 0.7f)
                {
                    // Direct velocity correction with stronger anchor bias
                    Vector2 newDir = Vector2.Lerp(currentDir, targetDir, blendFactor).normalized;
                    float speed = Mathf.Max(6f, rb.linearVelocity.magnitude);
                    rb.linearVelocity = newDir * speed;
                }
            }
            else
            {
                // Before overshooting, apply light elastic return force
                if (distToAnchor > 1.0f)
                {
                    // Stronger pull back when getting far from anchor
                    rb.AddForce(dirToAnchor.normalized * distToAnchor * 3f, ForceMode2D.Force);
                }
            }
            
            // If very close to anchor after overshoot, just snap
            if (hasOvershot && distToAnchor < 0.2f)
            {
                SnapToAnchor();
                break;
            }
            
            // Safety timeout per frame
            yield return null;
        }
        
        // Final safety check
        ResetPosition();
    }
    
    // Constrains the bag position to the level boundaries
    private void ConstrainToBounds()
    {
        if (!boundsInitialized)
        {
            InitializeLevelBounds();
        }
        
        Vector3 currentPosition = transform.position;
        bool wasConstrained = false;
        float currentSpeed = rb.linearVelocity.magnitude;
        
        // Check X boundaries
        if (currentPosition.x < levelMin.x)
        {
            currentPosition.x = levelMin.x;
            // Bounce back with NO energy loss
            if (rb.linearVelocity.x < 0)
            {
                rb.linearVelocity = new Vector2(-rb.linearVelocity.x, rb.linearVelocity.y).normalized * currentSpeed;
                wasConstrained = true;
            }
        }
        else if (currentPosition.x > levelMax.x)
        {
            currentPosition.x = levelMax.x;
            // Bounce back with NO energy loss
            if (rb.linearVelocity.x > 0)
            {
                rb.linearVelocity = new Vector2(-rb.linearVelocity.x, rb.linearVelocity.y).normalized * currentSpeed;
                wasConstrained = true;
            }
        }
        
        // Check Y boundaries
        if (currentPosition.y < levelMin.y)
        {
            currentPosition.y = levelMin.y;
            // Bounce back with NO energy loss
            if (rb.linearVelocity.y < 0)
            {
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, -rb.linearVelocity.y).normalized * currentSpeed;
                wasConstrained = true;
            }
        }
        else if (currentPosition.y > levelMax.y)
        {
            currentPosition.y = levelMax.y;
            // Bounce back with NO energy loss
            if (rb.linearVelocity.y > 0)
            {
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, -rb.linearVelocity.y).normalized * currentSpeed;
                wasConstrained = true;
            }
        }
        
        // Apply the constrained position
        if (wasConstrained)
        {
            Debug.Log("Punching bag constrained within level boundaries, maintaining speed: " + currentSpeed);
            transform.position = currentPosition;
            
            // If we've passed anchor, add an extra nudge toward the anchor
            if (hasPassedAnchor)
            {
                Vector2 toAnchor = (Vector2)anchorPos - (Vector2)transform.position;
                rb.AddForce(toAnchor.normalized * currentSpeed * 0.2f, ForceMode2D.Impulse);
            }
        }
    }
    
    // Helper to cleanly snap to anchor
    private void SnapToAnchor()
    {
        transform.position = anchorPos;
        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f;
        rb.constraints = RigidbodyConstraints2D.FreezePosition | RigidbodyConstraints2D.FreezeRotation;
        isPunched = false;
        hasPassedAnchor = false;
    }
    
    private void ResetPosition()
    {
        Debug.Log("Resetting punching bag position to anchor");
        
        // Cancel all forces and velocities first
        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f;
        
        // Then reposition
        transform.position = anchorPos;
        
        // Set state flags
        isPunched = false;
        hasPassedAnchor = false;
        
        // Ensure constraints are properly set
        rb.constraints = RigidbodyConstraints2D.FreezePosition | RigidbodyConstraints2D.FreezeRotation;
    }
    
    void OnCollisionEnter2D(Collision2D collision)
    {
        // Extremely simplified collision handling
        if (!collision.gameObject.CompareTag("Player") && !collision.gameObject.CompareTag("Enemy"))
        {
            if (isPunched)
            {
                // Ensure we have a valid contact point
                if (collision.contactCount == 0)
                {
                    // Fallback if we don't have contacts
                    Vector2 awayDir = (transform.position - collision.transform.position).normalized;
                    rb.AddForce(awayDir * 10f, ForceMode2D.Impulse);
                    return;
                }
                
                ContactPoint2D contact = collision.GetContact(0);
                
                // Safety check for NaN in normal
                if (float.IsNaN(contact.normal.x) || float.IsNaN(contact.normal.y))
                {
                    Debug.LogWarning("Invalid collision normal detected");
                    Vector2 awayDir = (transform.position - collision.transform.position).normalized;
                    rb.AddForce(awayDir * 10f, ForceMode2D.Impulse);
                    return;
                }
                
                // Record velocity before applying changes
                float speed = rb.linearVelocity.magnitude;
                Vector2 dirToAnchor = (Vector2)anchorPos - (Vector2)transform.position;
                
                // Simple reflection - use stored velocity if current is very low
                if (speed < 0.5f)
                {
                    // If speed is too low, use direction to or from anchor
                    Vector2 fallbackDir = hasPassedAnchor ? dirToAnchor.normalized : -dirToAnchor.normalized;
                    rb.linearVelocity = Vector2.Reflect(fallbackDir, contact.normal) * 5f;
                }
                else
                {
                    rb.linearVelocity = Vector2.Reflect(rb.linearVelocity, contact.normal);
                    // Make sure we maintain speed with slight boost
                    rb.linearVelocity = rb.linearVelocity.normalized * (speed * 1.2f);
                }
                
                // Apply stronger escape force perpendicular to the collision
                rb.AddForce(contact.normal * 7f, ForceMode2D.Impulse);
                
                // If we've already overshot, schedule delayed correction
                if (hasPassedAnchor)
                {
                    StartCoroutine(DelayedDirectionCorrection());
                }
            }
            return;
        }
        
        // For player/enemy collisions - simplified handling
        if (collision.gameObject.CompareTag("Player"))
        {
            // When punched, apply knockback to player
            if (isPunched)
            {
                Rigidbody2D playerRb = collision.gameObject.GetComponent<Rigidbody2D>();
                if (playerRb != null)
                {
                    Vector2 knockDir = (collision.transform.position - transform.position).normalized;
                    playerRb.AddForce(knockDir * playerKnockbackForce, ForceMode2D.Impulse);
                }
                
                // Add force to bag away from player to prevent sticking
                Vector2 escapeDir = (transform.position - collision.transform.position).normalized;
                rb.AddForce(escapeDir * 5f, ForceMode2D.Impulse);
            }
        }
        else if (collision.gameObject.CompareTag("Enemy"))
        {
            // Only apply effects if the bag is in motion 
            if (isPunched && !hitColliders.Contains(collision.collider))
            {
                hitColliders.Add(collision.collider);
                
                // Apply damage to enemy
                EnemyHealth enemy = collision.gameObject.GetComponent<EnemyHealth>();
                if (enemy != null)
                {
                    enemy.TakeDamage(enemyDamage);
                }
                
                // Apply knockback to enemy
                Rigidbody2D enemyRb = collision.gameObject.GetComponent<Rigidbody2D>();
                if (enemyRb != null)
                {
                    // Temporarily disable the enemy AI to ensure knockback works
                    EnemyAI enemyAI = collision.gameObject.GetComponent<EnemyAI>();
                    if (enemyAI != null)
                    {
                        enemyAI.enabled = false;
                        StartCoroutine(ReenableEnemyAI(enemyAI, 0.8f)); // Increased disable time
                    }
                    
                    // Calculate knockback vector
                    Vector2 knockDir = (collision.transform.position - transform.position).normalized;
                    
                    // Zero out current velocity and constraints
                    enemyRb.linearVelocity = Vector2.zero;
                    enemyRb.angularVelocity = 0f;
                    RigidbodyConstraints2D originalConstraints = enemyRb.constraints;
                    enemyRb.constraints = RigidbodyConstraints2D.None;
                    
                    // Apply a much stronger knockback force
                    float speedFactor = rb.linearVelocity.magnitude / 5f; // Scale with bag speed
                    float effectiveKnockbackForce = enemyKnockbackForce * (1f + speedFactor);
                    
                    // Multiple methods of applying force to ensure it works
                    enemyRb.AddForce(knockDir * effectiveKnockbackForce, ForceMode2D.Impulse);
                    enemyRb.linearVelocity = knockDir * effectiveKnockbackForce * 0.5f;
                    
                    // Direct position modification to ensure movement
                    StartCoroutine(EnsureEnemyKnockback(enemyRb, knockDir, effectiveKnockbackForce, originalConstraints));
                    
                    // Add force to bag away from enemy to prevent sticking
                    rb.AddForce(-knockDir * 5f, ForceMode2D.Impulse);
                }
            }
        }
    }
    
    // Helper method to re-enable enemy AI after knockback
    private IEnumerator ReenableEnemyAI(EnemyAI enemyAI, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (enemyAI != null)
        {
            enemyAI.enabled = true;
        }
    }
    
    // Helper method to correct direction after a collision
    private IEnumerator DelayedDirectionCorrection()
    {
        // Cancel any existing correction routines first
        StopCoroutine("DelayedDirectionCorrection");
        
        // Wait 3 frames to let physics fully resolve
        for (int i = 0; i < 3; i++)
        {
            yield return new WaitForFixedUpdate();
        }
        
        // Now correct direction toward anchor
        if (isPunched && hasPassedAnchor)
        {
            Vector2 dirToAnchor = (Vector2)anchorPos - (Vector2)transform.position;
            float distToAnchor = dirToAnchor.magnitude;
            
            // CORNER DETECTION: Check for nearby objects in a small radius
            Collider2D[] nearbyColliders = Physics2D.OverlapCircleAll(transform.position, 0.7f);
            bool nearObstacle = false;
            bool nearCorner = false;
            Vector2 cornerNormal = Vector2.zero;
            
            foreach (Collider2D col in nearbyColliders)
            {
                // Skip player, enemy and self
                if (col.gameObject.CompareTag("Player") || 
                    col.gameObject.CompareTag("Enemy") || 
                    col.gameObject == gameObject)
                {
                    continue;
                }
                
                nearObstacle = true;
                
                // Check if we're near a corner by raycasting in multiple directions
                Vector2[] checkDirections = new Vector2[] {
                    Vector2.up, Vector2.down, Vector2.left, Vector2.right,
                    new Vector2(1, 1).normalized, new Vector2(1, -1).normalized,
                    new Vector2(-1, 1).normalized, new Vector2(-1, -1).normalized
                };
                
                int hitCount = 0;
                foreach (Vector2 dir in checkDirections)
                {
                    RaycastHit2D hit = Physics2D.Raycast(transform.position, dir, 0.6f);
                    if (hit.collider != null && hit.collider.gameObject == col.gameObject)
                    {
                        hitCount++;
                        // Accumulate normals to find average direction away from corner
                        cornerNormal += hit.normal;
                    }
                }
                
                // If we have multiple hits on the same object from different directions, it's likely a corner
                if (hitCount >= 2)
                {
                    nearCorner = true;
                    cornerNormal.Normalize();
                    break;
                }
            }
            
            // Special handling for corners
            if (nearCorner && cornerNormal != Vector2.zero)
            {
                // Apply strong escape force away from corner
                rb.AddForce(cornerNormal * 12f, ForceMode2D.Impulse);
                
                // Wait a moment for the escape force to take effect
                yield return new WaitForFixedUpdate();
                yield return new WaitForFixedUpdate();
                
                // Then set velocity toward anchor with increased speed to escape the corner
                float returnSpeed = 18f + distToAnchor * 4f;
                rb.linearVelocity = dirToAnchor.normalized * returnSpeed;
            }
            else
            {
                // Normal obstacle case (not a corner)
                float returnSpeed = nearObstacle ? 
                    15f + distToAnchor * 3f :  // Stronger when near obstacle
                    10f + distToAnchor * 2f;   // Normal case
                    
                rb.linearVelocity = dirToAnchor.normalized * returnSpeed;
                
                // Apply additional escape impulse if near obstacle
                if (nearObstacle)
                {
                    rb.AddForce(dirToAnchor.normalized * 8f, ForceMode2D.Impulse);
                }
            }
        }
    }
    
    // Add continuous collision handling to prevent sticking
    void OnCollisionStay2D(Collision2D collision)
    {
        // Only handle non-player, non-enemy collisions to prevent sticking to walls
        if (isPunched && !collision.gameObject.CompareTag("Player") && !collision.gameObject.CompareTag("Enemy"))
        {
            // Track duration of continuous collision
            if (!isHandlingCollision)
            {
                isHandlingCollision = true;
                StartCoroutine(CollisionStickingTimeout());
            }
            
            // Get the contact point
            if (collision.contactCount > 0)
            {
                ContactPoint2D contact = collision.GetContact(0);
                
                // Check if collision normal is at an angle (likely a corner or diagonal surface)
                float angle = Vector2.Angle(contact.normal, Vector2.up);
                bool isAngled = (angle > 20 && angle < 70) || (angle > 110 && angle < 160);
                
                // Apply stronger force if colliding with an angled surface
                float forceMultiplier = isAngled ? 20f : 12f;
                rb.AddForce(contact.normal * forceMultiplier, ForceMode2D.Force);
                
                // If we're returning to anchor, apply additional force toward anchor
                if (hasPassedAnchor)
                {
                    Vector2 dirToAnchor = (Vector2)anchorPos - (Vector2)transform.position;
                    
                    // Check if anchor direction is not perpendicular to normal
                    float alignment = Vector2.Dot(dirToAnchor.normalized, contact.normal);
                    
                    // If we're on an angled surface AND the anchor is somewhat in the same direction,
                    // apply a large impulse to escape the corner trap
                    if (isAngled && alignment > 0.3f)
                    {
                        // Strong impulse to break out of corner
                        Vector2 escapeDir = (dirToAnchor.normalized + contact.normal).normalized;
                        rb.AddForce(escapeDir * 15f, ForceMode2D.Impulse);
                        StartCoroutine(DelayedDirectionCorrection()); // Reset direction afterward
                    }
                    else
                    {
                        // Normal case - just add force toward anchor
                        float forceMagnitude = alignment > 0 ? 10f : 5f;
                        rb.AddForce(dirToAnchor.normalized * forceMagnitude, ForceMode2D.Force);
                    }
                }
            }
        }
    }
    
    // Timeout handler for continuous collisions
    private IEnumerator CollisionStickingTimeout()
    {
        float collisionStartTime = Time.time;
        int stuckCount = 0;
        
        // Wait for a short period
        yield return new WaitForSeconds(0.3f);
        
        // If we're still handling collision after this time, force unstick
        if (isPunched && isHandlingCollision && Time.time - collisionStartTime > 0.25f)
        {
            stuckCount++;
            
            // Force a teleport toward anchor to unstick
            Vector2 dirToAnchor = (Vector2)anchorPos - (Vector2)transform.position;
            float distToAnchor = dirToAnchor.magnitude;
            
            // More aggressive repositioning - move 15% toward anchor instead of 10%
            transform.position = transform.position + (Vector3)(dirToAnchor.normalized * distToAnchor * 0.15f);
            
            // Apply strong force toward anchor
            rb.linearVelocity = dirToAnchor.normalized * 18f;
            
            // Special handling for repeated stuck scenarios (likely a corner trap)
            if (stuckCount >= 2)
            {
                // Cast rays to find likely corners
                RaycastHit2D[] hits = Physics2D.CircleCastAll(transform.position, 0.5f, Vector2.zero, 0.1f);
                if (hits.Length > 0)
                {
                    // More extreme teleport if we detect lots of contact points
                    transform.position = transform.position + (Vector3)(dirToAnchor.normalized * distToAnchor * 0.3f);
                    rb.linearVelocity = dirToAnchor.normalized * 20f;
                }
            }
        }
        
        // Reset collision handling flag
        isHandlingCollision = false;
    }
    
    // If we go off-screen, reset position
    void OnBecameInvisible()
    {
        if (isPunched && Vector2.Distance(transform.position, anchorPos) > 10f)
        {
            StopAllCoroutines();
            ResetPosition();
            StartCoroutine(ReturnToAnchor());
        }
    }
    
    void OnDrawGizmos()
    {
        // Show anchor position
        if (anchorPoint != null)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawSphere(anchorPoint.position, 0.2f);
        }
        
        // Show punch range
        Gizmos.color = new Color(1f, 0.5f, 0f, 0.3f); // Semi-transparent orange
        Gizmos.DrawWireSphere(transform.position, maxPunchDistance);
        
        // Show level bounds if initialized
        if (boundsInitialized)
        {
            Gizmos.color = new Color(0f, 1f, 0f, 0.3f); // Semi-transparent green
            Vector3 boundsCenter = new Vector3((levelMin.x + levelMax.x) / 2f, (levelMin.y + levelMax.y) / 2f, 0f);
            Vector3 boundsSize = new Vector3(levelMax.x - levelMin.x, levelMax.y - levelMin.y, 0.1f);
            Gizmos.DrawWireCube(boundsCenter, boundsSize);
        }
    }
    
    // Helper method to ensure enemy knockback happens
    private IEnumerator EnsureEnemyKnockback(Rigidbody2D enemyRb, Vector2 knockDir, float force, RigidbodyConstraints2D originalConstraints)
    {
        if (enemyRb == null) yield break;
        
        // Store initial position
        Vector2 startPos = enemyRb.position;
        
        // Apply movement over several frames to overcome any potential constraints
        for (int i = 0; i < 5; i++)
        {
            if (enemyRb == null) yield break;
            
            // Direct position manipulation each frame
            float moveAmount = force * 0.02f * (5-i); // Diminishing effect
            enemyRb.MovePosition(enemyRb.position + knockDir * moveAmount);
            
            // Re-apply velocity and force each frame
            enemyRb.linearVelocity = knockDir * force * 0.2f;
            enemyRb.AddForce(knockDir * force * 0.1f, ForceMode2D.Impulse);
            
            yield return new WaitForFixedUpdate();
        }
        
        // Wait before restoring constraints
        yield return new WaitForSeconds(0.3f);
        
        // Restore original constraints
        if (enemyRb != null)
        {
            enemyRb.constraints = originalConstraints;
        }
    }
} 