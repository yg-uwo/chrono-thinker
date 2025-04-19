using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class PunchingBag : MonoBehaviour
{
    [Header("Physics Settings")]
    public float punchForce = 8f;             // Reduced from 10f
    public float returnSpeed = 3.5f;          // Reduced from 5f
    public float bagSize = 3.5f;
    public float bounceEnergyLoss = 0.2f;     // Added small energy loss for more stable bounce
    
    [Header("Combat Settings")]
    public float enemyDamage = 20f;            // Damage applied to enemies
    public float playerKnockbackForce = 3.5f;    // Force applied to player on hit (reduced from 5f)
    public float enemyKnockbackForce = 6.5f;     // Force applied to enemies on hit (increased from 5f)
    public float maxPunchDistance = 2f;        // Maximum distance player can be to punch the bag
    
    [Header("Charged Punch Settings")]
    public float minChargeDamageMultiplier = 0.5f;    // Multiplier for immediately released charged punches
    public float maxChargeDamageMultiplier = 1.75f;   // Multiplier for fully charged punches
    private float currentChargeFactor = 0f;           // Current charge factor (0-1) from the player's charged punch
    
    [Header("Setup")]
    public Transform anchorPoint;              // Reference to the anchor point
    
    [Header("Recovery Settings")]
    public float hoverTimeThreshold = 1.5f;   // Time in seconds before fixing hovering bag
    public float anchorSnapDistance = 0.3f;   // Distance threshold to snap to anchor
    public float minVelocityThreshold = 0.8f; // Min velocity to consider "hovering"
    
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
    
    private bool isProcessingPlayerNearAnchor = false;
    
    // List to track enemies waiting to be re-enabled
    private List<EnemyReenableData> enemiesWaitingToBeReenabled = new List<EnemyReenableData>();
    
    void Awake()
    {
        Debug.Log("PunchingBag Awake on " + gameObject.name);
        rb = GetComponent<Rigidbody2D>();
        
        // Ensure we're ignoring collisions with enemies and players by tag, not just layer
        // This is a more direct approach that doesn't rely on layer collision matrix
        IgnoreCollisionsWithTaggedObjects();
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
        
        // Configure physics interactions to ignore players and enemies
        Physics2D.IgnoreLayerCollision(
            LayerMask.NameToLayer("Punchable"), 
            LayerMask.NameToLayer("Player"), 
            true
        );
        
        Physics2D.IgnoreLayerCollision(
            LayerMask.NameToLayer("Punchable"), 
            LayerMask.NameToLayer("Enemy"), 
            true
        );
        
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
        
        // Find or create the main physics collider
        CircleCollider2D physicsCollider = null;
        
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
                    physicsCollider = circleCol;
                    // Match the sprite size exactly
                    physicsCollider.radius = spriteRadius;
                }
            }
        }
        
        // Add a circle collider if none exists
        if (physicsCollider == null)
        {
            physicsCollider = gameObject.AddComponent<CircleCollider2D>();
            physicsCollider.isTrigger = false;
            physicsCollider.radius = spriteRadius;
        }
        
        // Add a separate trigger collider for detecting player and enemy
        CircleCollider2D triggerCollider = gameObject.AddComponent<CircleCollider2D>();
        triggerCollider.isTrigger = true;
        triggerCollider.radius = spriteRadius * 1.05f; // Slightly larger than physics collider
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
        
        // Determine if this is a charged punch by comparing with the quick punch power
        PlayerPunching playerPunching = FindObjectOfType<PlayerPunching>();
        if (playerPunching != null)
        {
            float quickPunchPower = playerPunching.quickPunchPower;
            
            // If the power is greater than quick punch power, this is a charged punch
            if (power > quickPunchPower)
            {
                // Calculate charge factor (0-1) based on power range
                currentChargeFactor = Mathf.Clamp01((power - quickPunchPower) / 
                                               (playerPunching.maxChargedPunchPower - quickPunchPower));
                
                Debug.Log($"Charged punch detected! Charge factor: {currentChargeFactor}");
            }
            else
            {
                // Regular quick punch
                currentChargeFactor = 0f;
                Debug.Log("Quick punch detected");
            }
        }
        
        // Log initial punch details
        Debug.Log($"[DIAGNOSTICS] Applying punch: direction={direction}, power={power}, position={transform.position}, anchor={anchorPos}");
        
        // Reduce the initial punch speed significantly to keep within level bounds
        float limitedPower = Mathf.Min(power, 4f); // Reduced from 5f
        float limitedForce = Mathf.Min(punchForce, 4f); // Reduced from 5f
        
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
        float maxSpeed = 6.5f; // Slightly increased max speed cap
        float hoverStartTime = 0f; // Track when hovering started
        bool wasHovering = false;
        
        // Main movement loop with simplified physics
        while (isPunched && Time.time - startTime < 4f) // Shorter timeout for safety
        {
            // Calculate direction to anchor
            Vector2 dirToAnchor = (Vector2)anchorPos - (Vector2)transform.position;
            float distToAnchor = dirToAnchor.magnitude;
            
            // HOVERING DETECTION - Check if bag is hovering/stuck
            bool isHovering = rb.linearVelocity.magnitude < minVelocityThreshold && 
                             distToAnchor > anchorSnapDistance && 
                             hasPassedAnchor;
                             
            // Start tracking hover time when hovering begins
            if (isHovering && !wasHovering)
            {
                hoverStartTime = Time.time;
                wasHovering = true;
            }
            else if (!isHovering)
            {
                // Reset tracking if no longer hovering
                wasHovering = false;
                hoverStartTime = 0f;
            }
            
            // HOVERING RECOVERY - If hovering too long, force unstick
            if (wasHovering && Time.time - hoverStartTime > hoverTimeThreshold)
            {
                Debug.Log("Punching bag detected stuck/hovering - forcing return to anchor");
                
                // Strong direct force toward anchor to break hover state
                rb.linearVelocity = dirToAnchor.normalized * 9f;
                
                // If very close to anchor, just snap
                if (distToAnchor < anchorSnapDistance * 2)
                {
                    SnapToAnchor();
                    break;
                }
                
                // Reset hover tracking
                wasHovering = false;
                hoverStartTime = 0f;
            }
            
            // Cap velocity for stability, but allow faster return speed
            if (rb.linearVelocity.magnitude > maxSpeed)
            {
                // Allow slightly higher velocity when returning to anchor
                float speedCap = hasPassedAnchor ? maxSpeed : maxSpeed * 0.9f;
                rb.linearVelocity = rb.linearVelocity.normalized * speedCap;
            }
            
            // Simple state detection for overshoot
            if (!hasOvershot && distToAnchor < 0.5f && 
                Vector2.Dot(rb.linearVelocity.normalized, dirToAnchor.normalized) < -0.5f)
            {
                hasOvershot = true;
                hasPassedAnchor = true;
                
                // Give a gentle push in the current direction to ensure we overshoot properly
                rb.AddForce(rb.linearVelocity.normalized * 1f, ForceMode2D.Impulse);
                yield return new WaitForSeconds(0.1f); // Short wait to let it move past anchor
            }
            
            // Apply appropriate forces based on state
            if (hasOvershot)
            {
                // After overshoot, apply force toward anchor - stronger with distance
                // Increased base return force for more energetic return
                float returnForce = 10f + distToAnchor * 5f; // Increased from 8f and 4f
                
                // Anti-orbit logic - check if we're moving perpendicular to anchor direction
                Vector2 currentDir = rb.linearVelocity.normalized;
                Vector2 targetDir = dirToAnchor.normalized;
                
                // Calculate alignment with anchor direction
                float alignment = Vector2.Dot(currentDir, targetDir);
                
                // Detect potential orbiting (moving perpendicular to anchor direction)
                float perpAlignment = Vector2.Dot(currentDir, new Vector2(-targetDir.y, targetDir.x));
                bool isPotentiallyOrbiting = Mathf.Abs(perpAlignment) > 0.6f && distToAnchor > 1.0f;
                
                // Apply stronger correction when misaligned or orbiting
                float blendFactor = alignment < 0.7f || isPotentiallyOrbiting ? 0.6f : 0.3f; // Increased from 0.5f/0.25f
                
                // Apply damping - less damping for a more energetic return 
                rb.linearVelocity *= 0.85f; // Slightly less damping than 0.85f
                
                // Anti-orbiting - if we detect potential orbit, apply direct force toward anchor
                if (isPotentiallyOrbiting)
                {
                    // Strong direct force to break orbit
                    rb.AddForce(targetDir * returnForce * 1.5f, ForceMode2D.Impulse);
                    
                    // Directly manipulate velocity to point more toward anchor
                    Vector2 newDir = Vector2.Lerp(currentDir, targetDir, 0.5f).normalized;
                    float currentSpeed = rb.linearVelocity.magnitude;
                    rb.linearVelocity = newDir * currentSpeed;
                }
                else
                {
                    // Normal return behavior - apply direct force toward anchor, stronger when misaligned
                    rb.AddForce(targetDir * returnForce * (1.8f - alignment), ForceMode2D.Force); // Increased multiplier from 1.6f
                }
                
                // If we're not moving fast enough or in wrong direction, correct more aggressively
                if (rb.linearVelocity.magnitude < 4.5f || alignment < 0.7f) // Increased from 4f
                {
                    // Direct velocity correction with stronger anchor bias
                    Vector2 newDir = Vector2.Lerp(currentDir, targetDir, blendFactor).normalized;
                    float speed = Mathf.Min(maxSpeed, Mathf.Max(4.5f, rb.linearVelocity.magnitude)); // Slightly increased min speed
                    rb.linearVelocity = newDir * speed;
                }
            }
            else
            {
                // Before overshooting, apply light elastic return force
                if (distToAnchor > 1.0f)
                {
                    // Stronger pull back when getting far from anchor
                    rb.AddForce(dirToAnchor.normalized * distToAnchor * 2.2f, ForceMode2D.Force); // Increased from 2f
                }
            }
            
            // If very close to anchor after overshoot, just snap
            if (hasOvershot && distToAnchor < anchorSnapDistance)
            {
                SnapToAnchor();
                break;
            }
            
            // Special case: If very slow and close to anchor, just snap
            if (hasPassedAnchor && rb.linearVelocity.magnitude < 2f && distToAnchor < anchorSnapDistance * 2)
            {
                SnapToAnchor();
                break;
            }
            
            // Constrain to bounds every frame to prevent sticking at edges
            ConstrainToBounds();
            
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
    
    // For enemy/player hit detection
    void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            HandlePlayerCollision(collision);
        }
        else if (collision.CompareTag("Enemy"))
        {
            HandleEnemyCollision(collision);
        }
    }
    
    // Also add continuous collision checking to catch players standing near the anchor point
    void OnTriggerStay2D(Collider2D collision)
    {
        // Only process for player and only when bag is returning to anchor (has passed anchor)
        if (collision.CompareTag("Player") && isPunched && hasPassedAnchor)
        {
            // Check if player is near the anchor
            float playerDistToAnchor = Vector2.Distance(collision.transform.position, anchorPos);
            
            // Only handle players who are reasonably close to the anchor (within 3 units)
            if (playerDistToAnchor < 3.0f)
            {
                // Only trigger knockback if the bag is moving toward the anchor
                Vector2 bagDirection = rb.linearVelocity.normalized;
                Vector2 toAnchorDir = ((Vector2)anchorPos - (Vector2)transform.position).normalized;
                
                // If bag is moving toward anchor (dot product > 0) and has decent speed
                float movingTowardAnchor = Vector2.Dot(bagDirection, toAnchorDir);
                if (movingTowardAnchor > 0.3f && rb.linearVelocity.magnitude > 2.0f)
                {
                    // Reuse our player collision handler but with a small delay to avoid rapid knockbacks
                    if (!isProcessingPlayerNearAnchor)
                    {
                        StartCoroutine(ProcessPlayerNearAnchor(collision));
                    }
                }
            }
        }
    }
    
    private IEnumerator ProcessPlayerNearAnchor(Collider2D collision)
    {
        isProcessingPlayerNearAnchor = true;
        
        // Apply knockback to player near anchor
        HandlePlayerCollision(collision, true);
        
        // Small delay to prevent multiple knockbacks in quick succession
        yield return new WaitForSeconds(0.5f);
        
        isProcessingPlayerNearAnchor = false;
    }

    private void HandlePlayerCollision(Collider2D collision, bool isNearAnchor = false)
    {
        if (!isPunched) return;
        
        // Only apply knockback when:
        // 1. Initially punched and moving away from anchor (not hasPassedAnchor) OR
        // 2. Returning to anchor (hasPassedAnchor) OR
        // 3. Explicitly handling player near anchor case
        if (!hasPassedAnchor || hasPassedAnchor || isNearAnchor)
        {
            Rigidbody2D playerRb = collision.GetComponent<Rigidbody2D>();
            if (playerRb != null)
            {
                // Calculate if player is between bag and anchor
                Vector2 dirToAnchor = (Vector2)anchorPos - (Vector2)transform.position;
                Vector2 dirToPlayer = (Vector2)collision.transform.position - (Vector2)transform.position;
                float dotProduct = Vector2.Dot(dirToAnchor.normalized, dirToPlayer.normalized);
                
                // Player is between bag and anchor if dot product is positive (same direction)
                bool isPlayerInPath = dotProduct > 0.3f || isNearAnchor; 
                
                // Calculate knockback direction
                Vector2 knockDir;
                
                // Special case for player near anchor - knock away from anchor
                if (isNearAnchor)
                {
                    // When near anchor, knock player away from the anchor point
                    knockDir = ((Vector2)collision.transform.position - (Vector2)anchorPos).normalized;
                    
                    // If knockDir is zero (player directly on anchor), use a default direction
                    if (knockDir.sqrMagnitude < 0.1f)
                    {
                        // Use bag's direction as fallback
                        knockDir = rb.linearVelocity.normalized;
                    }
                }
                else
                {
                    // Normal case - knock player away from bag
                    knockDir = (collision.transform.position - transform.position).normalized;
                }
                
                // Apply more consistent knockback strength regardless of position
                // Use a consistent base multiplier with small adjustments for position
                float baseMultiplier = 1.0f;
                
                // Only small adjustments based on position
                float positionAdjustment = isPlayerInPath ? 0.2f : 0.0f;
                if (isNearAnchor)
                {
                    // Slightly increase near-anchor knockback as it was too weak
                    positionAdjustment = 0.3f;
                }
                
                // Final knockback multiplier is more consistent
                float knockbackMultiplier = baseMultiplier + positionAdjustment;
                
                // Scale knockback with bag speed but with tighter limits
                float speedFactor = Mathf.Clamp01(rb.linearVelocity.magnitude / 8f);
                float effectiveKnockback = playerKnockbackForce * knockbackMultiplier * (0.7f + speedFactor * 0.5f);
                
                // Cap knockback - slightly lower cap for more consistency
                float maxKnockbackForce = 6.5f;
                effectiveKnockback = Mathf.Min(effectiveKnockback, maxKnockbackForce);
                
                // Apply consistent minimum knockback to ensure it's never too weak
                effectiveKnockback = Mathf.Max(effectiveKnockback, 3.5f);
                
                // Apply knockback to player
                playerRb.AddForce(knockDir * effectiveKnockback, ForceMode2D.Impulse);
                
                // Briefly disable player movement for more reliable knockback, regardless of position
                PlayerMovement playerMovement = collision.GetComponent<PlayerMovement>();
                if (playerMovement != null)
                {
                    StartCoroutine(BrieflyDisablePlayerMovement(playerMovement, 0.12f));
                }
            }
        }
    }

    private void HandleEnemyCollision(Collider2D collision)
    {
        if (!isPunched || hitColliders.Contains(collision)) return;
        
        hitColliders.Add(collision);
        
        // Apply damage to enemy
        EnemyHealth enemy = collision.GetComponent<EnemyHealth>();
        if (enemy != null)
        {
            // Calculate actual damage based on punch velocity
            float damageMultiplier = Mathf.Clamp01(rb.linearVelocity.magnitude / 8f);
            
            // Apply charge factor to damage calculation
            float chargeDamageMultiplier;
            if (currentChargeFactor > 0f)
            {
                // Lerp between min and max charge multipliers based on charge factor
                chargeDamageMultiplier = Mathf.Lerp(minChargeDamageMultiplier, maxChargeDamageMultiplier, currentChargeFactor);
                Debug.Log($"Charged punch damage multiplier: {chargeDamageMultiplier} (Charge factor: {currentChargeFactor})");
            }
            else
            {
                // Regular quick punch - no additional multiplier (1.0)
                chargeDamageMultiplier = 1.0f;
            }
            
            // Calculate base damage with velocity factor
            float baseDamage = enemyDamage * (0.8f + damageMultiplier * 0.4f);
            
            // Apply charge multiplier 
            float actualDamage = baseDamage * chargeDamageMultiplier;
            
            // Round damage to nearest integer for cleaner display
            actualDamage = Mathf.Round(actualDamage);
            
            Debug.Log($"Enemy damage calculation: Base({baseDamage}) x ChargeMult({chargeDamageMultiplier}) = {actualDamage}");
            
            // Apply the damage
            enemy.TakeDamage(actualDamage);
        }
        
        // Apply knockback to enemy
        Rigidbody2D enemyRb = collision.GetComponent<Rigidbody2D>();
        if (enemyRb != null)
        {
            // Calculate if enemy is between bag and anchor
            Vector2 dirToAnchor = (Vector2)anchorPos - (Vector2)transform.position;
            Vector2 dirToEnemy = (Vector2)collision.transform.position - (Vector2)transform.position;
            float dotProduct = Vector2.Dot(dirToAnchor.normalized, dirToEnemy.normalized);
            bool isEnemyInPath = dotProduct > 0.3f;
            
            // Temporarily disable the enemy AI to ensure knockback works
            EnemyAI enemyAI = collision.GetComponent<EnemyAI>();
            if (enemyAI != null)
            {
                // Revert to original disable times
                float disableTime = isEnemyInPath ? 0.7f : 0.5f; 
                enemyAI.enabled = false;
                StartCoroutine(ReenableEnemyAI(enemyAI, disableTime));
            }
            
            // Calculate knockback vector
            Vector2 knockDir = (collision.transform.position - transform.position).normalized;
            
            // Zero out current velocity and constraints
            enemyRb.linearVelocity = Vector2.zero;
            enemyRb.angularVelocity = 0f;
            RigidbodyConstraints2D originalConstraints = enemyRb.constraints;
            enemyRb.constraints = RigidbodyConstraints2D.None;
            
            // Apply knockback force with moderately increased strength 
            float speedFactor = rb.linearVelocity.magnitude / 6f; // Back to original divisor
            float pathMultiplier = isEnemyInPath ? 1.15f : 0.95f; // Moderately increased
            float effectiveKnockbackForce = enemyKnockbackForce * (0.8f + speedFactor) * pathMultiplier;
            
            // Slightly increase the max cap for knockback force
            effectiveKnockbackForce = Mathf.Min(effectiveKnockbackForce, 7.0f); // Moderate increase
            
            // Apply knockback to enemy - with moderate impulse boost
            enemyRb.AddForce(knockDir * effectiveKnockbackForce * 1.05f, ForceMode2D.Impulse); // Moderate multiplier
            enemyRb.linearVelocity = knockDir * effectiveKnockbackForce * 0.3f; // Keep at adjusted value
            
            // Ensure enemy gets knocked back with wall collision checking
            StartCoroutine(EnsureEnemyKnockbackWithWallStop(enemyRb, knockDir, effectiveKnockbackForce, originalConstraints));
        }
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        // Only handle collisions with environment objects, not players or enemies
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
        }
    }
    
    // Simple helper to restore the bag's velocity after collision with player/enemy
    private IEnumerator RestoreBagVelocity(Vector2 originalVelocity)
    {
        // Wait for one physics frame to let Unity's collision resolution happen
        yield return new WaitForFixedUpdate();
        
        // Directly restore the original velocity
        if (isPunched)
        {
            rb.linearVelocity = originalVelocity;
            Debug.Log($"Restored bag velocity to original: {originalVelocity.magnitude}");
        }
    }
    
    // Helper method to re-enable enemy AI after knockback
    private IEnumerator ReenableEnemyAI(EnemyAI enemyAI, float delay)
    {
        if (enemyAI == null) yield break;
        
        // Store a reference to this enemy in case we need to re-enable it later
        EnemyReenableData enemyData = new EnemyReenableData
        {
            enemy = enemyAI,
            enableTime = Time.time + delay
        };
        
        // Add to our tracking list
        enemiesWaitingToBeReenabled.Add(enemyData);
        
        yield return new WaitForSeconds(delay);
        
        if (enemyAI != null)
        {
            enemyAI.enabled = true;
            // Remove from tracking list if successfully re-enabled
            enemiesWaitingToBeReenabled.Remove(enemyData);
        }
    }
    
    // Structure to store data about enemies waiting to be re-enabled
    private struct EnemyReenableData
    {
        public EnemyAI enemy;
        public float enableTime;
    }
    
    // Helper method to correct direction after a collision - updated to handle orbiting
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
            
            // Anti-orbit detection - check if we're moving perpendicular to anchor direction
            Vector2 currentVelocity = rb.linearVelocity;
            Vector2 normalizedVelocity = currentVelocity.normalized;
            Vector2 normalizedDirToAnchor = dirToAnchor.normalized;
            
            // Measure how perpendicular our movement is to the anchor direction (0 = aligned, 1 = perpendicular)
            float perpFactor = Vector2.Dot(normalizedVelocity, new Vector2(-normalizedDirToAnchor.y, normalizedDirToAnchor.x));
            bool isPerpendicular = Mathf.Abs(perpFactor) > 0.7f; // If movement is mostly perpendicular = orbiting
            
            // If we're orbiting, apply strong correction
            if (isPerpendicular && distToAnchor > 1.0f)
            {
                // Strong force toward anchor to break orbit
                rb.AddForce(normalizedDirToAnchor * 15f, ForceMode2D.Impulse);
                
                // Directly modify velocity to point more toward anchor
                Vector2 blendedDir = Vector2.Lerp(normalizedVelocity, normalizedDirToAnchor, 0.6f).normalized;
                float speed = rb.linearVelocity.magnitude * 1.1f; // Slightly boost speed for more energetic return
                rb.linearVelocity = blendedDir * speed;
                
                // Skip the rest of the processing as we've handled the orbit
                yield break;
            }
            
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
                    12f + distToAnchor * 2.5f;   // Normal case - increased from 10f/2f for more energy
                    
                rb.linearVelocity = dirToAnchor.normalized * returnSpeed;
                
                // Apply additional escape impulse if near obstacle
                if (nearObstacle)
                {
                    rb.AddForce(dirToAnchor.normalized * 8f, ForceMode2D.Impulse);
                }
            }
        }
    }
    
    // Improved collision handling to prevent sticking
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
                
                // Apply force perpendicular to the collision surface to slide along it
                float forceMultiplier = isAngled ? 12f : 8f; // Reduced from 20f and 12f
                rb.AddForce(contact.normal * forceMultiplier, ForceMode2D.Force);
                
                // If we're returning to anchor, apply additional force toward anchor
                if (hasPassedAnchor)
                {
                    Vector2 dirToAnchor = (Vector2)anchorPos - (Vector2)transform.position;
                    
                    // Check if anchor direction is not perpendicular to normal
                    float alignment = Vector2.Dot(dirToAnchor.normalized, contact.normal);
                    
                    // If we're on an angled surface AND the anchor is somewhat in the same direction,
                    // apply a gentler impulse to escape the corner trap
                    if (isAngled && alignment > 0.3f)
                    {
                        // Impulse to break out of corner, but gentler
                        Vector2 escapeDir = (dirToAnchor.normalized + contact.normal).normalized;
                        rb.AddForce(escapeDir * 10f, ForceMode2D.Impulse); // Reduced from 15f
                        StartCoroutine(DelayedDirectionCorrection()); // Reset direction afterward
                    }
                    else
                    {
                        // Normal case - just add force toward anchor
                        float forceMagnitude = alignment > 0 ? 7f : 3.5f; // Reduced from 10f and 5f
                        rb.AddForce(dirToAnchor.normalized * forceMagnitude, ForceMode2D.Force);
                    }
                }
                
                // Cap velocity to prevent excessive speed from collision resolution
                if (rb.linearVelocity.magnitude > 6f)
                {
                    rb.linearVelocity = rb.linearVelocity.normalized * 6f;
                }
            }
        }
    }
    
    // Timeout handler for continuous collisions - improved to fix jittering
    private IEnumerator CollisionStickingTimeout()
    {
        float collisionStartTime = Time.time;
        int stuckCount = 0;
        
        // Wait for a short period
        yield return new WaitForSeconds(0.2f); // Quicker reaction time
        
        // If we're still handling collision after this time, force unstick
        if (isPunched && isHandlingCollision && Time.time - collisionStartTime > 0.15f) // Reduced from 0.25f
        {
            stuckCount++;
            
            // Force a teleport toward anchor to unstick
            Vector2 dirToAnchor = (Vector2)anchorPos - (Vector2)transform.position;
            float distToAnchor = dirToAnchor.magnitude;
            
            // More aggressive repositioning for unsticking
            transform.position = transform.position + (Vector3)(dirToAnchor.normalized * distToAnchor * 0.2f); // Increased from 0.15f
            
            // Apply more gentle force toward anchor
            rb.linearVelocity = dirToAnchor.normalized * 12f; // Reduced from 18f
            
            // Special handling for repeated stuck scenarios (likely a corner trap)
            if (stuckCount >= 2)
            {
                // More extreme teleport if stuck repeatedly
                transform.position = transform.position + (Vector3)(dirToAnchor.normalized * distToAnchor * 0.35f); // Increased teleportation
                rb.linearVelocity = dirToAnchor.normalized * 8f; // Lower speed after teleport for stability
                
                // If stuck for a long time, consider just resetting to avoid frustration
                if (stuckCount >= 4)
                {
                    ResetPosition();
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
    
    // Reduced enemy knockback helper
    private IEnumerator EnsureEnemyKnockback(Rigidbody2D enemyRb, Vector2 knockDir, float force, RigidbodyConstraints2D originalConstraints)
    {
        if (enemyRb == null) yield break;
        
        // Store initial position
        Vector2 startPos = enemyRb.position;
        
        // Apply movement over several frames to overcome any potential constraints
        for (int i = 0; i < 5; i++)
        {
            if (enemyRb == null) yield break;
            
            // Direct position manipulation each frame (reduced)
            float moveAmount = force * 0.01f * (5-i); // Reduced from 0.015f to 0.01f
            enemyRb.MovePosition(enemyRb.position + knockDir * moveAmount);
            
            // Re-apply velocity and force each frame (reduced)
            enemyRb.linearVelocity = knockDir * force * 0.1f; // Reduced from 0.15f to 0.1f
            enemyRb.AddForce(knockDir * force * 0.05f, ForceMode2D.Impulse); // Reduced from 0.08f to 0.05f
            
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

    // New wall-stopping version of enemy knockback handler
    private IEnumerator EnsureEnemyKnockbackWithWallStop(Rigidbody2D enemyRb, Vector2 knockDir, float force, RigidbodyConstraints2D originalConstraints)
    {
        if (enemyRb == null) yield break;
        
        // Store initial position
        Vector2 startPos = enemyRb.position;
        
        // Track if the enemy has hit a wall
        bool hitWall = false;
        
        // Apply movement over a moderate number of frames (4 instead of original 3)
        for (int j = 0; j < 4; j++)
        {
            if (enemyRb == null || hitWall) yield break;
            
            // Check for walls before moving (slightly increased check distance)
            RaycastHit2D hit = Physics2D.Raycast(
                enemyRb.position,
                knockDir,
                0.8f, // Slightly increased from 0.6f
                LayerMask.GetMask("Obstacle", "Wall", "Ground")
            );
            
            if (hit.collider != null)
            {
                // Hit a wall - stop the knockback immediately
                Debug.Log("Enemy hit wall during knockback - stopping knockback");
                hitWall = true;
                
                // Position slightly away from the wall
                enemyRb.position = hit.point - (knockDir * 0.1f);
                
                // Stop movement
                enemyRb.linearVelocity = Vector2.zero;
                break;
            }
            
            // No wall hit, continue with knockback - modestly increased movement
            float moveAmountLimited = force * 0.01f * (4-j); // Slightly increased from 0.008f
            enemyRb.MovePosition(enemyRb.position + knockDir * moveAmountLimited);
            
            // Modest boost to knockback velocity and force
            enemyRb.linearVelocity = knockDir * force * 0.1f; // Slightly increased from 0.08f
            enemyRb.AddForce(knockDir * force * 0.04f, ForceMode2D.Impulse); // Slightly increased from 0.03f
            
            yield return new WaitForFixedUpdate();
        }
        
        // Apply a small final impulse after initial movement
        if (!hitWall && enemyRb != null)
        {
            enemyRb.AddForce(knockDir * force * 0.2f, ForceMode2D.Impulse);
        }
        
        // Wait a short time before restoring constraints
        yield return new WaitForSeconds(0.25f); // Slightly increased from 0.2f
        
        // Final wall check
        if (!hitWall)
        {
            // Continue checking for wall collisions after the initial knockback
            int safetyCount = 0;
            while (safetyCount < 10 && enemyRb != null)
            {
                safetyCount++;
                
                // Check if enemy is currently overlapping with any walls
                Collider2D enemyCollider = enemyRb.GetComponent<Collider2D>();
                bool insideWall = false;
                
                if (enemyCollider != null)
                {
                    ContactFilter2D filter = new ContactFilter2D();
                    filter.SetLayerMask(LayerMask.GetMask("Obstacle", "Wall", "Ground"));
                    filter.useLayerMask = true;
                    
                    Collider2D[] overlaps = new Collider2D[5];
                    int count = Physics2D.OverlapCollider(enemyCollider, filter, overlaps);
                    insideWall = count > 0;
                }
                
                // If inside a wall, pull back
                if (insideWall)
                {
                    // Get enemy AI component to use its FindSafePosition method
                    EnemyAI enemyAI = enemyRb.GetComponent<EnemyAI>();
                    if (enemyAI != null)
                    {
                        // Use reflection to access the private FindSafePosition method
                        System.Reflection.MethodInfo methodInfo = 
                            enemyAI.GetType().GetMethod("FindSafePosition", 
                            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                            
                        if (methodInfo != null)
                        {
                            object result = methodInfo.Invoke(enemyAI, null);
                            if (result is Vector2 safePos && safePos != Vector2.zero)
                            {
                                // Move to safe position
                                enemyRb.position = safePos;
                                enemyRb.linearVelocity = Vector2.zero;
                                break;
                            }
                        }
                    }
                    
                    // If we can't use FindSafePosition, try a simple pull-back
                    enemyRb.position = startPos;
                    enemyRb.linearVelocity = Vector2.zero;
                    break;
                }
                
                if (!insideWall)
                {
                    // If not inside a wall, check if about to hit a wall (slightly increased check)
                    RaycastHit2D wallCheck = Physics2D.Raycast(
                        enemyRb.position,
                        enemyRb.linearVelocity.normalized,
                        0.4f, // Slightly increased from 0.3f
                        LayerMask.GetMask("Obstacle", "Wall", "Ground")
                    );
                    
                    if (wallCheck.collider != null)
                    {
                        // About to hit a wall, stop all movement
                        enemyRb.linearVelocity = Vector2.zero;
                        break;
                    }
                }
                
                // If no issues detected, we're done
                if (!insideWall && enemyRb.linearVelocity.magnitude < 0.5f)
                {
                    break;
                }
                
                yield return new WaitForFixedUpdate();
            }
        }
        
        // Restore original constraints
        if (enemyRb != null)
        {
            enemyRb.constraints = originalConstraints;
        }
    }

    // This ensures the bag can't be pushed by player or enemies
    void FixedUpdate()
    {
        // If not punched, make sure it stays at anchor
        if (!isPunched && Vector3.Distance(transform.position, anchorPos) > 0.1f)
        {
            transform.position = anchorPos;
            rb.linearVelocity = Vector2.zero;
        }
        
        // Ensure constraints are properly maintained
        if (!isPunched)
        {
            rb.constraints = RigidbodyConstraints2D.FreezePosition | RigidbodyConstraints2D.FreezeRotation;
        }
        
        // FAILSAFE: Check for "hovering/stuck" state even outside of return coroutine
        if (isPunched && hasPassedAnchor && Time.frameCount % 30 == 0) // Only check every 30 frames for performance
        {
            float distToAnchor = Vector2.Distance(transform.position, anchorPos);
            bool isVelocityLow = rb.linearVelocity.magnitude < minVelocityThreshold;
            
            // If we're far enough from anchor but barely moving, we might be stuck
            if (isVelocityLow && distToAnchor > anchorSnapDistance)
            {
                // Log the issue
                Debug.Log($"Punching bag may be stuck: velocity={rb.linearVelocity.magnitude}, distance={distToAnchor}");
                
                // Try to resolve by forcing a return
                Vector2 dirToAnchor = (Vector2)anchorPos - (Vector2)transform.position;
                rb.linearVelocity = dirToAnchor.normalized * 8f;
                
                // If very close to anchor, just snap
                if (distToAnchor < anchorSnapDistance * 2)
                {
                    SnapToAnchor();
                }
            }
        }
        
        // Periodically update collision ignores for any new enemies spawned
        if (Time.frameCount % 60 == 0)  // Check every 60 frames
        {
            // Only enemies need to be updated as they might be spawned during gameplay
            GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
            Collider2D[] myColliders = GetComponents<Collider2D>();
            
            foreach (GameObject enemy in enemies)
            {
                Collider2D[] enemyColliders = enemy.GetComponents<Collider2D>();
                
                foreach (Collider2D myCollider in myColliders)
                {
                    if (myCollider.isTrigger) continue;
                    
                    foreach (Collider2D enemyCollider in enemyColliders)
                    {
                        if (!enemyCollider.isTrigger)
                        {
                            Physics2D.IgnoreCollision(myCollider, enemyCollider, true);
                        }
                    }
                }
            }
        }
        
        // FAILSAFE: Check for enemies that should be re-enabled but weren't
        if (Time.frameCount % 15 == 0) // Check every 15 frames
        {
            // Make a copy of the list to safely remove items during iteration
            var enemiesToCheck = new List<EnemyReenableData>(enemiesWaitingToBeReenabled);
            
            foreach (var enemyData in enemiesToCheck)
            {
                // If it's time to re-enable or past time
                if (Time.time >= enemyData.enableTime)
                {
                    if (enemyData.enemy != null)
                    {
                        enemyData.enemy.enabled = true;
                    }
                    enemiesWaitingToBeReenabled.Remove(enemyData);
                }
            }
        }
    }

    // Helper to briefly disable player movement for more reliable knockback
    private IEnumerator BrieflyDisablePlayerMovement(PlayerMovement playerMovement, float duration)
    {
        bool wasEnabled = playerMovement.enabled;
        playerMovement.enabled = false;
        
        yield return new WaitForSeconds(duration);
        
        if (playerMovement != null && wasEnabled)
        {
            playerMovement.enabled = true;
        }
    }

    // Directly ignore collisions with tagged objects to ensure physics doesn't affect trajectory
    private void IgnoreCollisionsWithTaggedObjects()
    {
        // Find all objects with Player or Enemy tag
        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        
        // Get all colliders on this object
        Collider2D[] myColliders = GetComponents<Collider2D>();
        
        // Ignore collisions with all player colliders
        foreach (GameObject player in players)
        {
            Collider2D[] playerColliders = player.GetComponents<Collider2D>();
            foreach (Collider2D myCollider in myColliders)
            {
                foreach (Collider2D playerCollider in playerColliders)
                {
                    if (!myCollider.isTrigger && !playerCollider.isTrigger)
                    {
                        Physics2D.IgnoreCollision(myCollider, playerCollider, true);
                        Debug.Log($"Ignoring collision between {gameObject.name} and {player.name}");
                    }
                }
            }
        }
        
        // Ignore collisions with all enemy colliders
        foreach (GameObject enemy in enemies)
        {
            Collider2D[] enemyColliders = enemy.GetComponents<Collider2D>();
            foreach (Collider2D myCollider in myColliders)
            {
                foreach (Collider2D enemyCollider in enemyColliders)
                {
                    if (!myCollider.isTrigger && !enemyCollider.isTrigger)
                    {
                        Physics2D.IgnoreCollision(myCollider, enemyCollider, true);
                        Debug.Log($"Ignoring collision between {gameObject.name} and {enemy.name}");
                    }
                }
            }
        }
    }
} 