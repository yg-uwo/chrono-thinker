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
        if (GetComponent<Collider2D>() == null)
        {
            Debug.LogError("Obstacle has no Collider2D component! Add a collider for collision detection.");
        }
    }
    
    // We'll implement both collision methods to ensure it works
    void OnCollisionEnter2D(Collision2D collision)
    {
        HandleCollision(collision.gameObject);
    }
    
    void OnTriggerEnter2D(Collider2D other)
    {
        HandleCollision(other.gameObject);
    }
    
    // private void HandleCollision(GameObject collidingObject)
    // {
    //     // Log all collisions to help debug
    //     Debug.Log($"Obstacle collided with: {collidingObject.name}, Tag: {collidingObject.tag}");
        
    //     if (collidingObject.CompareTag("Enemy") && gameTimer != null)
    //     {
    //         Debug.Log("Enemy hit obstacle! Reducing time by " + timeReduction + " seconds.");
    //         gameTimer.ReduceTime(timeReduction);
            
    //         // Show UI notification
    //         if (uiManager != null)
    //         {
    //             uiManager.ShowTimeReduction(timeReduction);
    //         }
            
    //         // You could add visual/audio feedback here
    //         // For example, flash the obstacle briefly
    //         StartCoroutine(FlashObstacle());
    //     }
    // }

    private void HandleCollision(GameObject collidingObject)
{
    // Log all collisions to help debug
    Debug.Log($"Obstacle collided with: {collidingObject.name}, Tag: {collidingObject.tag}");
    
    // Check if it's an enemy by tag rather than just the word "Enemy"
    if (collidingObject.CompareTag("Enemy") && gameTimer != null)
    {
        Debug.Log("Enemy hit obstacle! Reducing time by " + timeReduction + " seconds.");
        gameTimer.ReduceTime(timeReduction);
        
        // You could add visual/audio feedback here
        // For example, flash the obstacle briefly
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