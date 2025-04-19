using UnityEngine;
using UnityEngine.SceneManagement;

public class GoalTrigger : MonoBehaviour 
{
    public string nextLevelName = "Level2"; // Set this in inspector
    public bool isFinalLevel = false;       // Is this the final level?
    public GameObject victoryPanel;         // Reference to the victory panel
    
    [Header("Goal Appearance")]
    public Color activeColor = Color.blue;     // Color when goal is active (all enemies dead)
    public Color inactiveColor = Color.gray;   // Color when goal is inactive (enemies alive)
    
    private GameUIManager uiManager;
    private bool levelCompleted = false;
    private SpriteRenderer spriteRenderer;
    private bool goalActive = false;
    private bool isGameOver = false; // Track if the game is over due to player death or time up
    private PlayerHealth playerHealth;
    private GameTimer gameTimer;
    
    private void Start()
    {
        // Find UI Manager at start
        uiManager = FindObjectOfType<GameUIManager>();
        if (uiManager == null)
        {
            Debug.LogError("UI Manager not found! Make sure GameUIManager exists in the scene.");
        }
        
        // Get the sprite renderer
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            
            if (spriteRenderer == null)
            {
                Debug.LogError("No SpriteRenderer found on goal! Add a SpriteRenderer to show goal state.");
            }
        }
        
        // Cache references to player health and game timer
        playerHealth = FindObjectOfType<PlayerHealth>();
        gameTimer = FindObjectOfType<GameTimer>();
        
        // Update goal appearance on start
        UpdateGoalAppearance();
        
        // Start checking for enemies periodically
        InvokeRepeating("CheckEnemiesStatus", 0.5f, 0.5f);
    }
    
    private void Update()
    {
        // Check if game is over
        CheckGameOverStatus();
    }
    
    private void CheckGameOverStatus()
    {
        if (isGameOver)
            return;
            
        // Check for game over conditions
        bool gameTimerOver = gameTimer != null && gameTimer.IsGameOver();
        bool playerDead = playerHealth != null && playerHealth.IsDead();
        
        if (gameTimerOver || playerDead)
        {
            isGameOver = true;
            
            // Force goal to be grey when game is over
            if (spriteRenderer != null)
            {
                spriteRenderer.color = inactiveColor;
            }
            
            // Stop checking for enemies
            CancelInvoke("CheckEnemiesStatus");
            
            Debug.Log("Goal has been locked (grey) due to game over");
        }
    }
    
    private void CheckEnemiesStatus()
    {
        // Don't update if game is over
        if (isGameOver)
            return;
            
        bool enemiesRemaining = AreEnemiesRemaining();
        
        // Only update if the status has changed
        if (goalActive == enemiesRemaining) 
        {
            goalActive = !enemiesRemaining;
            UpdateGoalAppearance();
        }
    }
    
    private void UpdateGoalAppearance()
    {
        if (spriteRenderer != null)
        {
            // If game is over, always show as inactive
            if (isGameOver)
            {
                spriteRenderer.color = inactiveColor;
                return;
            }
            
            // Otherwise, set color based on whether goal is active
            spriteRenderer.color = goalActive ? activeColor : inactiveColor;
        }
    }
    
    private void OnTriggerEnter2D(Collider2D other)
    {
        // Add debug logs to help track the issue
        Debug.Log($"Something entered the goal trigger: {other.name} with tag: {other.tag}");
        
        // Don't allow goal completion if game is over
        if (isGameOver)
            return;
            
        if (other.CompareTag("Player") && !levelCompleted)
        {
            // Check if there are still enemies alive
            if (AreEnemiesRemaining())
            {
                Debug.Log("Player reached goal, but enemies are still alive! Cannot complete level yet.");
                return;
            }
            
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
    
    private bool AreEnemiesRemaining()
    {
        // Find all enemy objects in the scene
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        
        // If there are any enemies left, return true
        return enemies.Length > 0;
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
    
    // For visual debugging purposes only
    private void OnDrawGizmos()
    {
        // Draw a visual indicator for the goal in the editor
        Gizmos.color = AreEnemiesRemaining() ? inactiveColor : activeColor;
        Gizmos.DrawWireSphere(transform.position, 0.5f);
    }
}