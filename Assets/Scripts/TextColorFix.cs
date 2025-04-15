using UnityEngine;
using TMPro;

/// <summary>
/// Attach this script to any TextMeshPro text element that needs a unique color
/// that won't affect other text elements using the same font.
/// </summary>
public class TextColorFix : MonoBehaviour
{
    [SerializeField] private Color textColor = Color.white;
    
    private TMP_Text textComponent;
    private Material originalMaterial;
    private Material instanceMaterial;
    
    void Start()
    {
        // Get the TextMeshPro component
        textComponent = GetComponent<TMP_Text>();
        if (textComponent == null)
        {
            Debug.LogError("TextColorFix requires a TextMeshPro component!");
            return;
        }
        
        // Create a unique material instance for this text element
        CreateUniqueMaterialInstance();
        
        // Apply the desired color
        ApplyTextColor();
    }
    
    private void CreateUniqueMaterialInstance()
    {
        // Store the original shared material
        originalMaterial = textComponent.fontMaterial;
        
        // Create a unique instance of the material just for this text element
        instanceMaterial = new Material(originalMaterial);
        
        // Assign the unique material to this text element
        textComponent.fontMaterial = instanceMaterial;
    }
    
    private void ApplyTextColor()
    {
        if (textComponent != null && instanceMaterial != null)
        {
            // Set the text color (this won't affect other text elements now)
            textComponent.color = textColor;
        }
    }
    
    // This lets you change the color from the inspector or from scripts
    public void SetTextColor(Color newColor)
    {
        textColor = newColor;
        ApplyTextColor();
    }
    
    // Clean up when destroyed to avoid memory leaks
    private void OnDestroy()
    {
        if (instanceMaterial != null)
        {
            // Destroy the unique material instance
            if (Application.isPlaying)
                Destroy(instanceMaterial);
            else
                DestroyImmediate(instanceMaterial);
        }
    }
} 