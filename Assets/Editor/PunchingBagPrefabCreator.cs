using UnityEngine;
using UnityEditor;
using UnityEngine.UI;

public class PunchingBagPrefabCreator : EditorWindow
{
    private Sprite punchingBagSprite;
    private Sprite arrowSprite;
    private Color punchingBagColor = Color.white;
    private Color arrowColor = Color.yellow;
    private bool useCircleCollider = true;
    
    [MenuItem("Tools/Create Punching Bag Prefabs")]
    public static void ShowWindow()
    {
        GetWindow<PunchingBagPrefabCreator>("Punching Bag Creator");
    }
    
    void OnGUI()
    {
        GUILayout.Label("Punching Bag Mechanic Prefab Creator", EditorStyles.boldLabel);
        
        EditorGUILayout.Space(10);
        
        GUILayout.Label("Punching Bag Settings", EditorStyles.boldLabel);
        punchingBagSprite = (Sprite)EditorGUILayout.ObjectField("Punching Bag Sprite", punchingBagSprite, typeof(Sprite), false);
        punchingBagColor = EditorGUILayout.ColorField("Punching Bag Color", punchingBagColor);
        useCircleCollider = EditorGUILayout.Toggle("Use Circle Collider", useCircleCollider);
        
        EditorGUILayout.Space(10);
        
        GUILayout.Label("Aim Indicator Settings", EditorStyles.boldLabel);
        arrowSprite = (Sprite)EditorGUILayout.ObjectField("Arrow Sprite", arrowSprite, typeof(Sprite), false);
        arrowColor = EditorGUILayout.ColorField("Arrow Color", arrowColor);
        
        EditorGUILayout.Space(20);
        
        if (GUILayout.Button("Create Punching Bag Prefab"))
        {
            CreatePunchingBagPrefab();
        }
        
        if (GUILayout.Button("Create Aim Indicator Prefab"))
        {
            CreateAimIndicatorPrefab();
        }
        
        if (GUILayout.Button("Create Enemy Health Bar Prefab"))
        {
            CreateEnemyHealthBarPrefab();
        }
        
        EditorGUILayout.Space(10);
        
        if (GUILayout.Button("Create All Prefabs"))
        {
            CreatePunchingBagPrefab();
            CreateAimIndicatorPrefab();
            CreateEnemyHealthBarPrefab();
        }
    }
    
