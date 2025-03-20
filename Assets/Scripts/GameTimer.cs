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
    
    void Start()
    {
        currentTime = startTime;
        timerText = GameObject.Find("TimerText").GetComponent<TMP_Text>(); // Correct component
        
        if (timerText == null)
        {
            Debug.LogWarning("Timer Text reference not set. Please assign a TextMeshPro UI element.");
        }
    }
    
    void Update()
    {
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
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}
