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
            
            // Restart level after delay
            Invoke("RestartLevel", delayBeforeRestart);
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
            
            // Restart level after delay
            Invoke("RestartLevel", delayBeforeRestart);
        }
    }
    
    // Call this when level is completed
    public void ShowVictory(float finalTime)
    {
        if (victoryPanel && finalTimeText)
        {
            finalTimeText.text = FormatTime(finalTime);
            victoryPanel.SetActive(true);
            
            // Hide in-game UI elements
            if (inGameUI) inGameUI.SetActive(false);
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
        // Implementation details depend on your UI setup
        Debug.Log($"Time reduced by {amount} seconds!");
        
        // You could instantiate a prefab with animation here
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
    
    // For restart button in game over panel
    public void RestartButton()
    {
        RestartLevel();
    }
    
    // For quit button
    public void QuitGame()
    {
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #else
        Application.Quit();
        #endif
    }
}