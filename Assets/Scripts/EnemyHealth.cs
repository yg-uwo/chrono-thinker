using UnityEngine;
using UnityEngine.UI;

public class EnemyHealth : MonoBehaviour
{
    public float maxHealth = 100f;
    public float currentHealth;
    
    // Health bar
    public GameObject healthBarPrefab;
    private GameObject healthBarInstance;
    private Slider healthBarSlider;
    private Image healthBarFillImage;
    
    // Offset position for health bar
    public Vector2 healthBarOffset = new Vector2(0, 0.8f);
    
    void Start()
    {
        currentHealth = maxHealth;
        CreateHealthBar();
    }
    
    void Update()
    {
        // Update health bar position to follow enemy
        if (healthBarInstance != null)
        {
            Vector3 worldPos = transform.position + new Vector3(healthBarOffset.x, healthBarOffset.y, 0);
            healthBarInstance.transform.position = worldPos;
            
            // Ensure the health bar is always facing the camera
            healthBarInstance.transform.rotation = Quaternion.identity;
            
            // Debug health display
            Debug.DrawLine(transform.position, worldPos, Color.red);
        }
    }
    
    private void CreateHealthBar()
    {
        // Check if the health bar prefab is assigned
        if (healthBarPrefab == null)
        {
            Debug.LogError("Health bar prefab not assigned to enemy " + gameObject.name);
            return;
        }
        
        // Find or create world space canvas
        Canvas worldCanvas = FindOrCreateWorldCanvas();
        
        // Instantiate health bar prefab
        healthBarInstance = Instantiate(healthBarPrefab, transform.position, Quaternion.identity);
        
        // Make it a child of the world canvas
        if (worldCanvas != null)
        {
            healthBarInstance.transform.SetParent(worldCanvas.transform, false);
        }
        else
        {
            Debug.LogError("Could not find or create a world canvas for health bars");
        }
        
        // Get slider component
        healthBarSlider = healthBarInstance.GetComponentInChildren<Slider>();
        if (healthBarSlider == null)
        {
            Debug.LogError("Health bar prefab must contain a Slider component!");
        }
        else
        {
            // Configure slider
            healthBarSlider.minValue = 0f;
            healthBarSlider.maxValue = 1f;
            
            // Get and store reference to fill image
            healthBarFillImage = healthBarSlider.fillRect.GetComponent<Image>();
            if (healthBarFillImage != null)
            {
                // Force full opacity
                Color fillColor = healthBarFillImage.color;
                fillColor.a = 1f;
                healthBarFillImage.color = fillColor;
                
                // Make all UI elements fully opaque
                Image[] allImages = healthBarInstance.GetComponentsInChildren<Image>(true);
                foreach (Image img in allImages)
                {
                    Color imgColor = img.color;
                    imgColor.a = 1f;
                    img.color = imgColor;
                    img.enabled = true;
                }
                
                // Make background fully opaque too
                Transform bgTrans = healthBarSlider.transform.Find("Background");
                if (bgTrans != null)
                {
                    Image backgroundImage = bgTrans.GetComponent<Image>();
                    if (backgroundImage != null)
                    {
                        Color bgColor = backgroundImage.color;
                        bgColor.a = 1f;
                        backgroundImage.color = bgColor;
                    }
                }
            }
            
            // Disable the handle if it exists
            if (healthBarSlider.handleRect != null)
            {
                healthBarSlider.handleRect.gameObject.SetActive(false);
            }
            
            // Find and disable any handle slide area
            Transform handleSlideArea = healthBarSlider.transform.Find("Handle Slide Area");
            if (handleSlideArea != null)
            {
                handleSlideArea.gameObject.SetActive(false);
            }
            
            // Ensure no transitions
            healthBarSlider.transition = Selectable.Transition.None;
            healthBarSlider.targetGraphic = null;
            
            // Set initial value - explicitly set it based on health ratio
            float healthRatio = currentHealth / maxHealth;
            healthBarSlider.value = healthRatio;
            UpdateHealthBarColor(healthRatio);
            
            Debug.Log($"Enemy {gameObject.name} health bar initialized: {currentHealth}/{maxHealth} = {healthBarSlider.value}");
        }
    }
    
