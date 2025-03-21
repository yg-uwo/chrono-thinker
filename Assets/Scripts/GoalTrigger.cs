// using UnityEngine;
// using UnityEngine.SceneManagement;

// public class GoalTrigger : MonoBehaviour 
// {
//     public string nextLevelName = "Level2"; // Set this in inspector
//     public float completionDelay = 1.0f;    // Time before loading next level
//     public bool isFinalLevel = false;       // Is this the final level?
    
//     private void OnTriggerEnter2D(Collider2D other)
//     {
//         if (other.CompareTag("Player"))
//         {
//             Debug.Log("Level Complete!");
            
//             // Disable player movement to prevent further input
//             other.GetComponent<SimplePlayerMovement>().enabled = false;
            
//             // Get current time from GameTimer
//             GameTimer gameTimer = FindObjectOfType<GameTimer>();
//             float finalTime = gameTimer != null ? gameTimer.GetCurrentTime() : 0f;
            
//             // Show victory UI
//             GameUIManager.Instance?.ShowVictory(finalTime);
            
//             // Handle level completion
//             StartCoroutine(CompleteLevel());
//         }
//     }
    
//     private System.Collections.IEnumerator CompleteLevel()
//     {
//         // Wait for specified delay (for victory effects)
//         yield return new WaitForSeconds(completionDelay);
        
//         // If it's the final level, you could load a victory screen
//         if (isFinalLevel)
//         {
//             // For now, just reload the current level
//             // Later you can create a Victory scene
//             SceneManager.LoadScene(SceneManager.GetActiveScene().name);
//         }
//         else
//         {
//             // Load the next level
//             SceneManager.LoadScene(nextLevelName);
//         }
//     }
// }

using UnityEngine;
using UnityEngine.SceneManagement;

public class GoalTrigger : MonoBehaviour 
{
    public string nextLevelName = "Level2"; // Set this in inspector
    public float completionDelay = 1.0f;    // Time before loading next level
    public bool isFinalLevel = false;       // Is this the final level?
    public GameObject victoryPanel;         // Reference to the victory panel
    
    private GameUIManager uiManager;
    private bool levelCompleted = false;
    
    private void Start()
    {
        // Find UI Manager at start
        uiManager = FindObjectOfType<GameUIManager>();
        if (uiManager == null)
        {
            Debug.LogError("UI Manager not found! Make sure GameUIManager exists in the scene.");
        }
    }
    
    private void OnTriggerEnter2D(Collider2D other)
    {
        // Add debug logs to help track the issue
        Debug.Log($"Something entered the goal trigger: {other.name} with tag: {other.tag}");
        
        if (other.CompareTag("Player") && !levelCompleted)
        {
            Debug.Log("Level Complete! Player reached goal.");
            levelCompleted = true;
            
            // Disable player movement to prevent further input
            SimplePlayerMovement playerMovement = other.GetComponent<SimplePlayerMovement>();
            if (playerMovement != null)
            {
                playerMovement.enabled = false;
                Debug.Log("Disabled player movement");
            }
            else
            {
                Debug.LogWarning("SimplePlayerMovement component not found on player!");
            }
            
            // Get current time from GameTimer
            GameTimer gameTimer = FindObjectOfType<GameTimer>();
            float finalTime = 0f;
            
            if (gameTimer != null)
            {
                finalTime = gameTimer.GetCurrentTime();
                Debug.Log($"Final time: {finalTime}");
            }
            else
            {
                Debug.LogWarning("GameTimer not found! Cannot get time.");
            }
            
            // Show victory UI
            if (uiManager != null)
            {
                uiManager.ShowVictory(finalTime);
                Debug.Log("Victory UI shown with time: " + finalTime);
            }
            else
            {
                Debug.LogError("UI Manager not found! Cannot show victory panel.");
            }
            
            // Handle level completion
            StartCoroutine(CompleteLevel());
        }
    }
    
    private System.Collections.IEnumerator CompleteLevel()
    {
        Debug.Log($"Waiting {completionDelay} seconds before loading next level");
        // Wait for specified delay (for victory effects)
        yield return new WaitForSeconds(completionDelay);
        
        // If it's the final level, you could load a victory screen
        if (isFinalLevel)
        {
            Debug.Log("This is the final level. Reloading current scene.");
            // For now, just reload the current level
            // Later you can create a Victory scene
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
        else
        {
            Debug.Log($"Loading next level: {nextLevelName}");
            // Load the next level
            SceneManager.LoadScene(nextLevelName);
        }
    }
}