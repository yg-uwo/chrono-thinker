using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;

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
    public float punchingBagAvoidWeight = 0.3f; // Reduced from 0.8f - less afraid of punching bag

    [Header("Enemy Separation")]
    public float separationRadius = 1.5f;      // How far to check for other enemies
    public float separationWeight = 1.5f;      // How strongly to separate from other enemies
    public string enemyTag = "Enemy";          // Tag used to find other enemies

    [Header("Steering Smoothing")]
    [Range(0, 1)]
    public float smoothingFactor = 0.2f;        // 0 = no smoothing, 1 = instant change

    // Added path commitment parameters
    [Header("Path Commitment")]
    [Range(0, 1)]
    public float pathCommitmentStrength = 0.6f; // How strongly to commit to chosen path
    public float pathChangeInterval = 1.2f;     // How often to allow major path changes
    private float lastPathChangeTime = 0f;      // Track when we last changed path
    private Vector2 committedDirection = Vector2.zero; // Currently committed direction

    private Vector2 lastMovementDirection = Vector2.zero;
    private float raycastTimer = 0f;            // Timer for throttling raycasts
    private float raycastInterval = 0.1f;       // Reduced from 0.2f for more responsive obstacle detection
    private Vector2 cachedSteering = Vector2.zero;
    
    // Random offset to add variety to enemy movement
    private Vector2 randomOffset;
    private float randomOffsetTimer = 0f;
    private float randomOffsetInterval = 3f;
    private float randomOffsetStrength = 0.3f;
    
    private Bounds groundBounds;
    private float disabledTime = 0f;
    private bool wasManuallyDisabled = false;

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
        randomOffsetTimer = Random.Range(0, randomOffsetInterval);
        
        // Get the ground bounds for level constraints
        FindGroundBounds();
    }
    
    void OnEnable()
    {
        // Reset flag when enabled
        wasManuallyDisabled = false;
    }

    void OnDisable()
    {
        // Record time when disabled
        disabledTime = Time.time;
        wasManuallyDisabled = true;
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
            
            // Calculate special punching bag avoidance
            Vector2 bagAvoidance = CalculatePunchingBagAvoidance(currentPosition);
            
            // Combine forces with weights, giving extra weight to punching bag avoidance
            cachedSteering = (obstacleAvoidance * avoidWeight + 
                             separationForce * separationWeight + 
                             bagAvoidance * punchingBagAvoidWeight).normalized;
            
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
    
    void Update()
    {
        // Emergency recovery from being disabled too long
        if (!enabled && wasManuallyDisabled && Time.time - disabledTime > 2.0f)
        {
            Debug.Log("EnemyAI emergency recovery from prolonged disabled state");
            enabled = true;
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

            // Check if we have a committed direction that still works
            bool canUseCommittedDirection = false;
            if (committedDirection.sqrMagnitude > 0.1f && Time.time - lastPathChangeTime < pathChangeInterval)
            {
                // Cast a ray along our committed direction
                RaycastHit2D hitCommitted = Physics2D.Raycast(currentPosition, committedDirection, 
                                                   obstacleDetectionDistance, obstacleLayerMask);
                
                // If the committed direction is still clear enough, keep using it
                if (hitCommitted.collider == null || hitCommitted.distance > obstacleDetectionDistance * 0.7f)
                {
                    canUseCommittedDirection = true;
                    
                    // Return the committed direction with a blend toward the player
                    return Vector2.Lerp(committedDirection, desiredDirection, 0.2f).normalized;
                }
            }
            
            // Clearance map to track the best angles
            List<KeyValuePair<float, float>> clearanceMap = new List<KeyValuePair<float, float>>();
            
            for (int i = 0; i < searchRayCount; i++)
            {
                float angle = -searchAngleSpan/2 + i * (searchAngleSpan / (searchRayCount - 1));
                Vector2 rayDirection = Quaternion.Euler(0, 0, angle) * desiredDirection;
                
                // Cast a ray in this direction
                RaycastHit2D hit = Physics2D.Raycast(currentPosition, rayDirection, 
                                                   obstacleDetectionDistance, obstacleLayerMask);
                
                // Calculate clearance - how far until we hit something
                float clearance = hit.collider == null ? obstacleDetectionDistance : hit.distance;
                
                // Store the angle and its clearance
                clearanceMap.Add(new KeyValuePair<float, float>(angle, clearance));
                
                // Debug ray visualization
                Debug.DrawRay(currentPosition, rayDirection * obstacleDetectionDistance, 
                             hit.collider != null ? Color.red : Color.green, 0.1f);
                                                   
                // Track the best angle seen so far
                if (clearance > maxClearance)
                {
                    maxClearance = clearance;
                    bestAngle = angle;
                }
            }
            
            // Sort clearance map by distance (descending)
            clearanceMap.Sort((a, b) => b.Value.CompareTo(a.Value));
            
            // Use the top 2 clearances to find a more stable path
            if (clearanceMap.Count >= 2)
            {
                // Get the two best clearances
                KeyValuePair<float, float> best = clearanceMap[0];
                KeyValuePair<float, float> secondBest = clearanceMap[1];
                
                // If they're both good and fairly close to each other
                if (best.Value > obstacleDetectionDistance * 0.7f && 
                    secondBest.Value > obstacleDetectionDistance * 0.5f &&
                    Mathf.Abs(best.Key - secondBest.Key) < 30f)
                {
                    // Average the two best angles for more stability
                    bestAngle = (best.Key + secondBest.Key) / 2f;
                }
            }
            
            // Use the best angle for steering
            Vector2 steerDirection = Quaternion.Euler(0, 0, bestAngle) * desiredDirection;
            
            // Commit to this path if it's significantly different from our current commitment
            // or if we haven't committed to a path in a while
            if (!canUseCommittedDirection && 
                (Vector2.Dot(steerDirection, committedDirection) < 0.7f || 
                 Time.time - lastPathChangeTime >= pathChangeInterval))
            {
                committedDirection = steerDirection;
                lastPathChangeTime = Time.time;
                Debug.DrawLine(currentPosition, currentPosition + steerDirection * 2f, Color.magenta, 0.5f);
            }
            
            // Blend between the committed direction and the new direction based on commitment strength
            Vector2 finalDirection;
            if (committedDirection.sqrMagnitude > 0.1f)
            {
                finalDirection = Vector2.Lerp(steerDirection, committedDirection, pathCommitmentStrength).normalized;
            }
            else
            {
                finalDirection = steerDirection;
            }
            
            // If we're stuck, prioritize getting unstuck over heading toward the player
            if (stuckAgainstWall)
            {
                Debug.DrawLine(currentPosition, currentPosition + finalDirection * 2f, Color.yellow, 0.1f);
                // Reset commitment when stuck to allow finding a new path
                committedDirection = Vector2.zero;
                lastPathChangeTime = 0f;
            }
            
            return finalDirection.normalized;
        }
    }
    
    private bool IsStuckAgainstWall(Vector2 currentPosition)
    {
        // Check if we're very close to walls in multiple directions
        int obstacleCount = 0;
        float shortDistance = obstacleDetectionDistance * 0.3f; // Very close distance check
        
        // Check in more directions (8 instead of 4) for better detection
        Vector2[] directions = new Vector2[] 
        {
            Vector2.up,
            Vector2.right,
            Vector2.down,
            Vector2.left,
            new Vector2(1, 1).normalized, // Up-right
            new Vector2(1, -1).normalized, // Down-right
            new Vector2(-1, -1).normalized, // Down-left
            new Vector2(-1, 1).normalized  // Up-left
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
        
        // If we're stuck in a corner or tight spot
        bool isStuck = obstacleCount >= 3;
        
        // If stuck, create a stronger random offset to break free
        if (isStuck)
        {
            randomOffset = Random.insideUnitCircle.normalized * (randomOffsetStrength * 3f);
            randomOffsetTimer = 0f; // Reset timer to keep this offset
        }
        
        return isStuck;
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
    
    private Vector2 CalculatePunchingBagAvoidance(Vector2 currentPosition)
    {
        Vector2 avoidanceForce = Vector2.zero;
        
        // Find all punching bags in the scene
        PunchingBag[] punchingBags = FindObjectsOfType<PunchingBag>();
        
        foreach (PunchingBag bag in punchingBags)
        {
            if (bag == null) continue;
            
            // Get distance to bag
            Vector2 bagPosition = bag.transform.position;
            float distanceToBag = Vector2.Distance(currentPosition, bagPosition);
            
            // Define a much smaller avoidance radius for bags
            float avoidanceRadius = 1.5f * bag.bagSize; // Reduced from 2.5f
            
            // Only avoid if bag is not at anchor AND moving
            bool bagAtRest = Vector2.Distance(bagPosition, bag.transform.parent.position) < 0.1f;
            Rigidbody2D bagRb = bag.GetComponent<Rigidbody2D>();
            bool bagIsMoving = bagRb != null && bagRb.linearVelocity.magnitude > 2.0f; // Only avoid if moving fast
            
            if (bagAtRest || !bagIsMoving) continue; // Don't avoid stationary or slow bags
            
            // If within avoidance radius, calculate avoidance force
            if (distanceToBag < avoidanceRadius)
            {
                // Direction away from bag
                Vector2 awayFromBag = (currentPosition - bagPosition).normalized;
                
                // Calculate the direction to the player
                Vector2 toPlayer = Vector2.zero;
                if (player != null)
                {
                    toPlayer = ((Vector2)player.position - currentPosition).normalized;
                }
                
                // Much lower avoidance strength if player is in roughly same direction as bag
                float directionDot = Vector2.Dot(awayFromBag, -toPlayer);
                float directionFactor = Mathf.Lerp(0.5f, 0.2f, Mathf.Max(0, directionDot));
                
                // Only strong avoidance when very close
                float avoidanceStrength = 1.0f - (distanceToBag / avoidanceRadius);
                avoidanceStrength = Mathf.Pow(avoidanceStrength, 3) * directionFactor; // Cubic falloff - much sharper dropoff
                
                // Add to overall avoidance force
                avoidanceForce += awayFromBag * avoidanceStrength;
                
                // If bag is moving fast, apply minimal predictive avoidance
                if (bagRb != null && bagRb.linearVelocity.magnitude > 5.0f)
                {
                    // Get predicted bag position based on velocity
                    Vector2 predictedBagPosition = bagPosition + bagRb.linearVelocity * 0.25f; // Reduced lookahead time
                    
                    // If predicted position is very close, create minimal avoidance
                    float predictedDistance = Vector2.Distance(currentPosition, predictedBagPosition);
                    if (predictedDistance < avoidanceRadius * 0.7f) // Reduced prediction radius
                    {
                        // Direction away from predicted position
                        Vector2 awayFromPredicted = (currentPosition - predictedBagPosition).normalized;
                        
                        // Much weaker avoidance for predicted positions
                        float predictiveStrength = 0.7f - (predictedDistance / (avoidanceRadius * 0.7f));
                        predictiveStrength *= bagRb.linearVelocity.magnitude * 0.1f; // Reduced to 0.1f
                        
                        // Add to overall avoidance
                        avoidanceForce += awayFromPredicted * predictiveStrength;
                    }
                }
            }
        }
        
        // Normalize if we have a significant avoidance force
        if (avoidanceForce.magnitude > 0.01f)
        {
            avoidanceForce.Normalize();
        }
        
        return avoidanceForce;
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
                // Apply damage to player - the player's TakeDamage will handle showing the indicator
                playerHealth.TakeDamage(attackDamage);
                Debug.Log($"EnemyAI: Dealt {attackDamage} damage to player");
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

    void OnCollisionEnter2D(Collision2D collision)
    {
        // Check if it's a punching bag, which should handle its own collision response
        if (collision.gameObject.GetComponent<PunchingBag>() != null)
        {
            // Reset path commitment when hit by a punching bag
            committedDirection = Vector2.zero;
            lastPathChangeTime = 0f;
            
            // Simply add a random offset and minimal recovery time
            // Do NOT disable the AI for long periods
            lastMovementDirection = Vector2.zero; // Reset direction to break out of any loops
            randomOffset = Random.insideUnitCircle.normalized * randomOffsetStrength * 3f;
            StartCoroutine(RecoverFromBagCollision(0.15f)); // Reduced from 0.5f
            return;
        }
        
        // Handle collision with walls and obstacles
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
            
            // Only zero out the velocity component in the direction of the wall if moving toward it
            if (dotProduct < 0)
            {
                // Cancel velocity toward the wall but keep velocity parallel to the wall
                Vector2 cancelVelocity = normal * dotProduct;
                rb.linearVelocity -= cancelVelocity;
                
                // Apply slight friction
                rb.linearVelocity *= 0.9f;
            }
            
            // Create a random offset to help break out of corners or tight spots
            randomOffset = Random.insideUnitCircle.normalized * (randomOffsetStrength * 2f);
            randomOffsetTimer = 0f; // Reset timer to keep this offset for a bit
            
            // Reset steering calculation to account for new situation
            raycastTimer = raycastInterval; // Force a recalculation on next frame
            
            // Reset path commitment when hitting an obstacle
            committedDirection = Vector2.zero;
            lastPathChangeTime = 0f;
            
            Debug.Log($"Enemy hit obstacle/boundary - adjusted velocity: {rb.linearVelocity}");
        }
    }
    
    // Preventing enemy wall clipping after punching bag hit
    private IEnumerator RecoverFromBagCollision(float recoveryTime)
    {
        // Don't completely disable the AI, just pause movement calculations
        bool canMove = true;
        bool canAttackSaved = canAttack;
        
        // Wait for a very short time only
        float actualRecoveryTime = Mathf.Min(recoveryTime, 0.2f);
        yield return new WaitForSeconds(actualRecoveryTime);
        
        // Check if we're inside a wall after getting hit
        bool insideWall = false;
        Collider2D[] overlaps = new Collider2D[5];
        ContactFilter2D filter = new ContactFilter2D();
        filter.SetLayerMask(obstacleLayerMask);
        filter.useLayerMask = true;
        
        // Get our collider
        Collider2D myCollider = GetComponent<Collider2D>();
        if (myCollider != null)
        {
            int count = Physics2D.OverlapCollider(myCollider, filter, overlaps);
            insideWall = count > 0;
            
            if (insideWall)
            {
                Debug.Log("Enemy detected inside wall after punching bag hit - repositioning");
                
                // Try to find a safe position
                Vector2 safePosition = FindSafePosition();
                
                if (safePosition != Vector2.zero)
                {
                    // Teleport to safe position
                    transform.position = safePosition;
                    rb.linearVelocity = Vector2.zero;
                }
            }
        }
        
        // Apply a force in direction toward player to help escape
        if (player != null)
        {
            Vector2 toPlayer = ((Vector2)player.position - (Vector2)transform.position).normalized;
            rb.AddForce(toPlayer * moveSpeed * 1.5f, ForceMode2D.Impulse);
        }
        
        // Generate a new random direction to help prevent getting stuck
        randomOffset = Random.insideUnitCircle.normalized * randomOffsetStrength * 2.5f;
        raycastTimer = raycastInterval; // Force a recalculation on next frame
        
        // Reset direction to prevent loops
        lastMovementDirection = Vector2.zero;
    }
    
    // New method to find a safe position when inside walls
    private Vector2 FindSafePosition()
    {
        // Try several directions to find a safe position
        Vector2[] directions = new Vector2[] 
        {
            Vector2.up,
            Vector2.right,
            Vector2.down,
            Vector2.left,
            new Vector2(1, 1).normalized,
            new Vector2(1, -1).normalized,
            new Vector2(-1, -1).normalized,
            new Vector2(-1, 1).normalized
        };
        
        Vector2 currentPos = transform.position;
        Vector2 bestPosition = Vector2.zero;
        float maxDistance = 0f;
        
        // Check each direction
        foreach (Vector2 dir in directions)
        {
            for (float distance = 0.5f; distance <= 3f; distance += 0.5f)
            {
                Vector2 testPosition = currentPos + dir * distance;
                
                // Check if this position is safe (not inside obstacles)
                Collider2D[] overlaps = Physics2D.OverlapCircleAll(testPosition, 0.4f, obstacleLayerMask);
                if (overlaps.Length == 0)
                {
                    // Position is safe - try to find one closest to player
                    if (player != null)
                    {
                        float distToPlayer = Vector2.Distance(testPosition, player.position);
                        if (distToPlayer > maxDistance)
                        {
                            maxDistance = distToPlayer;
                            bestPosition = testPosition;
                        }
                    }
                    else
                    {
                        // No player reference, just use the first safe position
                        return testPosition;
                    }
                }
            }
        }
        
        return bestPosition; // Will be zero if no safe position found
    }
    
    private void FindGroundBounds()
    {
        // Find the ground object in the scene
        GameObject ground = GameObject.Find("Ground");
        if (ground == null)
        {
            Debug.LogWarning("No 'Ground' object found in the scene! Level boundaries may not work correctly.");
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
    
    void LateUpdate()
    {
        // If we have ground bounds, make sure we stay within them
        if (groundBounds.size != Vector3.zero)
        {
            Vector3 position = transform.position;
            
            // Get bounds adjusted for this object's size
            Collider2D myCollider = GetComponent<Collider2D>();
            float boundsAdjustment = myCollider != null ? Mathf.Max(myCollider.bounds.extents.x, myCollider.bounds.extents.y) : 0.5f;
            
            // Adjust the bounds to account for the enemy size
            Bounds adjustedBounds = new Bounds(groundBounds.center, groundBounds.size - new Vector3(boundsAdjustment * 2, boundsAdjustment * 2, 0));
            
            // Check if we're outside bounds
            bool wasOutsideBounds = false;
            
            // Clamp position to be within bounds
            if (position.x < adjustedBounds.min.x)
            {
                position.x = adjustedBounds.min.x;
                wasOutsideBounds = true;
            }
            else if (position.x > adjustedBounds.max.x)
            {
                position.x = adjustedBounds.max.x;
                wasOutsideBounds = true;
            }
            
            if (position.y < adjustedBounds.min.y)
            {
                position.y = adjustedBounds.min.y;
                wasOutsideBounds = true;
            }
            else if (position.y > adjustedBounds.max.y)
            {
                position.y = adjustedBounds.max.y;
                wasOutsideBounds = true;
            }
            
            // If the position was changed, adjust it and reset velocity
            if (wasOutsideBounds)
            {
                transform.position = position;
                rb.linearVelocity = Vector2.zero; // Stop velocity to prevent bouncing off invisible walls
                
                // Generate a new random offset to move away from the boundary
                Vector2 toCenterDir = ((Vector2)adjustedBounds.center - (Vector2)position).normalized;
                randomOffset = toCenterDir * randomOffsetStrength * 2f;
                
                Debug.Log($"Enemy constrained within ground boundaries at {position}");
            }
        }
    }
    
    void OnTriggerExit2D(Collider2D other)
    {
        // If we're exiting the ground, make sure we stay inside
        if (other.gameObject.name.Contains("Ground"))
        {
            LateUpdate(); // Force position check
        }
    }
}
