using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

public class GameUIManager : MonoBehaviour
{
    [Header("UI Panels")]
    public GameObject gameOverPanel;
    public GameObject victoryPanel;
    public GameObject inGameUI;
    
    [Header("UI Text Elements")]
    public TMP_Text gameOverReasonText;
    public TMP_Text finalTimeText;
    public TMP_Text victoryTimeText;  // Add this reference for the victory panel
    
    [Header("Settings")]
    public float delayBeforeRestart = 2f;
    
    [Header("References")]
    [SerializeField] private GameTimer gameTimer;
    [SerializeField] private PlayerMovement playerMovement;
    
    [Header("UI Elements")]
    public Button nextLevelButton;  // Reference to the Next Level button
    
    // Singleton pattern for easy access
    public static GameUIManager Instance { get; private set; }

    private void Awake()
    {
        // Singleton setup
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }
    
    private void Start()
    {
        // Make sure all panels are hidden at start
        if (gameOverPanel) gameOverPanel.SetActive(false);
        if (victoryPanel) victoryPanel.SetActive(false);
        if (inGameUI) inGameUI.SetActive(true);
        
        // Find references if not set in inspector
        if (gameTimer == null)
            gameTimer = FindObjectOfType<GameTimer>();
            
        if (playerMovement == null)
            playerMovement = FindObjectOfType<PlayerMovement>();
    }
    
    // Call this when time runs out
    public void ShowTimeUpGameOver(float finalTime)
    {
        if (gameOverPanel && gameOverReasonText && finalTimeText)
        {
            gameOverReasonText.text = "Time's Up!";
            finalTimeText.text = FormatTime(finalTime);
            gameOverPanel.SetActive(true);
            
            // Hide in-game UI elements
            if (inGameUI) inGameUI.SetActive(false);
            
            // Pause all game objects
            PauseGameObjects();
            
            // No automatic restart - wait for user to press restart button
        }
    }
    
    // Call this when player is caught by enemy
    public void ShowEnemyCaughtGameOver(float finalTime)
    {
        if (gameOverPanel && gameOverReasonText && finalTimeText)
        {
            gameOverReasonText.text = "Caught by Enemy!";
            finalTimeText.text = FormatTime(finalTime);
            gameOverPanel.SetActive(true);
            
            // Hide in-game UI elements
            if (inGameUI) inGameUI.SetActive(false);
            
            // Pause all game objects
            PauseGameObjects();
            
            // No automatic restart - wait for user to press restart button
        }
    }
    
    // Call this when level is completed
    public void ShowVictory(float finalTime)
    {
        Debug.Log("ShowVictory called with time: " + finalTime);
        
        if (victoryPanel)
        {
            // Find victory time text if not assigned
            if (victoryTimeText == null)
            {
                victoryTimeText = victoryPanel.GetComponentInChildren<TMP_Text>();
                if (victoryTimeText == null)
                {
                    Debug.LogError("Victory time text not found!");
                }
            }
            
            if (victoryTimeText != null)
            {
                // For countdown timer, calculate elapsed time
                if (gameTimer != null && gameTimer.countDown)
                {
                    // Calculate elapsed time (startTime - currentTime)
                    float elapsedTime = gameTimer.startTime - finalTime;
                    Debug.Log($"Calculated elapsed time: {elapsedTime} (Start time: {gameTimer.startTime} - Final time: {finalTime})");
                    victoryTimeText.text = "Level Complete! Time: " + FormatTime(elapsedTime);
                }
                else
                {
                    // If not countdown, use finalTime directly
                    victoryTimeText.text = "Level Complete! Time: " + FormatTime(finalTime);
                }
                
                Debug.Log("Set victory time text to: " + victoryTimeText.text);
            }
            
            // Configure the next level button
            ConfigureNextLevelButton();
            
            // Show the victory panel with its buttons (Next Level and Main Menu)
            victoryPanel.SetActive(true);
            
            // Hide in-game UI elements
            if (inGameUI) inGameUI.SetActive(false);
            
            // Hide player and enemies
            HideGameEntities();
        }
        else
        {
            Debug.LogError("Victory panel is not assigned!");
        }
    }
    
