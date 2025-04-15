using UnityEngine;
using UnityEngine.SceneManagement;

public class GoalTrigger : MonoBehaviour 
{
    public string nextLevelName = "Level2"; // Set this in inspector
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
            PlayerMovement playerMovement = other.GetComponent<PlayerMovement>();
            if (playerMovement != null)
            {
                playerMovement.enabled = false;
                Debug.Log("Disabled player movement");
            }
            else
            {
                Debug.LogWarning("PlayerMovement component not found on player!");
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
            
            // Show victory UI with Next Level button
            if (uiManager != null)
            {
                // Configure the Next Level button through the UI Manager
                SetupNextLevelButton();
                
                // Show the victory panel
                uiManager.ShowVictory(finalTime);
                Debug.Log("Victory UI shown with time: " + finalTime);
            }
            else
            {
                Debug.LogError("UI Manager not found! Cannot show victory panel.");
            }
            
            // Note: We're no longer automatically loading the next level
            // The player will use the "Next Level" button in the victory panel
        }
    }
    
    private void SetupNextLevelButton()
    {
        // Find and configure the Next Level button in the UI Manager
        if (uiManager != null)
        {
            // Set the Next Level button to load the appropriate scene
            // This is handled by the LoadNextLevel method in GameUIManager
            Debug.Log($"Next level is set to: {nextLevelName}, isFinalLevel: {isFinalLevel}");
        }
    }
}