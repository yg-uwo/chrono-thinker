using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class PlayerHealth : MonoBehaviour
{
    [Header("Health Settings")]
    public float maxHealth = 100f;
    public float currentHealth;

    [Header("UI Reference")]
    // Reference to the slider in your Canvas
    public Slider healthBar;

    private void Start()
    {
        currentHealth = maxHealth;
        UpdateHealthUI();
    }

    // Call this method whenever the player takes damage
    public void TakeDamage(float amount)
    {
        currentHealth -= amount;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
        UpdateHealthUI();

        // Start the damage flash effect
        StartCoroutine(FlashDamage());

        if (currentHealth <= 0)
        {
            Die();
        }
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
        // Example: Trigger game over via your UIManager
        if (GameUIManager.Instance != null)
        {
            // Here you can choose to pass a final time or any other metric
            GameUIManager.Instance.ShowGameOverPanel("You Died!", 0f);
        }
        else
        {
            Debug.LogError("UI Manager Instance not found!");
        }
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
}

