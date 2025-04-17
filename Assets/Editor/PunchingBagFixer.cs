using UnityEngine;
using UnityEditor;

public class PunchingBagFixer : EditorWindow
{
    [MenuItem("Tools/Fix Punching Bags")]
    public static void FixAllPunchingBags()
    {
        PunchingBag[] punchingBags = GameObject.FindObjectsOfType<PunchingBag>();
        
        if (punchingBags.Length == 0)
        {
            Debug.LogError("No punching bags found in the scene!");
            EditorUtility.DisplayDialog("No Punching Bags Found", 
                "There are no punching bags in the current scene. Add a punching bag first.", "OK");
            return;
        }
        
        int fixedCount = 0;
        
        foreach (PunchingBag bag in punchingBags)
        {
            bool wasFixed = FixPunchingBag(bag);
            if (wasFixed) fixedCount++;
        }
        
        Debug.Log($"Fixed {fixedCount} of {punchingBags.Length} punching bags in the scene.");
        EditorUtility.DisplayDialog("Punching Bags Fixed", 
            $"Fixed {fixedCount} of {punchingBags.Length} punching bags in the scene.", "OK");
    }
    
    private static bool FixPunchingBag(PunchingBag bag)
    {
        GameObject bagObject = bag.gameObject;
        bool wasFixed = false;
        
        Undo.RecordObject(bagObject, "Fix Punching Bag");
        Undo.RecordObject(bag, "Fix Punching Bag Properties");
        
        // Check layer
        int punchableLayer = LayerMask.NameToLayer("Punchable");
        if (punchableLayer != -1 && bagObject.layer != punchableLayer)
        {
            bagObject.layer = punchableLayer;
            wasFixed = true;
            Debug.Log($"Fixed layer for {bagObject.name}");
        }
        
        // Check for rigidbody
        Rigidbody2D rb = bagObject.GetComponent<Rigidbody2D>();
        if (rb == null)
        {
            rb = bagObject.AddComponent<Rigidbody2D>();
            wasFixed = true;
            Debug.Log($"Added missing Rigidbody2D to {bagObject.name}");
        }
        
        // Fix rigidbody settings
        if (rb != null)
        {
            if (rb.bodyType != RigidbodyType2D.Dynamic)
            {
                rb.bodyType = RigidbodyType2D.Dynamic;
                wasFixed = true;
            }
            
            if (rb.gravityScale != 0f)
            {
                rb.gravityScale = 0f;
                wasFixed = true;
            }
            
            if (!rb.freezeRotation)
            {
                rb.freezeRotation = true;
                wasFixed = true;
            }
            
            if (rb.collisionDetectionMode != CollisionDetectionMode2D.Continuous)
            {
                rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
                wasFixed = true;
            }
            
            if (rb.constraints != RigidbodyConstraints2D.FreezeRotation)
            {
                rb.constraints = RigidbodyConstraints2D.FreezeRotation;
                wasFixed = true;
            }
        }
        
        // Check for collider
        Collider2D collider = bagObject.GetComponent<Collider2D>();
        if (collider == null)
        {
            CircleCollider2D circleCollider = bagObject.AddComponent<CircleCollider2D>();
            circleCollider.radius = 0.5f;
            wasFixed = true;
            Debug.Log($"Added missing Collider2D to {bagObject.name}");
        }
        else if (collider.isTrigger)
        {
            collider.isTrigger = false;
            wasFixed = true;
            Debug.Log($"Fixed collider trigger setting on {bagObject.name}");
        }
        
        // Check for sprite renderer and sprite
        SpriteRenderer renderer = bagObject.GetComponent<SpriteRenderer>();
        if (renderer == null)
        {
            renderer = bagObject.AddComponent<SpriteRenderer>();
            CreateDefaultSprite(renderer);
            wasFixed = true;
            Debug.Log($"Added missing SpriteRenderer to {bagObject.name}");
        }
        else if (renderer.sprite == null)
        {
            CreateDefaultSprite(renderer);
            wasFixed = true;
            Debug.Log($"Fixed missing sprite on {bagObject.name}");
        }
        
        // Ensure the renderer is visible
        if (renderer != null)
        {
            if (renderer.sortingOrder < 5)
            {
                renderer.sortingOrder = 5;
                wasFixed = true;
                Debug.Log($"Increased sorting order to 5 for {bagObject.name}");
            }
            
            if (renderer.color.a < 1f)
            {
                Color color = renderer.color;
                color.a = 1f;
                renderer.color = color;
                wasFixed = true;
                Debug.Log($"Fixed alpha/transparency for {bagObject.name}");
            }
        }
        
        // Check for scale - make sure it's not too small to see
        if (bagObject.transform.localScale.magnitude < 1f)
        {
            bagObject.transform.localScale = Vector3.one * 3f;
            wasFixed = true;
            Debug.Log($"Increased scale for {bagObject.name} - was too small to see");
        }
        
        // Check for anchor point
        if (bag.anchorPoint == null)
        {
            // Try to find a suitable anchor in the parent
            Transform parent = bagObject.transform.parent;
            if (parent != null)
            {
                foreach (Transform child in parent)
                {
                    if (child != bagObject.transform && child.name.Contains("Anchor"))
                    {
                        bag.anchorPoint = child;
                        wasFixed = true;
                        Debug.Log($"Found and assigned anchor point for {bagObject.name}");
                        break;
                    }
                }
            }
            
            // If no anchor was found, create one
            if (bag.anchorPoint == null)
            {
                GameObject anchorGO = new GameObject(bagObject.name + "_Anchor");
                if (parent != null)
                {
                    anchorGO.transform.SetParent(parent);
                }
                anchorGO.transform.position = bagObject.transform.position;
                
                // Add static rigidbody to anchor
                Rigidbody2D anchorRb = anchorGO.AddComponent<Rigidbody2D>();
                anchorRb.bodyType = RigidbodyType2D.Static;
                
                bag.anchorPoint = anchorGO.transform;
                wasFixed = true;
                Debug.Log($"Created new anchor point for {bagObject.name}");
            }
        }
        
        if (wasFixed)
        {
            EditorUtility.SetDirty(bagObject);
            EditorUtility.SetDirty(bag);
        }
        
        return wasFixed;
    }
    
    private static void CreateDefaultSprite(SpriteRenderer renderer)
    {
        // Try to load a circle sprite first from the project
        Sprite circleSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Circle.png");
        
        // If that fails, use Unity's built-in sprite
        if (circleSprite == null)
        {
            circleSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
        }
        
        // If we still don't have a sprite, create one
        if (circleSprite == null)
        {
            Texture2D texture = new Texture2D(128, 128);
            Color[] colors = new Color[texture.width * texture.height];
            for (int i = 0; i < colors.Length; i++)
            {
                colors[i] = Color.clear;
            }
            
            // Draw a filled circle
            int radius = texture.width / 2;
            Vector2 center = new Vector2(texture.width / 2, texture.height / 2);
            for (int y = 0; y < texture.height; y++)
            {
                for (int x = 0; x < texture.width; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), center);
                    if (dist <= radius)
                    {
                        colors[y * texture.width + x] = Color.white;
                    }
                }
            }
            
            texture.SetPixels(colors);
            texture.Apply();
            
            circleSprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100f);
        }
        
        renderer.sprite = circleSprite;
        renderer.color = Color.red; // Bright red to be easy to see
        renderer.sortingOrder = 10; // Very high sorting order to ensure visibility
    }
} 