using UnityEngine;
using UnityEngine.Events;
using StylizedWater2;

/// <summary>
/// Component responsible for initializing and setting up proper Stylized Water 2 integration in the scene.
/// Attach this to a manager GameObject in your scene to easily setup water integration.
/// </summary>
[AddComponentMenu("Water/Stylized Water Initializer")]
public class StylizedWaterInitializer : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Reference to the StylizedWater2Handler component")]
    public StylizedWater2Handler waterHandler;
    
    [Header("Setup Settings")]
    [Tooltip("Auto-initialize on scene start")]
    public bool initializeOnStart = true;
    
    [Tooltip("Create required components if they don't exist")]
    public bool createMissingComponents = true;
    
    [Header("Events")]
    [Tooltip("Event triggered when water initialization is complete")]
    public UnityEvent onWaterInitialized;
    
    private void Start()
    {
        if (initializeOnStart)
        {
            InitializeWater();
        }
    }
    
    /// <summary>
    /// Initializes the water system and all its dependencies.
    /// </summary>
    public void InitializeWater()
    {
        // Ensure we have a water handler
        if (waterHandler == null && createMissingComponents)
        {
            GameObject handlerObject = new GameObject("Water Handler");
            waterHandler = handlerObject.AddComponent<StylizedWater2Handler>();
        }
        
        if (waterHandler == null)
        {
            Debug.LogError("StylizedWaterInitializer: No StylizedWater2Handler component found and createMissingComponents is set to false.");
            return;
        }
        
        // Find main water object if needed
        if (waterHandler.mainWaterObject == null)
        {
            waterHandler.mainWaterObject = waterHandler.FindMainWaterObject();
            
            if (waterHandler.mainWaterObject == null)
            {
                Debug.LogWarning("StylizedWaterInitializer: No WaterObject found in the scene. Water integration will not function properly.");
                return;
            }
        }
        
        // Integrate all boats
        waterHandler.IntegrateAllBoats();
        
        // Trigger the event
        onWaterInitialized?.Invoke();
        
        Debug.Log("Stylized Water 2 integration initialized successfully.");
    }
    
#if UNITY_EDITOR
    /// <summary>
    /// Editor utility function to quickly setup the water system.
    /// </summary>
    [ContextMenu("Auto Setup Water System")]
    private void EditorSetupWaterSystem()
    {
        // Create handler if needed
        if (waterHandler == null)
        {
            GameObject handlerObject = new GameObject("Water Handler");
            handlerObject.transform.SetParent(transform);
            waterHandler = handlerObject.AddComponent<StylizedWater2Handler>();
            UnityEditor.Undo.RegisterCreatedObjectUndo(handlerObject, "Create Water Handler");
        }
        
        // Find water object
        waterHandler.mainWaterObject = waterHandler.FindMainWaterObject();
        
        if (waterHandler.mainWaterObject == null)
        {
            Debug.LogWarning("No WaterObject found in the scene. Please add a Stylized Water 2 water object first.");
            return;
        }
        
        // Auto integrate boats
        waterHandler.IntegrateAllBoats();
        
        UnityEditor.EditorUtility.SetDirty(this);
        UnityEditor.EditorUtility.SetDirty(waterHandler);
        
        Debug.Log("Water system setup complete! Found water object: " + waterHandler.mainWaterObject.name);
    }
#endif
} 