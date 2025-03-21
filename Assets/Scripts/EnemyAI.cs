// using UnityEngine;
// using UnityEngine.SceneManagement;

// public class EnemyAI : MonoBehaviour 
// {
//     public float moveSpeed = 2f;
//     private Transform player;
    
//     private void Start()
//     {
//         // Automatically find the player GameObject by tag
//         player = GameObject.FindGameObjectWithTag("Player").transform;
        
//         // Check if the player was found
//         if (player == null)
//         {
//             Debug.LogError("Player not found! Make sure the player has the 'Player' tag.");
//         }
//     }
    
//     private void Update()
//     {
//         if (player != null)
//         {
//             // Move toward the player's position
//             transform.position = Vector2.MoveTowards(transform.position, player.position, moveSpeed * Time.deltaTime);
//         }
//     }
    
//     private void OnTriggerEnter2D(Collider2D other)
//     {
//         if (other.CompareTag("Player"))
//         {
//             Debug.Log("Player caught by enemy! Game Over!");
//             // Reset the current level
//             SceneManager.LoadScene(SceneManager.GetActiveScene().name);
            
//             // You could add game over UI or effects here later
//         }
        
//         if (other.CompareTag("Obstacle"))
//         {
//             // Slow down enemy briefly when it hits an obstacle
//             StartCoroutine(SlowDownEnemy());
//         }
//     }
    
//     private System.Collections.IEnumerator SlowDownEnemy()
//     {
//         float originalSpeed = moveSpeed;
//         moveSpeed = moveSpeed / 2;
//         yield return new WaitForSeconds(1.5f);
//         moveSpeed = originalSpeed;
//     }
// }

using UnityEngine;
using UnityEngine.SceneManagement;

public class EnemyAI : MonoBehaviour 
{
    public float moveSpeed = 2f;
    private Transform player;
    private bool isGameOver = false;
    
    private void Start()
    {
        // Automatically find the player GameObject by tag
        player = GameObject.FindGameObjectWithTag("Player")?.transform;
        
        // Check if the player was found
        if (player == null)
        {
            Debug.LogError("Player not found! Make sure the player has the 'Player' tag.");
        }
    }
    
    private void Update()
    {
        if (isGameOver || player == null)
            return;
            
        // Move toward the player's position
        transform.position = Vector2.MoveTowards(transform.position, player.position, moveSpeed * Time.deltaTime);
    }
    
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (isGameOver)
            return;
            
        if (other.CompareTag("Player"))
        {
            Debug.Log("Player caught by enemy! Game Over!");
            isGameOver = true;
            
            // Get current time from GameTimer
            GameTimer gameTimer = FindObjectOfType<GameTimer>();
            float finalTime = gameTimer != null ? gameTimer.GetCurrentTime() : 0f;
            
            // Show game over UI
            GameUIManager.Instance?.ShowEnemyCaughtGameOver(finalTime);
            
            // Disable player movement
            other.GetComponent<SimplePlayerMovement>().enabled = false;
        }
        
        if (other.CompareTag("Obstacle"))
        {
            // Slow down enemy briefly when it hits an obstacle
            StartCoroutine(SlowDownEnemy());
        }
    }
    
    private System.Collections.IEnumerator SlowDownEnemy()
    {
        float originalSpeed = moveSpeed;
        moveSpeed = moveSpeed / 2;
        yield return new WaitForSeconds(1.5f);
        moveSpeed = originalSpeed;
    }
}