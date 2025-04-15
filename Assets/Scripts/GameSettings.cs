using UnityEngine;

[CreateAssetMenu(fileName = "GameSettings", menuName = "Chrono-Thinker/Game Settings", order = 1)]
public class GameSettings : ScriptableObject
{
    [Header("Time Settings")]
    public float defaultStartTime = 60f;
    public float timeReductionOnCollision = 5f;
    
    [Header("Player Settings")]
    public float playerMoveSpeed = 5f;
    public float playerMaxHealth = 100f;
    
    [Header("Enemy Settings")]
    public float enemyMoveSpeed = 2f;
    public float enemyAttackDamage = 10f;
    public float enemyAttackCooldown = 2f;
    
    [Header("Level Boundaries")]
    public float minX = -4f;
    public float maxX = 4f;
    public float minY = -4f;
    public float maxY = 4f;
    
    [Header("UI Settings")]
    public float delayBeforeRestart = 2f;
    
    // Singleton pattern for quick access
    private static GameSettings _instance;
    public static GameSettings Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = Resources.Load<GameSettings>("GameSettings");
                
                // If it still doesn't exist, create a default one
                if (_instance == null)
                {
                    Debug.LogWarning("GameSettings asset not found. Using default values.");
                    _instance = CreateInstance<GameSettings>();
                }
            }
            return _instance;
        }
    }
} 