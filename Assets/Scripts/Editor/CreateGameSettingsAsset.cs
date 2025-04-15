#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.IO;

public class CreateGameSettingsAsset
{
    [MenuItem("Chrono-Thinker/Create GameSettings Asset")]
    public static void CreateAsset()
    {
        GameSettings asset = ScriptableObject.CreateInstance<GameSettings>();

        // Create the Resources directory if it doesn't exist
        if (!Directory.Exists("Assets/Resources"))
            Directory.CreateDirectory("Assets/Resources");

        // Create the asset in the Resources folder
        AssetDatabase.CreateAsset(asset, "Assets/Resources/GameSettings.asset");
        AssetDatabase.SaveAssets();

        // Focus on the asset in the Project window
        EditorUtility.FocusProjectWindow();
        Selection.activeObject = asset;
        
        Debug.Log("GameSettings asset created in Resources folder");
    }
}
#endif 