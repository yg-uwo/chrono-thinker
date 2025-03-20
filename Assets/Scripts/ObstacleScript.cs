using UnityEngine;

public class ObstacleScript : MonoBehaviour
{
    public float timeReduction = 5f;     // Time to reduce when enemy hits
    private GameTimer gameTimer;         // Reference to the game timer
    
    void Start()
    {
        // Find the GameTimer component
        gameTimer = FindObjectOfType<GameTimer>();
        
        if (gameTimer == null)
        {
            Debug.LogWarning("GameTimer not found in scene. Time reduction won't work.");
        }
    }
    
    void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Enemy") && gameTimer != null)
        {
            Debug.Log("Enemy hit obstacle! Reducing time by " + timeReduction + " seconds.");
            gameTimer.ReduceTime(timeReduction);
            
            // You could add visual/audio feedback here
        }
    }
}