    private void CreatePunchingBagPrefab()
    {
        // Create parent GameObject to hold both the bag and anchor
        GameObject parent = new GameObject("PunchingBagWithAnchor");
        
        // Create the anchor
        GameObject anchor = new GameObject("PunchingBag_Anchor");
        anchor.transform.SetParent(parent.transform, false);
        Rigidbody2D anchorRb = anchor.AddComponent<Rigidbody2D>();
        anchorRb.bodyType = RigidbodyType2D.Static;
        
        // Create the punching bag GameObject
        GameObject punchingBag = new GameObject("PunchingBag");
        punchingBag.transform.SetParent(parent.transform, false);
        
        // Set the punching bag to be on the Punchable layer
        // Make sure to create this layer in the Unity Editor
        int punchableLayer = LayerMask.NameToLayer("Punchable");
        if (punchableLayer != -1)
        {
            punchingBag.layer = punchableLayer;
        }
        else
        {
            Debug.LogWarning("'Punchable' layer not found. Make sure to create it in the Unity Editor.");
        }
        
        // Add renderer first so we can determine sprite size
        SpriteRenderer renderer = punchingBag.AddComponent<SpriteRenderer>();
        
        // Set a high sorting order to ensure visibility
        renderer.sortingOrder = 10;
        
        // Set the sprite before creating the collider, so we can base collider size on sprite
        Sprite spriteToUse;
        if (punchingBagSprite != null)
        {
            spriteToUse = punchingBagSprite;
        }
        else
        {
            // Use Unity's built-in sprite instead of creating one
            spriteToUse = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
            if (spriteToUse != null)
            {
                Debug.Log("Using Unity's built-in Knob sprite for punching bag");
            }
            else
            {
                // As a fallback, create a simple sprite
                spriteToUse = CreateCircleSprite();
                Debug.Log("Created default circle sprite for punching bag");
            }
        }
        
        // Apply sprite to the renderer
        renderer.sprite = spriteToUse;
        renderer.color = punchingBagColor;
        renderer.sortingOrder = 10;
        
        // Add collider based on sprite
        Collider2D collider;
        if (useCircleCollider)
        {
            collider = punchingBag.AddComponent<CircleCollider2D>();
            if (collider is CircleCollider2D circleCollider)
            {
                // Match circle collider radius to sprite size exactly
                if (spriteToUse != null)
                {
                    // Use the smaller dimension of the sprite bounds
                    float minSize = Mathf.Min(spriteToUse.bounds.extents.x, spriteToUse.bounds.extents.y);
                    circleCollider.radius = minSize;
                    Debug.Log($"Set circle collider radius to match sprite size: {minSize}");
                }
                else
                {
                    circleCollider.radius = 0.5f;
                }
            }
        }
        else
        {
            collider = punchingBag.AddComponent<BoxCollider2D>();
            BoxCollider2D boxCollider = collider as BoxCollider2D;
            
            if (boxCollider != null && spriteToUse != null)
            {
                // Match box collider size to sprite bounds exactly
                boxCollider.size = new Vector2(
                    spriteToUse.bounds.size.x,
                    spriteToUse.bounds.size.y
                );
                Debug.Log($"Set box collider size to match sprite: {spriteToUse.bounds.size}");
            }
            else if (boxCollider != null)
            {
                boxCollider.size = new Vector2(1f, 1f);
            }
        }
        
        // Configure collider to NOT be a trigger
        collider.isTrigger = false;
        
        // Add Rigidbody2D
        Rigidbody2D rb = punchingBag.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.freezeRotation = true;
        rb.linearDamping = 0.3f;
        
        // Add the spring joint (disabled but kept for compatibility)
        SpringJoint2D springJoint = punchingBag.AddComponent<SpringJoint2D>();
        springJoint.enabled = false;
        springJoint.connectedBody = anchorRb;
        springJoint.autoConfigureDistance = false;
        springJoint.distance = 0.05f;
        springJoint.dampingRatio = 0f;
        springJoint.frequency = 0f;
        
        // Add and configure the PunchingBag script
        PunchingBag punchingBagScript = punchingBag.AddComponent<PunchingBag>();
        
        // IMPORTANT: Set the reference to the anchor in the PunchingBag script
        punchingBagScript.anchorPoint = anchor.transform;
        
        // Position the anchor at the same position as the punching bag
        punchingBag.transform.position = Vector3.zero;
        anchor.transform.position = punchingBag.transform.position;
        
        // Configure PunchingBag script with the values
        punchingBagScript.bagSize = 3.5f;
        punchingBagScript.punchForce = 10f;
        punchingBagScript.returnSpeed = 5f;
        punchingBagScript.bounceEnergyLoss = 0.1f;
        punchingBagScript.enemyDamage = 20f;
        punchingBagScript.playerKnockbackForce = 5f;
        punchingBagScript.enemyKnockbackForce = 8f;
        
        // Configure punching bag visuals
        punchingBagScript.bagColor = punchingBagColor;
        punchingBagScript.sortingOrder = 10;
        
        // Create the prefab
        string prefabPath = "Assets/Prefabs/PunchingBag.prefab";
        
        // Ensure directory exists
        if (!AssetDatabase.IsValidFolder("Assets/Prefabs"))
        {
            AssetDatabase.CreateFolder("Assets", "Prefabs");
        }
        
        // Save the prefab
        GameObject prefabAsset = PrefabUtility.SaveAsPrefabAsset(parent, prefabPath);
        if (prefabAsset != null) 
        {
            Debug.Log("Successfully created punching bag prefab at: " + prefabPath);
        }
        else
        {
            Debug.LogError("Failed to create punching bag prefab!");
        }
        
        // Clean up the scene
        DestroyImmediate(parent);
    }
    
