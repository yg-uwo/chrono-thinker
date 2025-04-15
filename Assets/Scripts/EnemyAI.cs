using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(Rigidbody2D))]
public class EnemyAI : MonoBehaviour 
{
    [Header("Movement Settings")]
    public float moveSpeed = 2f;
    
    [Header("Attack Settings")]
    public float attackDamage = 10f;            // Damage per attack
    public float attackCooldown = 2f;           // Time between attacks
    public float minimumDistanceToPlayer = 0.8f;  // Distance at which enemy stops moving
    public float attackDamageDelay = 0.3f;        // Delay before damage is applied to sync with swing

    private Rigidbody2D rb;
    private Transform player;
    private bool canAttack = true;
    private Animator animator;

    [Header("Obstacle Avoidance Settings")]
    public LayerMask obstacleLayerMask;
    public float obstacleDetectionDistance = 1.5f; // Increased from 1.0f
    public float rayAngleSpan = 120f;            // Increased from 90f for better obstacle detection
    public int rayCount = 5;                    // Increased from 3 for better obstacle detection
    public float avoidWeight = 2f;              // How strongly to avoid obstacles
    public float seekWeight = 1f;               // How strongly to pursue player

    [Header("Enemy Separation")]
    public float separationRadius = 1.5f;      // How far to check for other enemies
    public float separationWeight = 1.5f;      // How strongly to separate from other enemies
    public string enemyTag = "Enemy";          // Tag used to find other enemies

    [Header("Steering Smoothing")]
    [Range(0, 1)]
    public float smoothingFactor = 0.2f;        // 0 = no smoothing, 1 = instant change

    private Vector2 lastMovementDirection = Vector2.zero;
    private float raycastTimer = 0f;            // Timer for throttling raycasts
    private float raycastInterval = 0.1f;       // Reduced from 0.2f for more responsive obstacle detection
    private Vector2 cachedSteering = Vector2.zero;
    
    // Random offset to add variety to enemy movement
    private Vector2 randomOffset;
    private float randomOffsetTimer = 0f;
    private float randomOffsetInterval = 3f;
    private float randomOffsetStrength = 0.3f;
    
    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        
        // Configure Rigidbody2D for proper physics-based movement
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.sleepMode = RigidbodySleepMode2D.NeverSleep;
        rb.gravityScale = 0f; // No gravity for top-down movement
        rb.linearDamping = 3f; // Add some drag to prevent sliding
        rb.angularDamping = 0.1f;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation; // Prevent physics rotation
        
        // Make sure we have a collider for physics
        Collider2D collider = GetComponent<Collider2D>();
        if (collider == null)
        {
            Debug.LogError("Enemy missing Collider2D! Adding BoxCollider2D.");
            BoxCollider2D boxCollider = gameObject.AddComponent<BoxCollider2D>();
            // Size will use the default or match the sprite
        }
        else
        {
            // Make sure collider is not a trigger (so it physically blocks movement)
            collider.isTrigger = false;
        }
        
        // Ensure that we're using the right layers for obstacles
        if (obstacleLayerMask.value == 0)
        {
            Debug.LogWarning("Obstacle layer mask not set! Setting to default Obstacle layer.");
            obstacleLayerMask = LayerMask.GetMask("Obstacle");
        }
        
        animator = GetComponent<Animator>();
        
