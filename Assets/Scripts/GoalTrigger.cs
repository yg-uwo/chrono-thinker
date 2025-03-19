// using UnityEngine;

// public class GoalTrigger : MonoBehaviour
// {
//     private void OnTriggerEnter2D(Collider2D other)
//     {
//         if (other.CompareTag("Player"))
//         {
//             Debug.Log("Level Complete!");
//             // Add logic to transition to the next level or display a victory screen.
//         }
//     }
// }

using UnityEngine;
using UnityEngine.SceneManagement;

public class GoalTrigger : MonoBehaviour 
{
    public string nextLevelName = "Level2"; // Set this in inspector
    public float completionDelay = 1.0f;    // Time before loading next level
    public bool isFinalLevel = false;       // Is this the final level?
    
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            Debug.Log("Level Complete!");
            // Disable player movement to prevent further input
            other.GetComponent<SimplePlayerMovement>().enabled = false;
            
            // Handle level completion
            StartCoroutine(CompleteLevel());
        }
    }
    
    private System.Collections.IEnumerator CompleteLevel()
    {
        // Wait for specified delay (for victory effects)
        yield return new WaitForSeconds(completionDelay);
        
        // If it's the final level, you could load a victory screen
        if (isFinalLevel)
        {
            // For now, just reload the current level
            // Later you can create a Victory scene
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
        else
        {
            // Load the next level
            SceneManager.LoadScene(nextLevelName);
        }
    }
}