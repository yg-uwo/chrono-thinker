using UnityEngine;

public class AimIndicator : MonoBehaviour
{
    public float distance = 0.75f; // Distance from player
    public Color indicatorColor = Color.yellow;
    public float baseScale = 1.0f; // Base scale of the indicator
    public int sortingOrder = 10; // Ensure it renders above the player
    
    private SpriteRenderer spriteRenderer;
    private Transform playerTransform;
    
    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        
        if (spriteRenderer != null)
        {
            spriteRenderer.color = indicatorColor;
            spriteRenderer.sortingOrder = sortingOrder; // Make sure it renders above other sprites
        }
        else
        {
            Debug.LogError("AimIndicator requires a SpriteRenderer component!");
        }
        
        // Find the player if this is not a child of the player already
        if (transform.parent == null || !transform.parent.CompareTag("Player"))
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                // Make the indicator a child of the player
                playerTransform = player.transform;
                transform.SetParent(playerTransform, false);
                Debug.Log("AimIndicator auto-attached to player");
            }
            else
            {
                Debug.LogError("Player not found! AimIndicator won't follow the player.");
            }
        }
        else
        {
            playerTransform = transform.parent;
        }
        
        // Set initial scale
        transform.localScale = new Vector3(baseScale, baseScale, baseScale);
    }
    
    // Can be called to adjust the indicator's visibility, color, or other properties
    public void SetVisible(bool visible)
    {
        if (spriteRenderer != null)
            spriteRenderer.enabled = visible;
    }
    
    public void SetColor(Color color)
    {
        if (spriteRenderer != null)
            spriteRenderer.color = color;
    }
    
    // Set distance from player
    public void SetDistance(float newDistance)
    {
        distance = newDistance;
    }
    
    // Set a custom player transform to follow (if not using parent-child relationship)
    public void SetPlayerTransform(Transform player)
    {
        playerTransform = player;
    }
} 