        // Find the player by tag
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            player = playerObj.transform;
        }
        else
        {
            Debug.LogError("Player not found! Ensure the player has the 'Player' tag.");
        }
        
        // Add a random offset to make each enemy behave slightly differently
        randomOffset = Random.insideUnitCircle.normalized * randomOffsetStrength;
    }
    
    void FixedUpdate()
    {
        if (player == null)
            return;
        
        // Update random offset occasionally
        randomOffsetTimer += Time.fixedDeltaTime;
        if (randomOffsetTimer >= randomOffsetInterval)
        {
            randomOffset = Random.insideUnitCircle.normalized * randomOffsetStrength;
            randomOffsetTimer = 0f;
        }
        
        Vector2 currentPosition = rb.position;
        Vector2 playerPos = (Vector2)player.position;
        
        // Check if there's a wall between enemy and player before pursuing
        bool canSeePlayer = !Physics2D.Linecast(currentPosition, playerPos, obstacleLayerMask);
        
        // Basic direction toward the player (with small random offset for variety)
        Vector2 desiredDirection = (playerPos - currentPosition).normalized + randomOffset;
        desiredDirection.Normalize();
        
        // Throttle raycasts for performance
        raycastTimer += Time.fixedDeltaTime;
        if (raycastTimer >= raycastInterval)
        {
            // Calculate obstacle avoidance
            Vector2 obstacleAvoidance = CalculateSteering(currentPosition, desiredDirection);
            
            // Calculate separation from other enemies
            Vector2 separationForce = CalculateSeparation(currentPosition);
            
            // Combine forces with weights
            cachedSteering = (obstacleAvoidance * avoidWeight + separationForce * separationWeight).normalized;
            
            // If no strong steering forces, go toward player
            if (cachedSteering.magnitude < 0.1f)
                cachedSteering = desiredDirection;
                
            raycastTimer = 0f;
        }
        
        // Use the cached steering direction
        Vector2 combinedForce = cachedSteering;
        
        // Apply smoothing
        if (lastMovementDirection == Vector2.zero)
            lastMovementDirection = combinedForce;
            
        combinedForce = Vector2.Lerp(lastMovementDirection, combinedForce, smoothingFactor);
        lastMovementDirection = combinedForce;
        
        // Rotate enemy to face movement direction
        if (combinedForce != Vector2.zero)
            transform.up = -combinedForce;
        
        // Calculate new position
        Vector2 proposedMovement = combinedForce * moveSpeed * Time.fixedDeltaTime;
        Vector2 newPos = currentPosition + proposedMovement;
        
        // Stop if too close to player
        if (Vector2.Distance(newPos, playerPos) < minimumDistanceToPlayer)
            newPos = currentPosition;
        
        // Check for collisions before moving
        RaycastHit2D hit = Physics2D.Linecast(currentPosition, newPos, obstacleLayerMask);
        if (hit.collider != null)
        {
            // If we would hit an obstacle, don't move or adjust movement
            // Option 1: Don't move at all (bumping into wall)
            newPos = currentPosition;
            
            // Option 2: Slide along the wall (uncomment this if you prefer sliding behavior)
            // Vector2 wallNormal = hit.normal;
            // Vector2 slideDirection = Vector2.Perpendicular(wallNormal) * Mathf.Sign(Vector2.Dot(combinedForce, Vector2.Perpendicular(wallNormal)));
            // newPos = currentPosition + slideDirection * moveSpeed * 0.5f * Time.fixedDeltaTime;
        }
        
        // Move the enemy
        rb.MovePosition(newPos);
        
        // Attack if within range AND no obstacles between enemy and player
        if (Vector2.Distance(currentPosition, playerPos) <= minimumDistanceToPlayer + 0.05f && canSeePlayer)
        {
            if (canAttack)
                Attack();
        }
    }
    
    private Vector2 CalculateSteering(Vector2 currentPosition, Vector2 desiredDirection)
    {
        // Check for stuck detection - cast rays in multiple directions
        bool stuckAgainstWall = IsStuckAgainstWall(currentPosition);
        
        // First, check straight ahead
        RaycastHit2D hitCenter = Physics2D.Raycast(currentPosition, desiredDirection, 
                                                obstacleDetectionDistance, obstacleLayerMask);
                                                
        if (hitCenter.collider == null && !stuckAgainstWall)
        {
            // Path is clear, head toward player
            return desiredDirection;
        }
        else
        {
            // Try left and right angles
            float angleStep = rayAngleSpan / (rayCount - 1);
            float bestAngle = 0f;
            float maxClearance = 0f;
            
            // If we're stuck, use a wider angle range to find an escape route
            float searchAngleSpan = stuckAgainstWall ? 270f : rayAngleSpan;
            int searchRayCount = stuckAgainstWall ? 8 : rayCount;
            
            for (int i = 0; i < searchRayCount; i++)
            {
                float angle = -searchAngleSpan/2 + i * (searchAngleSpan / (searchRayCount - 1));
                Vector2 rayDirection = Quaternion.Euler(0, 0, angle) * desiredDirection;
                
                // Cast a ray in this direction
                RaycastHit2D hit = Physics2D.Raycast(currentPosition, rayDirection, 
                                                   obstacleDetectionDistance, obstacleLayerMask);
                
                // Debug ray visualization
                Debug.DrawRay(currentPosition, rayDirection * obstacleDetectionDistance, 
                             hit.collider != null ? Color.red : Color.green, 0.1f);
                                                   
                // If clear or has more distance than previous best
                if (hit.collider == null || (hit.distance > maxClearance))
                {
                    maxClearance = hit.collider == null ? obstacleDetectionDistance : hit.distance;
                    bestAngle = angle;
                }
            }
            
            // Use the best angle for steering
            Vector2 steerDirection = Quaternion.Euler(0, 0, bestAngle) * desiredDirection;
            
            // If we're stuck, prioritize getting unstuck over heading toward the player
            if (stuckAgainstWall)
            {
                Debug.DrawLine(currentPosition, currentPosition + steerDirection * 2f, Color.yellow, 0.1f);
            }
            
            return steerDirection.normalized;
        }
    }
    
    private bool IsStuckAgainstWall(Vector2 currentPosition)
    {
        // Check if we're very close to walls in multiple directions
        int obstacleCount = 0;
        float shortDistance = obstacleDetectionDistance * 0.3f; // Very close distance check
        
        // Check in 4 directions (up, right, down, left)
        Vector2[] directions = new Vector2[] 
        {
            Vector2.up,
            Vector2.right,
            Vector2.down,
            Vector2.left
        };
        
        foreach (Vector2 dir in directions)
        {
            RaycastHit2D hit = Physics2D.Raycast(currentPosition, dir, shortDistance, obstacleLayerMask);
            if (hit.collider != null)
            {
                obstacleCount++;
                Debug.DrawRay(currentPosition, dir * shortDistance, Color.red, 0.1f);
            }
        }
        
        // If surrounded on 3+ sides, we're probably stuck in a corner
        return obstacleCount >= 3;
    }
    
    private Vector2 CalculateSeparation(Vector2 currentPosition)
    {
        Vector2 separationVector = Vector2.zero;
        int neighborCount = 0;
        
        // Find all enemies
        GameObject[] enemies = GameObject.FindGameObjectsWithTag(enemyTag);
        
        // Calculate separation vector
        foreach (GameObject enemy in enemies)
        {
            // Skip self
            if (enemy == gameObject)
                continue;
                
            float distance = Vector2.Distance(currentPosition, enemy.transform.position);
            
            // Only consider nearby enemies
            if (distance < separationRadius)
            {
                // Direction away from neighbor
                Vector2 awayVector = (currentPosition - (Vector2)enemy.transform.position).normalized;
                
                // Weigh by inverse distance (closer = stronger force)
                float weight = 1.0f - (distance / separationRadius);
                separationVector += awayVector * weight;
                
                neighborCount++;
            }
        }
        
        // Average the separation vector
        if (neighborCount > 0)
        {
            separationVector /= neighborCount;
            separationVector.Normalize();
        }
        
        return separationVector;
    }
    
    private void Attack()
    {
        Debug.Log("Enemy triggers sword swing animation!");
        
        // Double check that player is still visible before attacking
        Vector2 currentPosition = rb.position;
        Vector2 playerPos = (Vector2)player.position;
        bool canSeePlayer = !Physics2D.Linecast(currentPosition, playerPos, obstacleLayerMask);
        
        if (!canSeePlayer)
        {
            // Don't attack if player is behind obstacle
            return;
        }
        
        // Trigger the sword swing animation
        if (animator != null)
        {
            animator.SetTrigger("Attack");
        }
        else
        {
            Debug.LogWarning("Animator component not found on enemy!");
        }
        
        // Delay the damage to match the swing timing
        StartCoroutine(DealDamageAfterDelay());
        
        // Begin cooldown
        canAttack = false;
        StartCoroutine(AttackCooldown());
    }
    
    private IEnumerator DealDamageAfterDelay()
    {
        yield return new WaitForSeconds(attackDamageDelay);
        if (player != null)
        {
            // Final check before dealing damage - make sure player is still visible
            Vector2 currentPosition = rb.position;
            Vector2 playerPos = (Vector2)player.position;
            bool canSeePlayer = !Physics2D.Linecast(currentPosition, playerPos, obstacleLayerMask);
            
            if (!canSeePlayer)
            {
                // Player moved behind obstacle, don't deal damage
                yield break;
            }
            
            PlayerHealth playerHealth = player.GetComponent<PlayerHealth>();
            if (playerHealth != null)
            {
                playerHealth.TakeDamage(attackDamage);
            }
            else
            {
                Debug.LogWarning("PlayerHealth component not found on player.");
            }
        }
    }
    
    private IEnumerator AttackCooldown()
    {
        yield return new WaitForSeconds(attackCooldown);
        canAttack = true;
    }
}
