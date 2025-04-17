using UnityEngine;
using UnityEditor;

public class LocatePunchingBag : EditorWindow
{
    [MenuItem("Tools/Find Punching Bag in Scene")]
    public static void FindPunchingBag()
    {
        PunchingBag[] bags = Object.FindObjectsOfType<PunchingBag>();
        
        if (bags.Length == 0)
        {
            Debug.LogError("No punching bags found in the scene!");
            EditorUtility.DisplayDialog("No Punching Bags Found", 
                "There are no punching bags in the current scene.", "OK");
            return;
        }
        
        foreach (PunchingBag bag in bags)
        {
            // Log information about the bag
            GameObject obj = bag.gameObject;
            Debug.Log($"Found PunchingBag at position {obj.transform.position}, with scale {obj.transform.localScale}");
            
            // Check if it has a renderer
            SpriteRenderer sr = obj.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                Debug.Log($"Renderer: Enabled={sr.enabled}, Sprite={(sr.sprite != null ? sr.sprite.name : "null")}, Color={sr.color}, Order={sr.sortingOrder}");
                
                // Check if sprite is missing and fix it
                if (sr.sprite == null)
                {
                    Sprite builtinSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
                    if (builtinSprite != null)
                    {
                        sr.sprite = builtinSprite;
                        Debug.Log("Fixed missing sprite by assigning Unity's built-in Knob sprite");
                    }
                }
                
                // Force visibility settings
                sr.color = Color.red;
                sr.sortingOrder = 20;
                Debug.Log("Set color to red and sorting order to 20");
            }
            else
            {
                Debug.LogError("PunchingBag has no SpriteRenderer component!");
            }
            
            // Select the object in the scene
            Selection.activeGameObject = obj;
            SceneView.FrameLastActiveSceneView();
            
            // Make the scene view focus on the object
            SceneView.lastActiveSceneView.FrameSelected();
            Debug.Log("Focused scene view on PunchingBag");
            
            // Mark the scene as dirty
            EditorUtility.SetDirty(obj);
            EditorUtility.SetDirty(bag);
        }
    }
    
    [MenuItem("Tools/Reset Punching Bag to Anchor")]
    public static void ResetPunchingBagPosition()
    {
        PunchingBag[] bags = Object.FindObjectsOfType<PunchingBag>();
        
        if (bags.Length == 0)
        {
            Debug.LogError("No punching bags found in the scene!");
            EditorUtility.DisplayDialog("No Punching Bags Found", 
                "There are no punching bags in the current scene.", "OK");
            return;
        }
        
        foreach (PunchingBag bag in bags)
        {
            if (bag.anchorPoint != null)
            {
                // Reset position to anchor
                bag.transform.position = bag.anchorPoint.position;
                Debug.Log($"Reset PunchingBag position to its anchor at {bag.anchorPoint.position}");
                
                // Select the object in the scene
                Selection.activeGameObject = bag.gameObject;
                SceneView.FrameLastActiveSceneView();
                SceneView.lastActiveSceneView.FrameSelected();
                
                // Mark the scene as dirty
                EditorUtility.SetDirty(bag.gameObject);
            }
            else
            {
                Debug.LogError("PunchingBag has no anchor point assigned!");
            }
        }
    }
} 