using UnityEngine;

/// <summary>
/// This helper script ensures that WaterSimulation and BoatBuoyancy components are properly connected
/// after both have been fully initialized, resolving timing issues between their Start methods.
/// </summary>
public class ComponentInitializer : MonoBehaviour 
{
    [Tooltip("Reference to the boat buoyancy component")]
    public BoatBuoyancy boatBuoyancy;
    
    [Tooltip("Reference to the water simulation component")]
    public WaterSimulation waterSimulation;
    
    [Tooltip("Delay before attempting initialization (seconds)")]
    public float initializationDelay = 0.2f;
    
    [Tooltip("Whether to automatically find components if not assigned")]
    public bool autoFindComponents = true;
    
    [Tooltip("Maximum number of retry attempts")]
    public int maxRetryAttempts = 5;
    
    private bool initialized = false;
    private int retryCount = 0;
    
    private void Start()
    {
        // Try to find components if not assigned and auto-find is enabled
        if (autoFindComponents)
        {
            if (boatBuoyancy == null)
                boatBuoyancy = FindObjectOfType<BoatBuoyancy>();
                
            if (waterSimulation == null)
                waterSimulation = FindObjectOfType<WaterSimulation>();
        }
        
        // Log warning if components are missing
        if (boatBuoyancy == null)
            Debug.LogWarning("ComponentInitializer: No BoatBuoyancy component found!");
            
        if (waterSimulation == null)
            Debug.LogWarning("ComponentInitializer: No WaterSimulation component found!");
            
        // Start initialization process
        if (boatBuoyancy != null && waterSimulation != null)
        {
            // Invoke with a delay to ensure both components are fully initialized
            Invoke("InitializeComponents", initializationDelay);
        }
    }
    
    private void InitializeComponents()
    {
        if (initialized) return;
        
        if (retryCount >= maxRetryAttempts)
        {
            Debug.LogWarning("Maximum retry attempts reached. Initialization abandoned.");
            return;
        }
        
        Debug.Log("Initializing connection between BoatBuoyancy and WaterSimulation... (Attempt " + (retryCount + 1) + ")");
        
        try
        {
            // Manually connect the water simulation to the boat buoyancy
            boatBuoyancy.waterSurface = waterSimulation;
            
            // Test if the water simulation is initialized by trying to get a water height
            // This will throw an exception if the water simulation isn't ready
            float testHeight = waterSimulation.GetWaterHeightAt(Vector3.zero);
            
            // If we get here, the water simulation is initialized
            boatBuoyancy.useDynamicWaterSurface = true;
            
            // Try initializing the boat with the water simulation
            try {
                waterSimulation.UpdateBoatBuoyancy(boatBuoyancy);
                initialized = true;
                Debug.Log("Successfully connected BoatBuoyancy and WaterSimulation components.");
            }
            catch (System.Exception ex) {
                Debug.LogWarning("Failed to update boat buoyancy: " + ex.Message + " - Will retry shortly.");
                ScheduleRetry();
            }
        }
        catch (System.Exception e)
        {
            Debug.Log("Water simulation not yet ready: " + e.Message + " - Will retry shortly.");
            ScheduleRetry();
        }
    }
    
    private void ScheduleRetry()
    {
        retryCount++;
        if (retryCount < maxRetryAttempts)
        {
            // Increase delay for each retry attempt
            float delay = 0.2f + (retryCount * 0.1f);
            Invoke("InitializeComponents", delay);
        }
        else
        {
            Debug.LogWarning("Failed to initialize after " + maxRetryAttempts + " attempts.");
        }
    }
} 