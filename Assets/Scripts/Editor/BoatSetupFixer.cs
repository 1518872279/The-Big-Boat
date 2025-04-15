#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

public class BoatSetupFixer : EditorWindow
{
    [MenuItem("Tools/Fix Boat Setup")]
    static void Init()
    {
        BoatSetupFixer window = (BoatSetupFixer)EditorWindow.GetWindow(typeof(BoatSetupFixer));
        window.Show();
    }

    void OnGUI()
    {
        GUILayout.Label("Boat Setup Fixer", EditorStyles.boldLabel);
        
        if (GUILayout.Button("Find and Fix Boat"))
        {
            FindAndFixBoat();
        }
        
        if (GUILayout.Button("Fix Physics Layers"))
        {
            FixPhysicsLayers();
        }
        
        EditorGUILayout.Space();
        EditorGUILayout.HelpBox("This tool helps resolve common issues with boat collision and obstacle interaction. Use 'Find and Fix Boat' to properly tag the boat and 'Fix Physics Layers' to ensure correct layer collision settings.", MessageType.Info);
    }
    
    void FindAndFixBoat()
    {
        // Find all potential boat objects
        BoatHealth[] boatHealthComponents = FindObjectsOfType<BoatHealth>();
        
        if (boatHealthComponents.Length == 0)
        {
            Debug.LogError("No GameObjects with BoatHealth component found in the scene!");
            return;
        }
        
        foreach (BoatHealth boatHealth in boatHealthComponents)
        {
            GameObject boat = boatHealth.gameObject;
            
            // Set correct tag
            if (boat.tag != "Boat" && boat.tag != "Player")
            {
                Undo.RecordObject(boat, "Set Boat Tag");
                boat.tag = "Boat";
                Debug.Log("Set tag 'Boat' on " + boat.name);
            }
            
            // Check for collider
            Collider collider = boat.GetComponent<Collider>();
            if (collider == null)
            {
                Undo.RecordObject(boat, "Add Collider to Boat");
                boat.AddComponent<BoxCollider>();
                Debug.Log("Added BoxCollider to " + boat.name);
            }
            
            // Check for Rigidbody
            Rigidbody rb = boat.GetComponent<Rigidbody>();
            if (rb == null)
            {
                Debug.LogWarning("Boat has no Rigidbody component! Collision detection may be limited.");
            }
        }
        
        Debug.Log("Boat setup fix complete!");
    }
    
    void FixPhysicsLayers()
    {
        // This function would normally modify Physics.IgnoreLayerCollision
        // But since we can't do that programmatically in a way that persists,
        // we'll just provide instructions
        
        EditorUtility.DisplayDialog("Physics Layer Setup", 
            "Please manually check these settings in Edit > Project Settings > Physics:\n\n" +
            "1. Make sure the layer containing the boat can collide with the layer containing obstacles\n" +
            "2. If using separate layers, ensure they are not in the ignore matrix\n" +
            "3. For trigger colliders, ensure both objects have Rigidbody components", 
            "OK");
    }
}
#endif 