    // Configure the Next Level button based on the current level
    private void ConfigureNextLevelButton()
    {
        if (nextLevelButton == null)
        {
            Debug.LogWarning("Next Level button not assigned in the inspector!");
            return;
        }
        
        // Find the GoalTrigger to get the next level information
        GoalTrigger goalTrigger = FindObjectOfType<GoalTrigger>();
        if (goalTrigger != null)
        {
            // Clear existing listeners
            nextLevelButton.onClick.RemoveAllListeners();
            
            // If this is the final level, button should loop back to the same level or main menu
            if (goalTrigger.isFinalLevel)
            {
                // Option 1: Return to main menu
                nextLevelButton.onClick.AddListener(ReturnToMainMenu);
                
                // Change the button text if we can find it
                TextMeshProUGUI buttonText = nextLevelButton.GetComponentInChildren<TextMeshProUGUI>();
                if (buttonText != null)
                {
                    buttonText.text = "Main Menu";
                }
                
                Debug.Log("Final level - Next Level button will return to main menu");
            }
            else
            {
                // Set button to load the next level
                string nextLevel = goalTrigger.nextLevelName;
                nextLevelButton.onClick.AddListener(() => LoadNextLevel(nextLevel));
                Debug.Log($"Next Level button will load {nextLevel}");
            }
        }
        else
        {
            Debug.LogWarning("GoalTrigger not found - using default next level button behavior");
            
            // Default: just go to the next scene in build settings
            nextLevelButton.onClick.AddListener(() => {
                int currentIndex = SceneManager.GetActiveScene().buildIndex;
                int nextIndex = currentIndex + 1;
                if (nextIndex < SceneManager.sceneCountInBuildSettings)
                {
                    SceneManager.LoadScene(nextIndex);
                }
                else
                {
                    // If no more scenes, go back to main menu
                    SceneManager.LoadScene("MainMenu");
                }
            });
        }
    }
    
    // Format time as MM:SS
    private string FormatTime(float timeInSeconds)
    {
        int minutes = Mathf.FloorToInt(timeInSeconds / 60);
        int seconds = Mathf.FloorToInt(timeInSeconds % 60);
        return string.Format("{0:00}:{1:00}", minutes, seconds);
    }
    
    // Show temporary notification for time reduction
    public void ShowTimeReduction(float amount)
    {
        // Create a temporary floating text that fades out
        Debug.Log($"Time reduced by {amount} seconds!");
        
        // Create a temporary text notification
        StartCoroutine(ShowFloatingText($"-{amount}s", Color.red));
    }
    
    // Show temporary notification with a custom message
    public void ShowMessage(string message)
    {
        Debug.Log($"Showing message: {message}");
        
        // Create a temporary text notification
        StartCoroutine(ShowFloatingText(message, Color.yellow));
    }
    
    private System.Collections.IEnumerator ShowFloatingText(string text, Color color)
    {
        // Create temporary text object
        GameObject tempTextObj = new GameObject("TempText");
        tempTextObj.transform.SetParent(inGameUI.transform);
        TMP_Text tmpText = tempTextObj.AddComponent<TextMeshProUGUI>();
        
        // Set text properties
        tmpText.text = text;
        tmpText.color = color;
        tmpText.fontSize = 36;
        tmpText.alignment = TextAlignmentOptions.Center;
        
        // Position near the timer
        if (GameObject.Find("TimerText") != null)
        {
            RectTransform timerRect = GameObject.Find("TimerText").GetComponent<RectTransform>();
            RectTransform textRect = tempTextObj.GetComponent<RectTransform>();
            textRect.position = timerRect.position + new Vector3(100, 0, 0);
            textRect.sizeDelta = new Vector2(200, 50);
        }
        
        // Animate the text
        float duration = 1.5f;
        float elapsed = 0;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(1, 0, elapsed / duration);
            tmpText.color = new Color(color.r, color.g, color.b, alpha);
            
            // Move upward slightly
            tempTextObj.transform.position += new Vector3(0, 30 * Time.deltaTime, 0);
            
            yield return null;
        }
        
