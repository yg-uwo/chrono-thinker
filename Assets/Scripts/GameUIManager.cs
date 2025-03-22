// using UnityEngine;
// using UnityEngine.UI;
// using TMPro;
// using UnityEngine.SceneManagement;

// public class GameUIManager : MonoBehaviour
// {
//     [Header("UI Panels")]
//     public GameObject gameOverPanel;
//     public GameObject victoryPanel;
//     public GameObject inGameUI;
    
//     [Header("UI Text Elements")]
//     public TMP_Text gameOverReasonText;
//     public TMP_Text finalTimeText;
//     public TMP_Text victoryTimeText;  // Add this reference for the victory panel
    
//     [Header("Settings")]
//     public float delayBeforeRestart = 2f;
    
//     // Singleton pattern for easy access
//     public static GameUIManager Instance { get; private set; }

//     private void Awake()
//     {
//         // Singleton setup
//         if (Instance == null)
//         {
//             Instance = this;
//         }
//         else
//         {
//             Destroy(gameObject);
//             return;
//         }
//     }
    
//     private void Start()
//     {
//         // Make sure all panels are hidden at start
//         if (gameOverPanel) gameOverPanel.SetActive(false);
//         if (victoryPanel) victoryPanel.SetActive(false);
//         if (inGameUI) inGameUI.SetActive(true);
//     }
    
//     // Call this when time runs out
//     public void ShowTimeUpGameOver(float finalTime)
//     {
//         if (gameOverPanel && gameOverReasonText && finalTimeText)
//         {
//             gameOverReasonText.text = "Time's Up!";
//             finalTimeText.text = FormatTime(finalTime);
//             gameOverPanel.SetActive(true);
            
//             // Hide in-game UI elements
//             if (inGameUI) inGameUI.SetActive(false);
            
//             // Pause all game objects
//             PauseGameObjects();
            
//             // Restart level after delay
//             Invoke("RestartLevel", delayBeforeRestart);
//         }
//     }
    
//     // Call this when player is caught by enemy
//     public void ShowEnemyCaughtGameOver(float finalTime)
//     {
//         if (gameOverPanel && gameOverReasonText && finalTimeText)
//         {
//             gameOverReasonText.text = "Caught by Enemy!";
//             finalTimeText.text = FormatTime(finalTime);
//             gameOverPanel.SetActive(true);
            
//             // Hide in-game UI elements
//             if (inGameUI) inGameUI.SetActive(false);
            
//             // Pause all game objects
//             PauseGameObjects();
            
//             // Restart level after delay
//             Invoke("RestartLevel", delayBeforeRestart);
//         }
//     }
    
//     // Call this when level is completed
//     // public void ShowVictory(float finalTime)
//     // {
//     //     Debug.Log("ShowVictory called with time: " + finalTime);
        
//     //     if (victoryPanel)
//     //     {
//     //         // Find victory time text if not assigned
//     //         if (victoryTimeText == null)
//     //         {
//     //             victoryTimeText = victoryPanel.GetComponentInChildren<TMP_Text>();
//     //             if (victoryTimeText == null)
//     //             {
//     //                 Debug.LogError("Victory time text not found!");
//     //             }
//     //         }
            
//     //         if (victoryTimeText != null)
//     //         {
//     //             victoryTimeText.text = "Your Time: " + FormatTime(finalTime);
//     //             Debug.Log("Set victory time text to: " + victoryTimeText.text);
//     //         }
            
//     //         victoryPanel.SetActive(true);
            
//     //         // Hide in-game UI elements
//     //         if (inGameUI) inGameUI.SetActive(false);
            
//     //         // Hide player and enemies
//     //         HideGameEntities();
//     //     }
//     //     else
//     //     {
//     //         Debug.LogError("Victory panel is not assigned!");
//     //     }
//     // }

//     // Call this when level is completed
// public void ShowVictory(float finalTime)
// {
//     Debug.Log("ShowVictory called with time: " + finalTime);
    
//     if (victoryPanel)
//     {
//         // Find victory time text if not assigned
//         if (victoryTimeText == null)
//         {
//             victoryTimeText = victoryPanel.GetComponentInChildren<TMP_Text>();
//             if (victoryTimeText == null)
//             {
//                 Debug.LogError("Victory time text not found!");
//             }
//         }
        
