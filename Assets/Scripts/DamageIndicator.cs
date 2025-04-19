using UnityEngine;
using TMPro;
using System.Collections;

public class DamageIndicator : MonoBehaviour
{
    [Header("Appearance")]
    public float fadeTime = 1f;
    public float moveDistance = 1f;
    public Color textColor = Color.red;
    
    private TextMeshPro textMesh;
    private Vector3 initialPosition;
    private float fadeTimer;
    
    void Awake()
    {
        textMesh = GetComponent<TextMeshPro>();
        if (textMesh == null)
        {
            textMesh = gameObject.AddComponent<TextMeshPro>();
            textMesh.alignment = TextAlignmentOptions.Center;
            textMesh.fontSize = 4;
        }
        
        textMesh.color = textColor;
    }
    
    public void Setup(float damageAmount)
    {
        initialPosition = transform.position;
        textMesh.text = damageAmount.ToString();
        fadeTimer = fadeTime;
        
        // Apply the text color
        textMesh.color = textColor;
        
        StartCoroutine(FadeAndMove());
    }
    
    private IEnumerator FadeAndMove()
    {
        while (fadeTimer > 0)
        {
            fadeTimer -= Time.deltaTime;
            
            // Move upward
            float t = 1f - (fadeTimer / fadeTime);
            transform.position = initialPosition + new Vector3(0, moveDistance * t, 0);
            
            // Fade out
            Color newColor = textMesh.color;
            newColor.a = Mathf.Lerp(1f, 0f, t);
            textMesh.color = newColor;
            
            yield return null;
        }
        
        Destroy(gameObject);
    }
} 