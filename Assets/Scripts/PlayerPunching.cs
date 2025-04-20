using UnityEngine;
using UnityEngine.UI;

public class PlayerPunching : MonoBehaviour
{
    [Header("Punch Settings")]
    public float quickPunchPower = 5f;
    public float maxChargedPunchPower = 12f;
    public float chargeTime = 2.5f; // Increased from 1.5f to make charging slower
    public float punchRange = 1.5f; // Increased from 0.5f to allow punching from a more reasonable distance
    public LayerMask punchableLayer;
    
    [Header("Visual Indicators")]
    public GameObject aimIndicator;
    public Slider chargeBar;
    public GameObject aimIndicatorPrefab; // Prefab to instantiate if aimIndicator is not set
    
    [Header("Audio")]
    public AudioClip punchSound;
    public AudioClip chargeSound;
    private AudioSource audioSource;
    private bool isPlayingChargeSound = false;
    
    private bool isCharging = false;
    private float currentChargeTime = 0f;
    private Camera mainCamera;
    private Rigidbody2D rb;
    private GameObject chargeBarInstance;
    private AimIndicator aimIndicatorComponent;
    
    void Start()
    {
        mainCamera = Camera.main;
        rb = GetComponent<Rigidbody2D>();
        
        // Setup the aim indicator
        SetupAimIndicator();
        
        // Setup the charge bar
        SetupChargeBar();
        
        // Setup audio source
        SetupAudioSource();
    }
    
    private void SetupAudioSource()
    {
        // Get existing audio source or add a new one
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 0f; // 2D sound
        }
        
        // Attempt to load audio clips from Resources if not assigned
        if (punchSound == null)
        {
            punchSound = Resources.Load<AudioClip>("Audio/SFX/Punch");
            if (punchSound == null) Debug.LogWarning("Punch sound not found in Resources/Audio/SFX/Punch");
        }
        