    private Canvas FindOrCreateWorldCanvas()
    {
        // Try to find an existing world canvas
        Canvas[] canvases = FindObjectsOfType<Canvas>();
        foreach (Canvas canvas in canvases)
        {
            if (canvas.renderMode == RenderMode.WorldSpace && canvas.gameObject.name == "WorldSpaceCanvas")
            {
                return canvas;
            }
        }
        
        // If no world canvas found, create one
        GameObject canvasObject = new GameObject("WorldSpaceCanvas");
        Canvas newCanvas = canvasObject.AddComponent<Canvas>();
        newCanvas.renderMode = RenderMode.WorldSpace;
        
        // Add a canvas scaler
        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.dynamicPixelsPerUnit = 100f;
        
        // Add a raycaster
        canvasObject.AddComponent<GraphicRaycaster>();
        
        // Set world size
        RectTransform rectTransform = canvasObject.GetComponent<RectTransform>();
        rectTransform.sizeDelta = new Vector2(10, 10);  // 10x10 units in world space
        rectTransform.position = new Vector3(0, 0, 0);
        
        Debug.Log("Created new WorldSpaceCanvas for UI elements");
        return newCanvas;
    }
    
    public void TakeDamage(float damage)
    {
        currentHealth -= damage;
        
        // Clamp health to valid range
        currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);
        
        // Log the damage
        Debug.Log($"Enemy {gameObject.name} took {damage} damage. Health: {currentHealth}/{maxHealth}");
        
        // Update the health bar immediately
        UpdateHealthBar();
        
        if (currentHealth <= 0)
        {
            Die();
        }
    }
    
    private void UpdateHealthBar()
    {
        if (healthBarSlider != null)
        {
            // Force update in a more direct way
            float healthPercentage = currentHealth / maxHealth;
            
            // Set the slider value directly
            healthBarSlider.value = healthPercentage;
            
            // Force canvas refresh - using a different method that doesn't cause errors
            Canvas.ForceUpdateCanvases();
            
            // Ensure fill image is visible
            if (healthBarFillImage != null)
            {
                healthBarFillImage.enabled = true;
                
                // Re-apply the color with opacity to ensure it's visible
                Color color = healthBarFillImage.color;
                color.a = 1f;
                healthBarFillImage.color = color;
            }
            
            // Update color
            UpdateHealthBarColor(healthPercentage);
            
            // Force bar to refresh - can help with rendering issues
            LayoutRebuilder.ForceRebuildLayoutImmediate(healthBarSlider.GetComponent<RectTransform>());
            
            Debug.Log($"Updated health bar for {gameObject.name}: {currentHealth}/{maxHealth} = {healthPercentage}");
        }
    }
    
    private void UpdateHealthBarColor(float healthPercentage)
    {
        if (healthBarFillImage != null)
        {
            // Set color based on health percentage
            Color newColor;
            
            if (healthPercentage > 0.6f)
            {
                newColor = Color.green;
            }
            else if (healthPercentage > 0.3f)
            {
                newColor = Color.yellow;
            }
            else
            {
                newColor = Color.red;
            }
            
            // Force full opacity
            newColor.a = 1f;
            healthBarFillImage.color = newColor;
            
            // Ensure all related images are also fully opaque
            Transform parent = healthBarFillImage.transform.parent;
            while (parent != null)
            {
                Image parentImage = parent.GetComponent<Image>();
                if (parentImage != null)
                {
                    Color pColor = parentImage.color;
                    pColor.a = 1f;
                    parentImage.color = pColor;
                }
                parent = parent.parent;
            }
        }
    }
    
    private void Die()
    {
        // Destroy health bar
        if (healthBarInstance != null)
        {
            Destroy(healthBarInstance);
        }
        
        // Disable enemy AI
        EnemyAI enemyAI = GetComponent<EnemyAI>();
        if (enemyAI != null)
        {
            enemyAI.enabled = false;
        }
        
        // Play death animation if available
        Animator animator = GetComponent<Animator>();
        if (animator != null)
        {
            // Check if the animator has a "Die" parameter using the parameters array
            bool hasDieParameter = false;
            foreach (AnimatorControllerParameter param in animator.parameters)
            {
                if (param.name == "Die" && param.type == AnimatorControllerParameterType.Trigger)
                {
                    hasDieParameter = true;
                    break;
                }
            }
            
            if (hasDieParameter)
            {
                animator.SetTrigger("Die");
                // Delay destruction to allow animation to play
                Destroy(gameObject, 1f);
            }
            else
            {
                // No animation parameter, destroy immediately
                Destroy(gameObject);
            }
        }
        else
        {
            // No animation, destroy immediately
            Destroy(gameObject);
        }
    }
    
    // For debugging
    void OnGUI()
    {
        // Uncomment to debug if needed
        //Vector3 screenPos = Camera.main.WorldToScreenPoint(transform.position + new Vector3(0, 1.5f, 0));
        //GUI.Label(new Rect(screenPos.x - 50, Screen.height - screenPos.y, 100, 20), $"{currentHealth}/{maxHealth}");
    }
} 