    private Sprite CreateCircleSprite()
    {
        // Create a texture for a simple circle
        int textureSize = 128;
        Texture2D texture = new Texture2D(textureSize, textureSize);
        
        // Fill with transparent pixels
        Color[] pixels = new Color[texture.width * texture.height];
        for (int i = 0; i < pixels.Length; i++)
        {
            pixels[i] = Color.clear;
        }
        
        // Draw a circle
        float radius = textureSize / 2f;
        Vector2 center = new Vector2(textureSize / 2f, textureSize / 2f);
        
        for (int y = 0; y < textureSize; y++)
        {
            for (int x = 0; x < textureSize; x++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), center);
                if (distance <= radius)
                {
                    // Filled circle
                    pixels[y * textureSize + x] = Color.white;
                }
            }
        }
        
        texture.SetPixels(pixels);
        texture.Apply();
        
        // Create sprite from the texture
        return Sprite.Create(
            texture,
            new Rect(0, 0, textureSize, textureSize),
            new Vector2(0.5f, 0.5f), // Center pivot
            100f
        );
    }
    
    private void CreateAimIndicatorPrefab()
    {
        // Create the aim indicator GameObject
        GameObject aimIndicator = new GameObject("AimIndicator");
        
        // Add required components
        SpriteRenderer renderer = aimIndicator.AddComponent<SpriteRenderer>();
        AimIndicator aimIndicatorComponent = aimIndicator.AddComponent<AimIndicator>();
        
        // Set the sprite and color if provided
        if (arrowSprite != null)
        {
            renderer.sprite = arrowSprite;
        }
        else
        {
            // Create a simple arrow shape if no sprite is provided
            renderer.sprite = CreateTriangleSprite();
        }
        
        renderer.color = arrowColor;
        
        // Set ordering to appear above other sprites
        renderer.sortingOrder = 10;
        
        // Configure the component properties
        aimIndicatorComponent.indicatorColor = arrowColor;
        aimIndicatorComponent.distance = 0.75f;
        aimIndicatorComponent.sortingOrder = 10;
        
        // Create the prefab
        string prefabPath = "Assets/Prefabs/AimIndicator.prefab";
        
        // Ensure directory exists
        if (!AssetDatabase.IsValidFolder("Assets/Prefabs"))
        {
            AssetDatabase.CreateFolder("Assets", "Prefabs");
        }
        
        PrefabUtility.SaveAsPrefabAsset(aimIndicator, prefabPath);
        
        // Clean up the scene
        DestroyImmediate(aimIndicator);
        
        Debug.Log("Created aim indicator prefab at " + prefabPath);
    }
    
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
        if (total_height == 0) return;
        
        // First half of the triangle (bottom)
        for (int y = p1.y; y <= p2.y; y++)
        {
            int segment_height = p2.y - p1.y + 1;
            if (segment_height == 0) continue;
            
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
            if (segment_height == 0) continue;
            
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
    
    private void CreateEnemyHealthBarPrefab()
    {
        // Create a canvas for the health bar
        GameObject canvas = new GameObject("EnemyHealthBarCanvas");
        Canvas canvasComponent = canvas.AddComponent<Canvas>();
        canvasComponent.renderMode = RenderMode.WorldSpace;
        
        // Configure canvas scale
        RectTransform canvasRect = canvas.GetComponent<RectTransform>();
        canvasRect.sizeDelta = new Vector2(100, 20); // Reduced height from 30 to 20
        canvasRect.localScale = new Vector3(0.01f, 0.01f, 1f);
        
        // Add a slider for the health bar
        GameObject healthBar = new GameObject("HealthBar");
        healthBar.transform.SetParent(canvas.transform, false);
        
        // Configure rect transform
        RectTransform healthBarRect = healthBar.AddComponent<RectTransform>();
        healthBarRect.anchorMin = new Vector2(0.5f, 0.5f);
        healthBarRect.anchorMax = new Vector2(0.5f, 0.5f);
        healthBarRect.pivot = new Vector2(0.5f, 0.5f);
        healthBarRect.sizeDelta = new Vector2(80, 8); // Thinner bar (from 10 to 8)
        healthBarRect.localPosition = Vector3.zero;
        
        // Add slider component
        Slider slider = healthBar.AddComponent<Slider>();
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.value = 1f;
        slider.interactable = false;
        slider.transition = Selectable.Transition.None; // Disable transitions
        slider.targetGraphic = null; // No target graphic
        
        // Create background
        GameObject background = new GameObject("Background");
        background.transform.SetParent(healthBar.transform, false);
        Image bgImage = background.AddComponent<Image>();
        bgImage.color = new Color(0.2f, 0.2f, 0.2f, 1f); // Fully opaque background
        
        RectTransform bgRect = background.GetComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.sizeDelta = Vector2.zero;
        
        // Create fill area
        GameObject fillArea = new GameObject("Fill Area");
        fillArea.transform.SetParent(healthBar.transform, false);
        
        RectTransform fillAreaRect = fillArea.AddComponent<RectTransform>();
        fillAreaRect.anchorMin = new Vector2(0, 0);
        fillAreaRect.anchorMax = new Vector2(1, 1);
        fillAreaRect.offsetMin = new Vector2(2, 2);  // Padding from edges
        fillAreaRect.offsetMax = new Vector2(-2, -2);
        
        // Create fill
        GameObject fill = new GameObject("Fill");
        fill.transform.SetParent(fillArea.transform, false);
        
        Image fillImage = fill.AddComponent<Image>();
        fillImage.color = new Color(1f, 0f, 0f, 1f); // Fully opaque red
        
        RectTransform fillRect = fill.GetComponent<RectTransform>();
        fillRect.pivot = new Vector2(0, 0.5f);
        fillRect.anchorMin = new Vector2(0, 0);
        fillRect.anchorMax = new Vector2(1, 1);
        fillRect.sizeDelta = Vector2.zero;
        
        // Set up the slider references
        slider.fillRect = fillRect;
        
        // Don't create a handle slide area or handle - we don't want them
        slider.handleRect = null;
        
        // Create the prefab
        string prefabPath = "Assets/Prefabs/EnemyHealthBar.prefab";
        
        // Ensure directory exists
        if (!AssetDatabase.IsValidFolder("Assets/Prefabs"))
        {
            AssetDatabase.CreateFolder("Assets", "Prefabs");
        }
        
        PrefabUtility.SaveAsPrefabAsset(canvas, prefabPath);
        
        // Clean up the scene
        DestroyImmediate(canvas);
        
        Debug.Log("Created enemy health bar prefab at " + prefabPath);
    }
} 