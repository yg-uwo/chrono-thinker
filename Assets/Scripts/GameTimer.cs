using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class GameTimer : MonoBehaviour
{
    public float startTime = 60f;        // Starting time in seconds
    public Text timerText;               // UI Text element to display timer
    public bool countDown = true;        // If true, counts down; if false, counts up
    public float timeReductionOnCollision = 5f; // How much time to reduce when enemy hits obstacle
    
    private float currentTime;
    
    void Start()
    {
        currentTime = startTime;
        
        // Check if the timer text reference is set
        if (timerText == null)
        {
            Debug.LogWarning("Timer Text reference not set. Please assign a UI Text element.");
        }
    }
    
    void Update()
    {
        if (countDown)
        {
            currentTime -= Time.deltaTime;
            
            // Check if time is up
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
            // Format time as minutes:seconds
            int minutes = Mathf.FloorToInt(currentTime / 60);
            int seconds = Mathf.FloorToInt(currentTime % 60);
            timerText.text = string.Format("{0:00}:{1:00}", minutes, seconds);
        }
    }
    
    public void ReduceTime(float amount)
    {
        currentTime -= amount;
        if (currentTime < 0)
        {
            currentTime = 0;
            GameOver();
        }
    }
    
    void GameOver()
    {
        Debug.Log("Time's up! Game Over!");
        // Reload current level
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}