        // Destroy the text object
        Destroy(tempTextObj);
    }
    
    private void RestartLevel()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
    
    // For next level button in victory panel
    public void LoadNextLevel(string levelName)
    {
        SceneManager.LoadScene(levelName);
    }
    
    // For returning to main menu from any panel
    public void ReturnToMainMenu()
    {
        // Load the main menu scene
        SceneManager.LoadScene("MainMenu");
    }

    public void ShowGameOverPanel(string reason = "Game Over!", float finalTime = 0f)
    {
        if (gameOverPanel != null)
        {
            // Set the reason text
            if (gameOverReasonText != null)
            {
                gameOverReasonText.text = reason;
            }
            
            // Set the time text
            if (finalTimeText != null)
            {
                finalTimeText.text = FormatTime(finalTime);
            }
            
            // Show the panel
            gameOverPanel.SetActive(true);
            
            // Hide in-game UI
            if (inGameUI) inGameUI.SetActive(false);
            
            // Pause game objects
            PauseGameObjects();
        }
        else
        {
            Debug.LogError("GameOverPanel is not assigned in the Inspector!");
        }
    }
    
    public void RestartGame()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    // For restart button in game over panel
    public void RestartButton()
    {
        RestartLevel();
    }
    
    // For quit button - now returns to main menu
    public void QuitGame()
    {
        // Use our dedicated method to return to the main menu
        ReturnToMainMenu();
    }
    
    // Pause all moving entities and hide them
    private void PauseGameObjects()
    {
        Debug.Log("Pausing and hiding game entities");
        
        // Disable and hide all enemies
        foreach (EnemyAI enemy in FindObjectsOfType<EnemyAI>())
        {
            enemy.enabled = false;
            
            // Hide by disabling the GameObject
            enemy.gameObject.SetActive(false);
        }
        
        // Disable and hide player
        PlayerMovement player = FindObjectOfType<PlayerMovement>();
        if (player != null)
        {
            player.enabled = false;
            
            // Hide player by disabling the GameObject
            player.gameObject.SetActive(false);
        }
        
        // Disable player aiming
        PlayerPunching playerPunching = FindObjectOfType<PlayerPunching>();
        if (playerPunching != null && playerPunching.aimIndicator != null)
        {
            playerPunching.aimIndicator.SetActive(false);
            
            // Also hide the punching bag
            PunchingBag[] punchingBags = FindObjectsOfType<PunchingBag>();
            foreach (PunchingBag bag in punchingBags)
            {
                bag.gameObject.SetActive(false);
            }
        }
        
        // Destroy all enemy health bars
        DestroyAllEnemyHealthBars();
        
        // Destroy any damage indicators that might be visible
        DestroyAllDamageIndicators();
    }
    
    // Hide player and enemies when victory
    private void HideGameEntities()
    {
        Debug.Log("Hiding game entities for victory");
        
        // Hide all enemies
        foreach (EnemyAI enemy in FindObjectsOfType<EnemyAI>())
        {
            // Option 2: Hide entire game object
            enemy.gameObject.SetActive(false);
        }
        
        // Hide player
        PlayerMovement player = FindObjectOfType<PlayerMovement>();
        if (player != null)
        {
            // Hide entire game object
            player.gameObject.SetActive(false);
        }
        
        // Disable player aiming
        PlayerPunching playerPunching = FindObjectOfType<PlayerPunching>();
        if (playerPunching != null)
        {
            if (playerPunching.aimIndicator != null)
            {
                playerPunching.aimIndicator.SetActive(false);
            }
            
            // Also hide punching bags
            PunchingBag[] punchingBags = FindObjectsOfType<PunchingBag>();
            foreach (PunchingBag bag in punchingBags)
            {
                bag.gameObject.SetActive(false);
            }
        }
        
        // Destroy all enemy health bars that might still be active
        DestroyAllEnemyHealthBars();
        
        // Clean up damage indicators
        DestroyAllDamageIndicators();
    }
    
    // New method to find and destroy all enemy health bars
    private void DestroyAllEnemyHealthBars()
    {
        Debug.Log("Removing all enemy health bars");
        
        // Find world canvas and destroy all child health bars
        Canvas worldCanvas = FindObjectOfType<Canvas>();
        if (worldCanvas != null && worldCanvas.renderMode == RenderMode.WorldSpace && worldCanvas.name == "WorldSpaceCanvas")
        {
            // Look for all sliders under the canvas (health bars)
            Slider[] healthBars = worldCanvas.GetComponentsInChildren<Slider>(true);
            foreach (Slider healthBar in healthBars)
            {
                // Check if this is likely an enemy health bar
                if (healthBar.gameObject.name.Contains("HealthBar") || 
                    (healthBar.transform.parent != null && healthBar.transform.parent.name.Contains("HealthBar")))
                {
                    Debug.Log($"Destroying health bar: {healthBar.gameObject.name}");
                    Destroy(healthBar.gameObject);
                }
            }
        }
        
        // Alternative approach - find any GameObject with EnemyHealthBar in its name
        GameObject[] healthBarObjects = GameObject.FindObjectsOfType<GameObject>();
        foreach (GameObject obj in healthBarObjects)
        {
            if (obj.name.Contains("EnemyHealthBar") || obj.name.Contains("HealthBar"))
            {
                Debug.Log($"Destroying health bar object: {obj.name}");
                Destroy(obj);
            }
        }
    }

    // Destroy all damage indicators
    private void DestroyAllDamageIndicators()
    {
        // Find and destroy all objects with DamageIndicator component
        DamageIndicator[] indicators = FindObjectsOfType<DamageIndicator>();
        foreach (DamageIndicator indicator in indicators)
        {
            Destroy(indicator.gameObject);
        }
        
        // Also find any objects that might have been created with the name pattern
        GameObject[] damageObjects = GameObject.FindGameObjectsWithTag("Untagged"); // No specific tag, but we can filter by name
        foreach (GameObject obj in damageObjects)
        {
            if (obj.name.StartsWith("DamageIndicator_"))
            {
                Destroy(obj);
            }
        }
    }
}