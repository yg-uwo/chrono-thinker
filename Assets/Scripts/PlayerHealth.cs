using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using System.Linq;
using TMPro;

public class PlayerHealth : MonoBehaviour
{
    [Header("Health Settings")]
    public float maxHealth = 100f;
    public float currentHealth;

    [Header("UI Reference")]
    // Reference to the slider in your Canvas
    public Slider healthBar;
    
    [Header("Audio")]
    public AudioClip damageSound;
    private AudioSource audioSource;

    private void Start()
    {
        currentHealth = maxHealth;
        UpdateHealthUI();
        
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
        
        // Attempt to load audio clip from Resources if not assigned
        if (damageSound == null)
        {
            damageSound = Resources.Load<AudioClip>("Audio/SFX/PlayerDamage");
            if (damageSound == null) Debug.LogWarning("Player damage sound not found in Resources/Audio/SFX/PlayerDamage");
        }
    }

    // Call this method whenever the player takes damage
    public void TakeDamage(float amount)
    {
        currentHealth -= amount;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
        UpdateHealthUI();

        // Play damage sound
        if (damageSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(damageSound);
        }
        
        // Start the damage flash effect
        StartCoroutine(FlashDamage());
        
        // Show damage indicator
        ShowDamageIndicator(amount);

        if (currentHealth <= 0)
        {
            Die();
        }
    }
    
    // Show damage indicator near player
    private void ShowDamageIndicator(float damage)
    {
        // Skip if damage is zero
        if (damage <= 0)
            return;
            
        // Position directly above the player, but closer (reduced y offset)
        Vector3 indicatorPosition = transform.position + new Vector3(0, 0.7f, 0);
        
        // Create a new game object for the indicator
        GameObject indicatorObj = new GameObject("DamageIndicator_" + damage);
        indicatorObj.transform.position = indicatorPosition;
        
        // Add TextMeshPro component for the text
        TextMeshPro textMesh = indicatorObj.AddComponent<TextMeshPro>();
        textMesh.text = damage.ToString();
        textMesh.fontSize = 3f; // Smaller font size
        textMesh.alignment = TextAlignmentOptions.Center;
        
        // Define a bright orange color for player damage
        Color playerDamageColor = new Color(1f, 0.5f, 0f); // Bright orange
        
        // Set initial text color
        textMesh.color = playerDamageColor;
        
        // Add bold style for better visibility
        textMesh.fontStyle = FontStyles.Bold;
        
        // Add the DamageIndicator behavior
        DamageIndicator indicator = indicatorObj.AddComponent<DamageIndicator>();
        indicator.fadeTime = 1f;
        indicator.moveDistance = 1f;
        indicator.textColor = playerDamageColor;
        
        // Setup the indicator
        indicator.Setup(damage);
        
        Debug.Log($"Player: Created damage indicator showing {damage} at {indicatorPosition}");
    }

    // Update the health bar UI based on current health
    private void UpdateHealthUI()
    {
        if (healthBar != null)
        {
            // Since our slider ranges from 0 to 1, set its value as a fraction of maxHealth.
            healthBar.value = currentHealth / maxHealth;
        }
        else
        {
            Debug.LogWarning("HealthBar slider is not assigned in the inspector!");
        }
    }

    private void Die()
    {
        // Trigger game over via your UIManager
        if (GameUIManager.Instance != null)
        {
            // Here you can choose to pass a final time or any other metric
            GameUIManager.Instance.ShowGameOverPanel("You Died!", 0f);
            
            // GameUIManager will handle hiding the player and enemies
        }
        else
        {
            Debug.LogError("UI Manager Instance not found!");
            
            // Fallback - hide this object if UI manager isn't available
            gameObject.SetActive(false);
        }
        
        // Stop any in-progress animations or effects
        StopAllCoroutines();
    }

    private IEnumerator FlashDamage()
    {
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            Color originalColor = sr.color;
            sr.color = Color.red;  // Change to red to indicate damage
            yield return new WaitForSeconds(0.1f); // Adjust duration as needed
            sr.color = originalColor;
        }
    }

    // Public method to check if player is dead
    public bool IsDead()
    {
        return currentHealth <= 0;
    }
}

