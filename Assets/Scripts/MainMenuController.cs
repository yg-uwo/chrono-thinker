using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class MainMenuController : MonoBehaviour
{
    public string gameSceneName = "Level1";
    
    [Header("UI References")]
    public Button startButton;
    public Button quitButton;
    public TextMeshProUGUI titleText;
    public GameObject mainPanel;
    
    [Header("Animation Settings")]
    public float titlePulseSpeed = 0.5f;
    public float titlePulseAmount = 0.1f;
    
    private void Start()
    {
        // Set the game title
        if (titleText != null)
        {
            titleText.text = "CHRONO-THINKER";
        }
        
        // Add button listeners
        if (startButton != null)
        {
            startButton.onClick.AddListener(StartGame);
        }
        
        if (quitButton != null)
        {
            quitButton.onClick.AddListener(QuitGame);
        }
        
        // Ensure the main panel is visible
        if (mainPanel != null)
        {
            mainPanel.SetActive(true);
        }
    }
    
    private void Update()
    {
        // Simple animation effect for the title
        if (titleText != null)
        {
            float pulse = 1.0f + Mathf.Sin(Time.time * titlePulseSpeed) * titlePulseAmount;
            titleText.transform.localScale = new Vector3(pulse, pulse, 1f);
        }
        
        // Allow pressing Enter or Space to start
        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space))
        {
            StartGame();
        }
        
        // Allow pressing Escape to quit
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            QuitGame();
        }
    }
    
    public void StartGame()
    {
        // Use transition manager if available, otherwise load directly
        SceneTransitionManager transitionManager = FindObjectOfType<SceneTransitionManager>();
        if (transitionManager != null)
        {
            transitionManager.LoadScene(gameSceneName);
        }
        else
        {
            // Fallback to direct scene loading
            SceneManager.LoadScene(gameSceneName);
        }
    }
    
    public void QuitGame()
    {
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #else
        Application.Quit();
        #endif
    }
} 