        if (chargeSound == null)
        {
            chargeSound = Resources.Load<AudioClip>("Audio/SFX/Charge");
            if (chargeSound == null) Debug.LogWarning("Charge sound not found in Resources/Audio/SFX/Charge");
        }
    }
    
    private void SetupAimIndicator()
    {
        // If aim indicator is not assigned, try to create one
        if (aimIndicator == null)
        {
            // Check if we have a prefab to instantiate
            if (aimIndicatorPrefab != null)
            {
                // Instantiate the prefab as a child of this GameObject
                aimIndicator = Instantiate(aimIndicatorPrefab, transform.position, Quaternion.identity, transform);
                Debug.Log("Created aim indicator from prefab");
            }
            else
            {
                // Try to find an existing AimIndicator in the scene
                AimIndicator existingIndicator = FindObjectOfType<AimIndicator>();
                if (existingIndicator != null)
                {
                    aimIndicator = existingIndicator.gameObject;
                    Debug.Log("Found existing aim indicator in scene");
                }
                else
                {
                    // Create a simple aim indicator
                    aimIndicator = new GameObject("AimIndicator");
                    aimIndicator.transform.SetParent(transform, false);
                    
                    // Add sprite renderer
                    SpriteRenderer renderer = aimIndicator.AddComponent<SpriteRenderer>();
                    
                    // Create a simple triangle sprite
                    renderer.sprite = CreateTriangleSprite();
                    renderer.color = Color.yellow;
                    renderer.sortingOrder = 10; // Ensure it's visible above other sprites
                    
                    // Add the AimIndicator component
                    aimIndicatorComponent = aimIndicator.AddComponent<AimIndicator>();
                    
                    Debug.Log("Created a new aim indicator");
                }
            }
        }
        
        // Get the AimIndicator component (if we don't have it yet)
        if (aimIndicatorComponent == null && aimIndicator != null)
        {
            aimIndicatorComponent = aimIndicator.GetComponent<AimIndicator>();
        }
        
        // Ensure the aim indicator is properly setup
        if (aimIndicator != null)
        {
            // Make sure it's a child of the player
            if (aimIndicator.transform.parent != transform)
            {
                aimIndicator.transform.SetParent(transform, false);
                aimIndicator.transform.localPosition = Vector3.zero;
            }
            
            // Make sure it has appropriate settings
            if (aimIndicatorComponent != null)
            {
                aimIndicatorComponent.SetPlayerTransform(transform);
                aimIndicatorComponent.SetVisible(true);
            }
        }
        else
        {
            Debug.LogWarning("Aim indicator not assigned and could not be created. Player punching will work but without visual indicator.");
        }
    }
    
    // Create a simple triangle sprite for aiming
    private Sprite CreateTriangleSprite()
    {
        // Create a texture for a simple triangle
        int textureSize = 32;
        Texture2D texture = new Texture2D(textureSize, textureSize);
        
        // Fill with transparent pixels
        Color[] pixels = new Color[texture.width * texture.height];
        for (int i = 0; i < pixels.Length; i++)
        {
            pixels[i] = Color.clear;
        }
        
        // Draw a triangle pointing downward (90 degrees clockwise from pointing right)
        int midX = textureSize / 2;
        int bottom = textureSize - 1;
        int triangleWidth = textureSize / 2;
        
        // Define triangle points
        Vector2Int[] trianglePoints = new Vector2Int[] 
        {
            new Vector2Int(midX, 0),                        // Top point
            new Vector2Int(midX - triangleWidth/2, bottom), // Bottom-left
            new Vector2Int(midX + triangleWidth/2, bottom)  // Bottom-right
        };
        
        // Draw filled triangle
        FillTriangle(texture, trianglePoints[0], trianglePoints[1], trianglePoints[2], Color.white);
        
        texture.Apply();
        
        // Create sprite from the texture with pivot at top-center
        return Sprite.Create(
            texture,
            new Rect(0, 0, textureSize, textureSize),
            new Vector2(0.5f, 1.0f), // Pivot at top-center
            100f
        );
    }
    
    // Helper method to fill a triangle in a texture
    private void FillTriangle(Texture2D texture, Vector2Int p1, Vector2Int p2, Vector2Int p3, Color color)
    {
        // Sort points by Y-coordinate (p1 lowest, p3 highest)
        if (p1.y > p2.y) { Vector2Int temp = p1; p1 = p2; p2 = temp; }
        if (p2.y > p3.y) { Vector2Int temp = p2; p2 = p3; p3 = temp; }
        if (p1.y > p2.y) { Vector2Int temp = p1; p1 = p2; p2 = temp; }
        
        // Triangle filling algorithm (scan line method)
        int total_height = p3.y - p1.y;
        
        // First half of the triangle (bottom)
        for (int y = p1.y; y <= p2.y; y++)
        {
            int segment_height = p2.y - p1.y + 1;
            float alpha = (float)(y - p1.y) / total_height;
            float beta = (float)(y - p1.y) / segment_height;
            
            int A_x = p1.x + (int)((p3.x - p1.x) * alpha);
            int B_x = p1.x + (int)((p2.x - p1.x) * beta);
            
            // Ensure A is on the left of B
            if (A_x > B_x) { int temp = A_x; A_x = B_x; B_x = temp; }
            
            // Draw the horizontal line
            for (int x = A_x; x <= B_x; x++)
            {
                if (x >= 0 && x < texture.width && y >= 0 && y < texture.height)
                {
                    texture.SetPixel(x, y, color);
                }
            }
        }
        
        // Second half of the triangle (top)
        for (int y = p2.y; y <= p3.y; y++)
        {
            int segment_height = p3.y - p2.y + 1;
            float alpha = (float)(y - p1.y) / total_height;
            float beta = (float)(y - p2.y) / segment_height;
            
            int A_x = p1.x + (int)((p3.x - p1.x) * alpha);
            int B_x = p2.x + (int)((p3.x - p2.x) * beta);
            
            // Ensure A is on the left of B
            if (A_x > B_x) { int temp = A_x; A_x = B_x; B_x = temp; }
            
            // Draw the horizontal line
            for (int x = A_x; x <= B_x; x++)
            {
                if (x >= 0 && x < texture.width && y >= 0 && y < texture.height)
                {
                    texture.SetPixel(x, y, color);
                }
            }
        }
    }
    
    private void SetupChargeBar()
    {
        // If charge bar is not assigned in the inspector, create a default one
        if (chargeBar == null)
        {
            // Find or create a UI canvas
            Canvas canvas = FindObjectOfType<Canvas>();
            if (canvas == null || canvas.renderMode != RenderMode.ScreenSpaceOverlay)
            {
                GameObject canvasObj = new GameObject("UICanvas");
                canvas = canvasObj.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvasObj.AddComponent<CanvasScaler>();
                canvasObj.AddComponent<GraphicRaycaster>();
                
                Debug.Log("Created UI Canvas for charge bar");
            }
            
            // Create charge bar GameObject
            chargeBarInstance = new GameObject("ChargeBar");
            chargeBarInstance.transform.SetParent(canvas.transform, false);
            
            // Add RectTransform and set its properties
            RectTransform rectTransform = chargeBarInstance.AddComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0.5f, 0.05f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.05f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.sizeDelta = new Vector2(200, 20);
            
            // Add Slider component
            chargeBar = chargeBarInstance.AddComponent<Slider>();
            chargeBar.minValue = 0f;
            chargeBar.maxValue = 1f;
            chargeBar.value = 0f;
            chargeBar.interactable = false;
            chargeBar.transition = Selectable.Transition.None; // Disable visual transition effects
            
            // Create background
            GameObject background = new GameObject("Background");
            background.transform.SetParent(chargeBarInstance.transform, false);
            Image bgImage = background.AddComponent<Image>();
            bgImage.color = new Color(0.2f, 0.2f, 0.2f, 1f); // Make fully opaque
            
            RectTransform bgRect = background.GetComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.sizeDelta = Vector2.zero;
            
            // Create fill area
            GameObject fillArea = new GameObject("Fill Area");
            fillArea.transform.SetParent(chargeBarInstance.transform, false);
            
            RectTransform fillAreaRect = fillArea.AddComponent<RectTransform>();
            fillAreaRect.anchorMin = new Vector2(0, 0);
            fillAreaRect.anchorMax = new Vector2(1, 1);
            fillAreaRect.offsetMin = new Vector2(5, 5);
            fillAreaRect.offsetMax = new Vector2(-5, -5);
            
            // Create fill
            GameObject fill = new GameObject("Fill");
            fill.transform.SetParent(fillArea.transform, false);
            
            Image fillImage = fill.AddComponent<Image>();
            fillImage.color = new Color(1f, 1f, 0f, 1f); // Fully opaque yellow
            
            RectTransform fillRect = fill.GetComponent<RectTransform>();
            fillRect.pivot = new Vector2(0, 0.5f);
            fillRect.anchorMin = new Vector2(0, 0);
            fillRect.anchorMax = new Vector2(1, 1);
            fillRect.sizeDelta = Vector2.zero;
            
            // Set up slider without a handle
            chargeBar.fillRect = fillRect;
            chargeBar.handleRect = null; // Ensure no handle is used
            chargeBar.targetGraphic = null; // No target graphic for transitions
            
            Debug.Log("Created default charge bar");
        }
        else
        {
            // If we're using an existing charge bar, disable the handle
            if (chargeBar.handleRect != null)
            {
                // Hide the handle if it exists
                chargeBar.handleRect.gameObject.SetActive(false);
            }
            
            // Disable any visual transitions
            chargeBar.transition = Selectable.Transition.None;
            chargeBar.targetGraphic = null;
        }
        
        // Make sure the charge bar is accessible
        if (chargeBar != null)
        {
            // Make sure it's always visible initially but with zero value
            chargeBar.gameObject.SetActive(true);
            chargeBar.value = 0f;
            
            // Ensure all images in the charge bar have full opacity
            Image[] chargeBarImages = chargeBar.GetComponentsInChildren<Image>(true);
            foreach (Image img in chargeBarImages)
            {
                Color c = img.color;
                c.a = 1f;
                img.color = c;
                img.enabled = true;
            }
            
            // Check if fillRect is properly set
            if (chargeBar.fillRect == null)
            {
                Debug.LogError("Fill rect not set on charge bar!");
                // Try to find and set it automatically
                Transform fillArea = chargeBar.transform.Find("Fill Area");
                if (fillArea != null)
                {
                    Transform fill = fillArea.Find("Fill");
                    if (fill != null)
                    {
                        chargeBar.fillRect = fill.GetComponent<RectTransform>();
                        Debug.Log("Automatically set fill rect for charge bar");
                    }
                }
            }
            
            // If fillRect has an Image component, ensure it's visible
            if (chargeBar.fillRect != null)
            {
                Image fillImage = chargeBar.fillRect.GetComponent<Image>();
                if (fillImage != null)
                {
                    Color c = fillImage.color;
                    c.a = 1f;
                    fillImage.color = c;
                    fillImage.enabled = true;
                }
            }
            
            // Find and disable any handle slide area or handle objects
            Transform handleSlideArea = chargeBar.transform.Find("Handle Slide Area");
            if (handleSlideArea != null)
            {
                handleSlideArea.gameObject.SetActive(false);
            }
            
            // Add a label above the charge bar
            GameObject labelObj = new GameObject("ChargeLabel");
            labelObj.transform.SetParent(chargeBar.transform, false);
            
            // Setup the label position
            RectTransform labelRect = labelObj.AddComponent<RectTransform>();
            labelRect.anchorMin = new Vector2(0.5f, 1f);
            labelRect.anchorMax = new Vector2(0.5f, 1f);
            labelRect.pivot = new Vector2(0.5f, 0f);
            labelRect.sizeDelta = new Vector2(200, 20);
            labelRect.anchoredPosition = new Vector2(0, 5f);
            
            // Add text component
            Text labelText = labelObj.AddComponent<Text>();
            labelText.text = "CHARGE";
            labelText.alignment = TextAnchor.LowerCenter;
            labelText.color = Color.white;
            labelText.fontSize = 14;
            
            // Find a font to use
            Font[] fonts = Resources.FindObjectsOfTypeAll<Font>();
            if (fonts.Length > 0)
            {
                labelText.font = fonts[0];
            }
            
            Debug.Log("Charge bar setup complete");
        }
        else
        {
            Debug.LogError("Failed to create or find a charge bar!");
        }
    }
    
    void Update()
    {
        // Update aim direction
        UpdateAimDirection();
        
        // Handle punching input
        HandlePunchInput();
        
        // Always update the charge bar (even when not charging, to show it properly)
        UpdateChargeBar();
    }
    
    void UpdateAimDirection()
    {
        if (aimIndicator != null)
        {
            // Get mouse position in world space
            Vector3 mousePos = mainCamera.ScreenToWorldPoint(Input.mousePosition);
            mousePos.z = 0;
            
            // Calculate direction from player to mouse
            Vector3 direction = (mousePos - transform.position).normalized;
            
            // Update aim indicator position and rotation
            aimIndicator.transform.position = transform.position;
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg + 90f;
            aimIndicator.transform.rotation = Quaternion.Euler(0, 0, angle);
        }
    }
    
    void UpdateChargeBar()
    {
        if (chargeBar != null)
        {
            // If charging, update the value based on charge time
            if (isCharging)
            {
                // Calculate charge percentage
                float chargePercentage = Mathf.Clamp01(currentChargeTime / chargeTime);
                
                // Log the current charge for debugging
                Debug.Log($"Charge bar value: {chargePercentage}, Current time: {currentChargeTime}, Max time: {chargeTime}");
                
                // Update the slider value directly
                chargeBar.value = chargePercentage;
                
                // Change color based on charge percentage
                if (chargeBar.fillRect != null)
                {
                    Image fillImage = chargeBar.fillRect.GetComponent<Image>();
                    if (fillImage != null)
                    {
                        // Ensure the fill image is fully opaque
                        Color newColor = Color.Lerp(Color.yellow, Color.red, chargePercentage);
                        newColor.a = 1f;
                        fillImage.color = newColor;
                        fillImage.enabled = true;
                    }
                    else
                    {
                        Debug.LogWarning("Fill image not found on charge bar!");
                    }
                }
                else
                {
                    Debug.LogWarning("Fill rect not set on charge bar!");
                }
            }
            else
            {
                // When not charging, show empty bar
                chargeBar.value = 0f;
            }
            
            // Make sure the charge bar is visible
            if (!chargeBar.gameObject.activeInHierarchy)
            {
                chargeBar.gameObject.SetActive(true);
            }
        }
    }
    
    void HandlePunchInput()
    {
        // Left click for quick punch
        if (Input.GetMouseButtonDown(0))
        {
            QuickPunch();
        }
        
        // Right click to charge
        if (Input.GetMouseButtonDown(1))
        {
            StartCharging();
        }
        
        // Charge while holding right button
        if (Input.GetMouseButton(1) && isCharging)
        {
            ContinueCharging();
        }
        
        // Release charge when right button is released
        if (Input.GetMouseButtonUp(1) && isCharging)
        {
            ReleaseChargedPunch();
        }
    }
    
    void QuickPunch()
    {
        Vector3 punchDirection = GetPunchDirection();
        
        // Play punch sound
        if (punchSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(punchSound);
        }
        
        // First check if any punching bag is within range, regardless of direction
        Collider2D[] hitColliders = Physics2D.OverlapCircleAll(transform.position, punchRange, punchableLayer);
        foreach (Collider2D hitCollider in hitColliders)
        {
            PunchingBag punchingBag = hitCollider.GetComponent<PunchingBag>();
            if (punchingBag != null)
            {
                // Check if the player is close enough to the punching bag based on the bag's setting
                if (!punchingBag.IsPlayerInPunchRange(transform.position))
                {
                    continue; // Skip this bag if player is too far away based on bag's own range check
                }
                
                // Check if the punching bag is in the direction the player is facing
                Vector2 toPunchingBag = (hitCollider.transform.position - transform.position).normalized;
                float dot = Vector2.Dot(punchDirection, toPunchingBag);
                
                // If the punching bag is roughly in front of the player (within ~60 degrees)
                if (dot > 0.5f)
                {
                    punchingBag.ApplyPunchForce(punchDirection, quickPunchPower);
                    break; // Only punch one bag at a time
                }
            }
        }
    }
    
    void StartCharging()
    {
        // Set charging flag
        isCharging = true;
        currentChargeTime = 0f;
        
        // Play charge sound
        if (chargeSound != null && audioSource != null && !isPlayingChargeSound)
        {
            audioSource.clip = chargeSound;
            audioSource.loop = true;
            audioSource.Play();
            isPlayingChargeSound = true;
        }
    }
    
    void ContinueCharging()
    {
        // Add to charge time
        currentChargeTime += Time.deltaTime;
        currentChargeTime = Mathf.Clamp(currentChargeTime, 0f, chargeTime);
    }
    
    void ReleaseChargedPunch()
    {
        // Stop charge sound
        if (audioSource != null && isPlayingChargeSound)
        {
            audioSource.Stop();
            audioSource.loop = false;
            isPlayingChargeSound = false;
        }
        
        // Play punch sound
        if (punchSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(punchSound);
        }
        
        Debug.Log($"Released charged punch with power level: {currentChargeTime/chargeTime}");
        
        // Calculate punch power based on charge time
        float chargeFactor = Mathf.Clamp01(currentChargeTime / chargeTime);
        float punchPower = Mathf.Lerp(quickPunchPower, maxChargedPunchPower, chargeFactor);
        
        Vector3 punchDirection = GetPunchDirection();
        
        // First check if any punching bag is within range, regardless of direction
        Collider2D[] hitColliders = Physics2D.OverlapCircleAll(transform.position, punchRange, punchableLayer);
        foreach (Collider2D hitCollider in hitColliders)
        {
            PunchingBag punchingBag = hitCollider.GetComponent<PunchingBag>();
            if (punchingBag != null)
            {
                // Check if the player is close enough to the punching bag based on the bag's setting
                if (!punchingBag.IsPlayerInPunchRange(transform.position))
                {
                    continue; // Skip this bag if player is too far away based on bag's own range check
                }
                
                // Check if the punching bag is in the direction the player is facing
                Vector2 toPunchingBag = (hitCollider.transform.position - transform.position).normalized;
                float dot = Vector2.Dot(punchDirection, toPunchingBag);
                
                // If the punching bag is roughly in front of the player (within ~60 degrees)
                if (dot > 0.5f)
                {
                    punchingBag.ApplyPunchForce(punchDirection, punchPower);
                    break; // Only punch one bag at a time
                }
            }
        }
        
        // Reset charging state
        isCharging = false;
        currentChargeTime = 0f;
    }
    
    Vector3 GetPunchDirection()
    {
        // Get mouse position in world space
        Vector3 mousePos = mainCamera.ScreenToWorldPoint(Input.mousePosition);
        mousePos.z = 0;
        
        // Calculate direction from player to mouse
        return (mousePos - transform.position).normalized;
    }

    // Draw the punch range in the editor for visualization
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, punchRange);
    }
} 