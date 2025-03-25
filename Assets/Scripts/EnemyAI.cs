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
    

//     private void OnTriggerEnter2D(Collider2D other)
// {
//     Debug.Log("Enemy has hit the player..sad");
//     Debug.Log("Enemy collision with: " + other.gameObject.name + " tag: " + other.tag);
//     if (isGameOver)
//         return;
        
//     if (other.CompareTag("Player"))
//     {
//         Debug.Log("Player caught by enemy! Game Over!");
//         isGameOver = true;
        
       
//         GameTimer gameTimer = FindObjectOfType<GameTimer>();
//         float finalTime = gameTimer != null ? gameTimer.GetCurrentTime() : 0f;
        
        
//         GameUIManager.Instance?.ShowEnemyCaughtGameOver(finalTime);
        
        
//         other.GetComponent<SimplePlayerMovement>().enabled = false;
//     }
    

//     if (other.gameObject.name == "Obstacle" || 
//     other.gameObject.name == "Obstacle_1" || 
//     other.gameObject.name == "Obstacle_2" || 
//     other.gameObject.name == "Obstacle_3")
// {
//     Debug.Log("Enemy hit an obstacle!");
//     StartCoroutine(SlowDownEnemy());
// }
// }

private void OnTriggerEnter2D(Collider2D other)
{
    Debug.Log("Enemy collision with: " + other.gameObject.name + " tag: " + other.tag);
    if (isGameOver)
        return;
    
    if (other.CompareTag("Player"))
    {
        Debug.Log("Player caught by enemy! Game Over!");
        isGameOver = true;
        GameTimer gameTimer = FindObjectOfType<GameTimer>();
        float finalTime = gameTimer != null ? gameTimer.GetCurrentTime() : 0f;
        GameUIManager.Instance?.ShowEnemyCaughtGameOver(finalTime);
        SimplePlayerMovement playerMovement = other.GetComponent<SimplePlayerMovement>();
        if (playerMovement != null)
        {
            playerMovement.enabled = false;
        }
        this.enabled = false;
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