//         if (victoryTimeText != null)
//         {
//             // For countdown timer, calculate elapsed time
//             GameTimer gameTimer = FindObjectOfType<GameTimer>();
//             float elapsedTime = 0f;
            
//             if (gameTimer != null && gameTimer.countDown)
//             {
//                 // Calculate elapsed time (startTime - currentTime)
//                 elapsedTime = gameTimer.startTime - finalTime;
//                 Debug.Log($"Calculated elapsed time: {elapsedTime} (Start time: {gameTimer.startTime} - Final time: {finalTime})");
//                 victoryTimeText.text = "Level Complete! Time: " + FormatTime(elapsedTime);
//             }
//             else
//             {
//                 // If not countdown, use finalTime directly
//                 victoryTimeText.text = "Level Complete! Time: " + FormatTime(finalTime);
//             }
            
//             Debug.Log("Set victory time text to: " + victoryTimeText.text);
//         }
        
//         victoryPanel.SetActive(true);
        
//         // Hide in-game UI elements
//         if (inGameUI) inGameUI.SetActive(false);
        
//         // Hide player and enemies
//         HideGameEntities();
//     }
//     else
//     {
//         Debug.LogError("Victory panel is not assigned!");
//     }
// }
    
//     // Format time as MM:SS
//     private string FormatTime(float timeInSeconds)
//     {
//         int minutes = Mathf.FloorToInt(timeInSeconds / 60);
//         int seconds = Mathf.FloorToInt(timeInSeconds % 60);
//         return string.Format("{0:00}:{1:00}", minutes, seconds);
//     }
    
//     // Show temporary notification for time reduction
//     public void ShowTimeReduction(float amount)
//     {
//         // Create a temporary floating text that fades out
//         Debug.Log($"Time reduced by {amount} seconds!");
        
//         // Create a temporary text notification
//         StartCoroutine(ShowFloatingText($"-{amount}s", Color.red));
//     }
    
//     private System.Collections.IEnumerator ShowFloatingText(string text, Color color)
//     {
//         // Create temporary text object
//         GameObject tempTextObj = new GameObject("TempText");
//         tempTextObj.transform.SetParent(inGameUI.transform);
//         TMP_Text tmpText = tempTextObj.AddComponent<TextMeshProUGUI>();
        
//         // Set text properties
//         tmpText.text = text;
//         tmpText.color = color;
//         tmpText.fontSize = 36;
//         tmpText.alignment = TextAlignmentOptions.Center;
        
//         // Position near the timer
//         if (GameObject.Find("TimerText") != null)
//         {
//             RectTransform timerRect = GameObject.Find("TimerText").GetComponent<RectTransform>();
//             RectTransform textRect = tempTextObj.GetComponent<RectTransform>();
//             textRect.position = timerRect.position + new Vector3(100, 0, 0);
//             textRect.sizeDelta = new Vector2(200, 50);
//         }
        
//         // Animate the text
//         float duration = 1.5f;
//         float elapsed = 0;
        
//         while (elapsed < duration)
//         {
//             elapsed += Time.deltaTime;
//             float alpha = Mathf.Lerp(1, 0, elapsed / duration);
//             tmpText.color = new Color(color.r, color.g, color.b, alpha);
            
//             // Move upward slightly
//             tempTextObj.transform.position += new Vector3(0, 30 * Time.deltaTime, 0);
            
//             yield return null;
//         }
        
//         // Destroy the text object
//         Destroy(tempTextObj);
//     }
    
//     private void RestartLevel()
//     {
//         SceneManager.LoadScene(SceneManager.GetActiveScene().name);
//     }
    
//     // For next level button in victory panel
//     public void LoadNextLevel(string levelName)
//     {
//         SceneManager.LoadScene(levelName);
//     }

//      public void ShowGameOverPanel()
//     {
//         if (gameOverPanel != null)
//         {
//             gameOverPanel.SetActive(true);
//         }
//         else
//         {
//             Debug.LogError("GameOverPanel is not assigned in the Inspector!");
//         }
//     }
    
//     public void RestartGame()
//     {
//         SceneManager.LoadScene(SceneManager.GetActiveScene().name);
//     }

//     // For restart button in game over panel
//     public void RestartButton()
//     {
//         RestartLevel();
//     }
//     // For quit button
//     public void QuitGame()
//     {
//         #if UNITY_EDITOR
//         UnityEditor.EditorApplication.isPlaying = false;
//         #else
//         Application.Quit();
//         #endif
//     }
    
