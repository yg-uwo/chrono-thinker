using UnityEngine;
using System.Collections;

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
    public float obstacleDetectionDistance = 1f; // used by whisker raycasts
    public int rayCount = 5;
    public float rayAngleSpan = 90f;
    public float rayLength = 2f;
    public float avoidWeight = 2f;
    public float seekWeight = 1f;

    // New fields for radial repulsion:
    public float obstacleAvoidanceRadius = 0.5f;  // the enemy will try to keep this distance from any obstacle
    public float obstacleRepulsionWeight = 3f;    // weight for the repulsion force

    [Header("Steering Smoothing")]
    [Range(0, 1)]
    public float smoothingFactor = 0.2f;            // 0 = no smoothing, 1 = instant change

    public float attackTolerance = 0.05f;           // extra tolerance for triggering an attack


    private Vector2 lastMovementDirection = Vector2.zero;

    
    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        // Prevent the rigidbody from sleeping so it always updates, even when stationary.
        rb.sleepMode = RigidbodySleepMode2D.NeverSleep;
        
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
    }
    
    void FixedUpdate()
    {
        if (player == null)
            return;
        
        Vector2 currentPosition = rb.position;
        Vector2 playerPos = (Vector2)player.position;
        // Basic direction toward the player.
        Vector2 desiredDirection = (playerPos - currentPosition).normalized;

        // --- WHISKER-BASED OBSTACLE AVOIDANCE ---
        float detectionDistance = obstacleDetectionDistance;
        Vector2 whiskerDirection = desiredDirection; // default: straight toward player
        
        // Cast a ray straight ahead.
        RaycastHit2D hitCenter = Physics2D.Raycast(currentPosition, desiredDirection, detectionDistance, obstacleLayerMask);
        if (hitCenter.collider != null)
        {
            // If something is directly ahead, check at an offset.
            float steerAngle = 30f; // degrees offset
            Vector2 leftDir = Quaternion.Euler(0, 0, steerAngle) * desiredDirection;
            Vector2 rightDir = Quaternion.Euler(0, 0, -steerAngle) * desiredDirection;
            
            RaycastHit2D hitLeft = Physics2D.Raycast(currentPosition, leftDir, detectionDistance, obstacleLayerMask);
            RaycastHit2D hitRight = Physics2D.Raycast(currentPosition, rightDir, detectionDistance, obstacleLayerMask);
            
            bool leftClear = (hitLeft.collider == null);
            bool rightClear = (hitRight.collider == null);
            
            if (leftClear && !rightClear)
                whiskerDirection = leftDir;
            else if (rightClear && !leftClear)
                whiskerDirection = rightDir;
            else if (leftClear && rightClear)
                whiskerDirection = leftDir; // choose arbitrarily
            else
            {
                // Both sides blocked: choose the one with the longer free distance.
                whiskerDirection = (hitLeft.distance > hitRight.distance) ? leftDir : rightDir;
            }
        }
        // --- END WHISKER AVOIDANCE ---

        // --- RADIAL REPULSION FROM OBSTACLES ---
        Vector2 repulsionForce = Vector2.zero;
        Collider2D[] nearbyObstacles = Physics2D.OverlapCircleAll(currentPosition, obstacleAvoidanceRadius, obstacleLayerMask);
        foreach (Collider2D col in nearbyObstacles)
        {
            Vector2 closestPoint = col.ClosestPoint(currentPosition);
            float dist = Vector2.Distance(currentPosition, closestPoint);
            if (dist < obstacleAvoidanceRadius && dist > 0)
            {
                // The closer, the stronger the repulsion.
                float repulsionStrength = (obstacleAvoidanceRadius - dist) / obstacleAvoidanceRadius;
                repulsionForce += (currentPosition - closestPoint).normalized * repulsionStrength;
            }
        }
        // --- END RADIAL REPULSION ---

        // Combine the whisker direction and the repulsion force.
        Vector2 combinedForce = (whiskerDirection * seekWeight) + (repulsionForce * obstacleRepulsionWeight);
        if (combinedForce != Vector2.zero)
            combinedForce.Normalize();

        // --- SMOOTH THE DECISION ---
        // Use a field to store last frame's movement direction.
        // (Declare this as a private field in your class: private Vector2 lastMovementDirection;)
        if (lastMovementDirection == Vector2.zero)
            lastMovementDirection = combinedForce; // initialize if first frame
        combinedForce = SlerpVector2(lastMovementDirection, combinedForce, smoothingFactor);
        lastMovementDirection = combinedForce;
        // --- END SMOOTHING ---

        // Rotate enemy so that its bottom (-up) faces the movement direction.
        if (combinedForce != Vector2.zero)
            transform.up = -combinedForce;
        
        // Calculate the new position based on the combined movement force.
        Vector2 proposedMovement = combinedForce * moveSpeed * Time.fixedDeltaTime;
        Vector2 newPos = currentPosition + proposedMovement;
        
        // If the proposed movement would overshoot (i.e. bring enemy closer than minimumDistanceToPlayer), cancel movement.
        if (Vector2.Distance(newPos, playerPos) < minimumDistanceToPlayer)
            newPos = currentPosition;
        
        rb.MovePosition(newPos);
        
        // Trigger attack if within range (with tolerance).
        if (Vector2.Distance(rb.position, playerPos) <= minimumDistanceToPlayer + attackTolerance)
        {
            if (canAttack)
                Attack();
        }
    }

    
    private void Attack()
    {
        Debug.Log("Enemy triggers sword swing animation!");
        
        // Trigger the sword swing animation using the Animator Controller's "Attack" trigger
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
        
        // Begin cooldown to prevent spamming attacks
        canAttack = false;
        StartCoroutine(AttackCooldown());
    }
    
    private IEnumerator DealDamageAfterDelay()
    {
        yield return new WaitForSeconds(attackDamageDelay);
        if (player != null)
        {
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
    private Vector2 SlerpVector2(Vector2 from, Vector2 to, float t)
    {
        // Get the angles in radians
        float angleFrom = Mathf.Atan2(from.y, from.x);
        float angleTo = Mathf.Atan2(to.y, to.x);
        // Interpolate between angles
        float angle = Mathf.LerpAngle(angleFrom * Mathf.Rad2Deg, angleTo * Mathf.Rad2Deg, t) * Mathf.Deg2Rad;
        return new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
    }
}
