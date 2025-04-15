using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Unified level manager that automatically configures level-specific settings.
/// Add one of these to each level scene.
/// </summary>
public class LevelManager : MonoBehaviour
{
    [Header("Level Configuration")]
    [SerializeField] private string levelName; // Will be auto-detected if empty
    
    [Header("Timer Settings")]
    [SerializeField] private float timerDuration = 60f; // Default for Level1
    
    [Header("Goal Settings")]
    [SerializeField] private string nextLevelName = "Level2";
    [SerializeField] private bool isFinalLevel = false;
    
    // References to required components
    private GameTimer gameTimer;
    private GoalTrigger goalTrigger;
    
    void Awake()
    {
        // Auto-detect which level we're in if not specified
        if (string.IsNullOrEmpty(levelName))
        {
            levelName = SceneManager.GetActiveScene().name;
        }
        
        // Apply preset configurations based on level
        ApplyLevelPresets();
        
        // Find and configure components
        FindRequiredComponents();
        ApplyLevelSettings();
    }
    
    private void ApplyLevelPresets()
    {
        // Apply built-in presets based on level name
        switch (levelName)
        {
            case "Level1":
                timerDuration = 60f;  // Default starting time
                nextLevelName = "Level2";
                isFinalLevel = false;
                break;
                
            case "Level2":
                timerDuration = 45f;  // Shorter time for level 2
                nextLevelName = "Level3";  // This would be the next level if you create one
                isFinalLevel = true;  // Currently this is the final level
                break;
                
            // Add more cases for additional levels
                
            default:
                Debug.LogWarning($"LevelManager: No preset configuration for '{levelName}'. Using defaults.");
                break;
        }
        
        Debug.Log($"LevelManager: Applied preset for {levelName} - Timer: {timerDuration}s, Next: {nextLevelName}, Final: {isFinalLevel}");
    }
    
    private void FindRequiredComponents()
    {
        // Find the game timer
        gameTimer = FindObjectOfType<GameTimer>();
        if (gameTimer == null)
        {
            Debug.LogError($"LevelManager: No GameTimer found in {levelName}!");
        }
        
        // Find the goal trigger
        goalTrigger = FindObjectOfType<GoalTrigger>();
        if (goalTrigger == null)
        {
            Debug.LogError($"LevelManager: No GoalTrigger found in {levelName}!");
        }
    }
    
    private void ApplyLevelSettings()
    {
        // Configure the timer
        if (gameTimer != null)
        {
            gameTimer.startTime = timerDuration;
            Debug.Log($"LevelManager: Set timer duration to {timerDuration} seconds in {levelName}");
        }
        
        // Configure the goal
        if (goalTrigger != null)
        {
            goalTrigger.nextLevelName = nextLevelName;
            goalTrigger.isFinalLevel = isFinalLevel;
            Debug.Log($"LevelManager: Configured goal - Next Level: {nextLevelName}, Is Final: {isFinalLevel}");
        }
    }
    
    // For calling from the editor
    public void ManualApplySettings()
    {
        FindRequiredComponents();
        ApplyLevelSettings();
    }
}

#if UNITY_EDITOR
// Custom editor for the LevelManager with helpful buttons
[UnityEditor.CustomEditor(typeof(LevelManager))]
public class LevelManagerEditor : UnityEditor.Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        
        LevelManager manager = (LevelManager)target;
        
        GUILayout.Space(10);
        if (GUILayout.Button("Apply Level Settings"))
        {
            manager.ManualApplySettings();
        }
        
        GUILayout.Space(5);
        if (GUILayout.Button("Get Current Scene Name"))
        {
            UnityEditor.SerializedProperty levelNameProp = serializedObject.FindProperty("levelName");
            levelNameProp.stringValue = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
            serializedObject.ApplyModifiedProperties();
        }
    }
}
#endif 