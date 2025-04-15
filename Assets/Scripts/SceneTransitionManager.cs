using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using UnityEngine.UI;

public class SceneTransitionManager : MonoBehaviour
{
    public static SceneTransitionManager Instance { get; private set; }
    
    [Header("Transition Settings")]
    public Image fadePanel;
    public float fadeSpeed = 1.5f;
    
    private void Awake()
    {
        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            
            // Create fade panel if it doesn't exist
            if (fadePanel == null)
            {
                CreateFadePanel();
            }
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    private void Start()
    {
        // Ensure the fade panel starts transparent
        if (fadePanel != null)
        {
            fadePanel.color = new Color(0, 0, 0, 0);
            fadePanel.gameObject.SetActive(true);
        }
        
        // Fade in from black when the game starts
        StartCoroutine(FadeIn());
    }
    
    private void CreateFadePanel()
    {
        // Create a canvas that stays on top of everything
        GameObject canvasObj = new GameObject("TransitionCanvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 999; // Make sure it's on top
        
        // Add canvas scaler for proper UI scaling
        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        
        // Create the fade panel (black image that covers the screen)
        GameObject panelObj = new GameObject("FadePanel");
        panelObj.transform.SetParent(canvasObj.transform, false);
        
        fadePanel = panelObj.AddComponent<Image>();
        fadePanel.color = Color.black;
        fadePanel.raycastTarget = false; // Don't block input
        
        // Make it fill the screen
        RectTransform rect = fadePanel.rectTransform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.sizeDelta = Vector2.zero;
        
        // Add the canvas to this object
        canvasObj.transform.SetParent(transform);
    }
    
    public void LoadScene(string sceneName)
    {
        StartCoroutine(TransitionToScene(sceneName));
    }
    
    private IEnumerator TransitionToScene(string sceneName)
    {
        // Fade out to black
        yield return StartCoroutine(FadeOut());
        
        // Load the new scene
        SceneManager.LoadScene(sceneName);
        
        // Fade in from black
        yield return StartCoroutine(FadeIn());
    }
    
    private IEnumerator FadeOut()
    {
        float alpha = 0f;
        
        while (alpha < 1f)
        {
            alpha += Time.deltaTime * fadeSpeed;
            fadePanel.color = new Color(0, 0, 0, alpha);
            yield return null;
        }
        
        fadePanel.color = new Color(0, 0, 0, 1);
    }
    
    private IEnumerator FadeIn()
    {
        float alpha = 1f;
        
        while (alpha > 0f)
        {
            alpha -= Time.deltaTime * fadeSpeed;
            fadePanel.color = new Color(0, 0, 0, alpha);
            yield return null;
        }
        
        fadePanel.color = new Color(0, 0, 0, 0);
    }
} 