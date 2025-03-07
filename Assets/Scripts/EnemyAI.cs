using UnityEngine;

public class EnemyAI : MonoBehaviour
{
    public float moveSpeed = 2f;
    private Transform player;

    private void Start()
    {
        // Automatically find the player GameObject by tag
        player = GameObject.FindGameObjectWithTag("Player").transform;

        // Check if the player was found
        if (player == null)
        {
            Debug.LogError("Player not found! Make sure the player has the 'Player' tag.");
        }
    }

    private void Update()
    {
        if (player != null)
        {
            // Move toward the player's position
            transform.position = Vector2.MoveTowards(transform.position, player.position, moveSpeed * Time.deltaTime);
        }
    }
}