//     // Pause all moving entities
//     private void PauseGameObjects()
//     {
//         // Disable all enemies
//         foreach (EnemyAI enemy in FindObjectsOfType<EnemyAI>())
//         {
//             enemy.enabled = false;
//         }
        
//         // Disable player
//         SimplePlayerMovement player = FindObjectOfType<SimplePlayerMovement>();
//         if (player != null)
//         {
//             player.enabled = false;
//         }
//     }
    
//     // Hide player and enemies when victory
//     private void HideGameEntities()
//     {
//         Debug.Log("Hiding player and enemies");
        
//         // Hide all enemies
//         foreach (EnemyAI enemy in FindObjectsOfType<EnemyAI>())
//         {
//             // Option 1: Disable renderers
//             SpriteRenderer enemyRenderer = enemy.GetComponent<SpriteRenderer>();
//             if (enemyRenderer != null)
//             {
//                 enemyRenderer.enabled = false;
//             }
            
//             // Option 2: Hide entire game object
//             enemy.gameObject.SetActive(false);
//         }
        
//         // Hide player
//         SimplePlayerMovement player = FindObjectOfType<SimplePlayerMovement>();
//         if (player != null)
//         {
//             // Option 1: Disable renderer
//             SpriteRenderer playerRenderer = player.GetComponent<SpriteRenderer>();
//             if (playerRenderer != null)
//             {
//                 playerRenderer.enabled = false;
//             }
            
//             // Option 2: Hide entire game object
//             player.gameObject.SetActive(false);
//         }
//     }
// }


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
                GameTimer gameTimer = FindObjectOfType<GameTimer>();
                float elapsedTime = 0f;
                
                if (gameTimer != null && gameTimer.countDown)
                {
                    // Calculate elapsed time (startTime - currentTime)
                    elapsedTime = gameTimer.startTime - finalTime;
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
    
    // General game over panel display that takes reason and time
    public void ShowGameOverPanel(string reason = "Game Over!", float finalTime = 0f)
    {
        if (gameOverPanel != null)
        {
            // Set reason text if available
            if (gameOverReasonText != null)
            {
                gameOverReasonText.text = reason;
            }
            
            // Set final time if available
            if (finalTimeText != null)
            {
                finalTimeText.text = FormatTime(finalTime);
            }
            
            gameOverPanel.SetActive(true);
            
            // Hide in-game UI
            if (inGameUI) inGameUI.SetActive(false);
            
            // Pause all game objects
            PauseGameObjects();
        }
        else
        {
            Debug.LogError("GameOverPanel is not assigned in the Inspector!");
        }
    }
    
    // Button callback for Restart button
    public void RestartButton()
    {
        Debug.Log("Restart button clicked!");
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
    
    // Button callback for Quit button
    public void QuitGame()
    {
        Debug.Log("Quit button clicked!");
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #else
        Application.Quit();
        #endif
    }
    
    // Pause all moving entities
    private void PauseGameObjects()
    {
        // Disable all enemies
        foreach (EnemyAI enemy in FindObjectsOfType<EnemyAI>())
        {
            enemy.enabled = false;
        }
        
        // Disable player
        SimplePlayerMovement player = FindObjectOfType<SimplePlayerMovement>();
        if (player != null)
        {
            player.enabled = false;
        }
    }
    
    // Hide player and enemies when victory
    private void HideGameEntities()
    {
        Debug.Log("Hiding player and enemies");
        
        // Hide all enemies
        foreach (EnemyAI enemy in FindObjectsOfType<EnemyAI>())
        {
            // Option 1: Disable renderers
            SpriteRenderer enemyRenderer = enemy.GetComponent<SpriteRenderer>();
            if (enemyRenderer != null)
            {
                enemyRenderer.enabled = false;
            }
            
            // Option 2: Hide entire game object
            enemy.gameObject.SetActive(false);
        }
        
        // Hide player
        SimplePlayerMovement player = FindObjectOfType<SimplePlayerMovement>();
        if (player != null)
        {
            // Option 1: Disable renderer
            SpriteRenderer playerRenderer = player.GetComponent<SpriteRenderer>();
            if (playerRenderer != null)
            {
                playerRenderer.enabled = false;
            }
            
            // Option 2: Hide entire game object
            player.gameObject.SetActive(false);
        }
    }
}