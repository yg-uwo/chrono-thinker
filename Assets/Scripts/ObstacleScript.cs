using UnityEngine;

public class ObstacleScript : MonoBehaviour
{
    public float timeReduction = 5f;     // Time to reduce when enemy hits
    private GameTimer gameTimer;         // Reference to the game timer
    private GameUIManager uiManager;     // Reference to UI manager
    
    void Start()
    {
        // Find the GameTimer component
        gameTimer = FindObjectOfType<GameTimer>();
        
        // Find UI Manager
        uiManager = FindObjectOfType<GameUIManager>();
        
        if (gameTimer == null)
        {
            Debug.LogWarning("GameTimer not found in scene. Time reduction won't work.");
        }
        
        // Make sure this object has the "Obstacle" tag
        if (gameObject.tag != "Obstacle")
        {
            Debug.LogWarning("This GameObject doesn't have the 'Obstacle' tag! Add this tag for proper collision detection.");
        }
        
        // Make sure this object has a collider
        Collider2D collider = GetComponent<Collider2D>();
        if (collider == null)
        {
            Debug.LogError("Obstacle has no Collider2D component! Add a collider for collision detection.");
        }
        else if (!collider.isTrigger)
        {
            Debug.LogWarning("Obstacle's collider is not set as a trigger. Set isTrigger to true for proper detection.");
        }
    }
    
    void OnTriggerEnter2D(Collider2D other)
    {
        // Log all collisions to help debug
        Debug.Log($"Obstacle collided with: {other.gameObject.name}, Tag: {other.gameObject.tag}");
        
        // Check if it's an enemy by tag
        if (other.CompareTag("Enemy") && gameTimer != null)
        {
            Debug.Log("Enemy hit obstacle! Reducing time by " + timeReduction + " seconds.");
            gameTimer.ReduceTime(timeReduction);
            
            // Visual feedback
            StartCoroutine(FlashObstacle());
        }
    }
    
    private System.Collections.IEnumerator FlashObstacle()
    {
        // Get the sprite renderer
        SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            // Store original color
            Color originalColor = spriteRenderer.color;
            
            // Change to red
            spriteRenderer.color = Color.red;
            
            // Wait for a moment
            yield return new WaitForSeconds(0.2f);
            
            // Change back to original color
            spriteRenderer.color = originalColor;
        }
        else
        {
            yield return null;
        }
    }
}