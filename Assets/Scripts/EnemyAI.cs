using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Rigidbody2D))]
public class EnemyAI : MonoBehaviour 
{
    [Header("Movement Settings")]
    public float moveSpeed = 2f;
    
    [Header("Attack Settings")]
    public float attackDamage = 10f;           // Damage per attack
    public float attackCooldown = 2f;          // Time between attacks
    public float minimumDistanceToPlayer = 0.8f; // Distance at which enemy stops moving
    public float attackDamageDelay = 0.3f;       // Delay (in seconds) before damage is applied to sync with sword swing

    private Rigidbody2D rb;
    private Transform player;
    private bool canAttack = true;
    private Animator animator;
    
    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        
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
        
        // Calculate direction and distance to player
        Vector2 direction = (player.position - transform.position).normalized;
        float distance = Vector2.Distance(rb.position, player.position);
        
        // Rotate enemy so that its bottom (-up) faces the player
        transform.up = -direction;
        
        // Move toward the player if farther than the minimum distance
        if (distance > minimumDistanceToPlayer)
        {
            Vector2 newPos = rb.position + direction * moveSpeed * Time.fixedDeltaTime;
            // Clamp so enemy stops at minimum distance
            if (Vector2.Distance(newPos, player.position) < minimumDistanceToPlayer)
            {
                newPos = (Vector2)player.position - direction * minimumDistanceToPlayer;
            }
            rb.MovePosition(newPos);
        }
        else
        {
            // When close enough, attack if allowed
            if (canAttack)
            {
                Attack();
            }
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
}
