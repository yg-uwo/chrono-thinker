using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    public GameObject enemyPrefab;
    public Transform[] spawnPoints; // Points where enemies will spawn
    public float spawnInterval = 3f; // How often enemies spawn
    private float timer;

    void Start()
    {
        timer = spawnInterval; // Initialize timer
    }

    void Update()
    {
        timer -= Time.deltaTime;

        if (timer <= 0f)
        {
            SpawnEnemy();
            timer = spawnInterval; // Reset timer
        }
    }

    void SpawnEnemy()
    {
        foreach (var spawnPoint in spawnPoints)
        {
            Instantiate(enemyPrefab, spawnPoint.position, Quaternion.identity);
        }
    }
}

