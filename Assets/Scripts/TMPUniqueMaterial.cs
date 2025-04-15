using UnityEngine;
using TMPro;

/// <summary>
/// Attach this to any TextMeshPro element to give it a unique material.
/// This allows you to change its color without affecting other text elements.
/// </summary>
public class TMPUniqueMaterial : MonoBehaviour
{
    // The color we want this specific text to have
    [SerializeField] private Color textColor = Color.white;
    
    private TMP_Text tmpText;
    private Material uniqueMaterial;
    
    void Awake()
    {
        CreateUniqueMaterial();
    }
    
    void Start()
    {
        // Double-check in Start in case other scripts modified the material
        if (tmpText != null && (tmpText.fontMaterial != uniqueMaterial || tmpText.color != textColor))
        {
            ApplyUniqueMaterial();
        }
    }
    
    private void CreateUniqueMaterial()
    {
        // Get the TextMeshPro component
        tmpText = GetComponent<TMP_Text>();
        if (tmpText == null) return;
        
        // Get the source material from which to create our unique instance
        Material sourceMaterial = tmpText.font.material;
        
        // Create a unique material instance
        uniqueMaterial = new Material(sourceMaterial);
        
        // Give it a distinct name for debugging
        uniqueMaterial.name = $"{gameObject.name}_UniqueMaterial";
        
        // Apply the unique material
        ApplyUniqueMaterial();
        
        Debug.Log($"Created unique material for {gameObject.name} with color {textColor}");
    }
    
    private void ApplyUniqueMaterial()
    {
        if (tmpText == null || uniqueMaterial == null) return;
        
        // Both properties need to be set for proper material override
        tmpText.fontSharedMaterial = uniqueMaterial;  // Set shared material reference
        tmpText.fontMaterial = uniqueMaterial;        // Set instance material
        
        // Apply the color
        SetTextColor(textColor);
    }
    
    public void SetTextColor(Color color)
    {
        if (tmpText == null) return;
        
        // Update our color property
        textColor = color;
        
        // Apply to the text component - this changes the vertex colors
        tmpText.color = color;
        
        // Also set the color on the material for good measure
        if (uniqueMaterial != null)
        {
            uniqueMaterial.SetColor("_FaceColor", color);
        }
    }
    
    // Called when properties are changed in the inspector
    void OnValidate()
    {
        // If we already have a reference to the TMP component, update its color
        if (tmpText != null && Application.isPlaying)
        {
            SetTextColor(textColor);
        }
    }
    
    // Clean up to prevent memory leaks
    void OnDestroy()
    {
        if (uniqueMaterial != null)
        {
            if (Application.isPlaying)
                Destroy(uniqueMaterial);
            else
                DestroyImmediate(uniqueMaterial);
        }
    }
} 