// using UnityEngine;
// using UnityEngine.SceneManagement;
// using TMPro; 

// public class GameTimer : MonoBehaviour
// {
//     public float startTime = 60f;        // Starting time in seconds
//     public TMP_Text timerText;           // Use TMP_Text instead of Text
//     public bool countDown = true;        // If true, counts down; if false, counts up
//     public float timeReductionOnCollision = 5f; // Time reduction on collision
    
//     private float currentTime;
//     private bool isGameOver = false;
    
//     void Start()
//     {
//         currentTime = startTime;
//         timerText = GameObject.Find("TimerText")?.GetComponent<TMP_Text>(); // Correct component
        
//         if (timerText == null)
//         {
//             Debug.LogWarning("Timer Text reference not set. Please assign a TextMeshPro UI element.");
//         }
//     }
    
//     void Update()
//     {
//         if (isGameOver)
//             return;
            
//         if (countDown)
//         {
//             currentTime -= Time.deltaTime;
            
//             if (currentTime <= 0)
//             {
//                 currentTime = 0;
//                 GameOver();
//             }
//         }
//         else
//         {
//             currentTime += Time.deltaTime;
//         }
        
//         UpdateTimerDisplay();
//     }
    
//     void UpdateTimerDisplay()
//     {
//         if (timerText != null)
//         {
//             int minutes = Mathf.FloorToInt(currentTime / 60);
//             int seconds = Mathf.FloorToInt(currentTime % 60);
//             timerText.text = string.Format("{0:00}:{1:00}", minutes, seconds);
//         }
//     }
    
//     public void ReduceTime(float amount)
//     {
//         if (isGameOver)
//             return;
            
//         currentTime -= amount;
        
//         // Show UI notification
//         GameUIManager.Instance?.ShowTimeReduction(amount);
        
//         if (currentTime < 0)
//         {
//             currentTime = 0;
//             GameOver();
//         }
//     }
    
//     void GameOver()
//     {
//         isGameOver = true;
//         Debug.Log("Time's up! Game Over!");
        
//         // Show game over UI
//         GameUIManager.Instance?.ShowTimeUpGameOver(currentTime);
        
//         // Disable player movement
//         SimplePlayerMovement playerMovement = FindObjectOfType<SimplePlayerMovement>();
//         if (playerMovement != null)
//         {
//             playerMovement.enabled = false;
//         }
//     }
    
//     public float GetCurrentTime()
//     {
//         return currentTime;
//     }
// }

using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class GameTimer : MonoBehaviour
{
    public float startTime = 60f;        // Starting time in seconds
    public TMP_Text timerText;           // Use TMP_Text instead of Text
    public bool countDown = true;        // If true, counts down; if false, counts up
    public float timeReductionOnCollision = 5f; // Time reduction on collision
    
    private float currentTime;
    private bool isGameOver = false;
    private GameUIManager uiManager;
    
    void Start()
    {
        currentTime = startTime;
        
        // Try to find timer text if not assigned
        if (timerText == null)
        {
            timerText = GameObject.Find("TimerText")?.GetComponent<TMP_Text>();
            
            if (timerText == null)
            {
                Debug.LogWarning("Timer Text reference not set. Please assign a TextMeshPro UI element.");
            }
        }
        
        // Find UI Manager
        uiManager = FindObjectOfType<GameUIManager>();
        if (uiManager == null)
        {
            Debug.LogWarning("GameUIManager not found! UI notifications won't work.");
        }
        
        // Initial update of timer display
        UpdateTimerDisplay();
    }
    
    void Update()
    {
        if (isGameOver)
            return;
            
        if (countDown)
        {
            currentTime -= Time.deltaTime;
            
            if (currentTime <= 0)
            {
                currentTime = 0;
                GameOver();
            }
        }
        else
        {
            currentTime += Time.deltaTime;
        }
        
        UpdateTimerDisplay();
    }
    
    void UpdateTimerDisplay()
    {
        if (timerText != null)
        {
            int minutes = Mathf.FloorToInt(currentTime / 60);
            int seconds = Mathf.FloorToInt(currentTime % 60);
            timerText.text = string.Format("{0:00}:{1:00}", minutes, seconds);
        }
    }
    
    public void ReduceTime(float amount)
    {
        if (isGameOver)
            return;
            
        Debug.Log($"Reducing time by {amount} seconds. Current time before reduction: {currentTime}");
        
        // Calculate new time
        currentTime -= amount;
        
        // Update the timer display immediately
        UpdateTimerDisplay();
        
        // Show UI notification
        if (uiManager != null)
        {
            uiManager.ShowTimeReduction(amount);
        }
        
        Debug.Log($"New time after reduction: {currentTime}");
        
        if (currentTime <= 0)
        {
            currentTime = 0;
            UpdateTimerDisplay(); // Update display once more
            GameOver();
        }
    }
    
    void GameOver()
    {
        if (isGameOver) return; // Prevent multiple calls
        
        isGameOver = true;
        Debug.Log("Time's up! Game Over!");
        
        // Show game over UI
        if (uiManager != null)
        {
            uiManager.ShowTimeUpGameOver(currentTime);
        }
        else
        {
            // Fallback if no UI manager
            Debug.LogWarning("No UI Manager found. Restarting level directly.");
            Invoke("RestartLevel", 2f);
        }
        
        // Disable player movement
        SimplePlayerMovement playerMovement = FindObjectOfType<SimplePlayerMovement>();
        if (playerMovement != null)
        {
            playerMovement.enabled = false;
        }
    }
    
    private void RestartLevel()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
    
    public float GetCurrentTime()
    {
        return currentTime;
    }
}
