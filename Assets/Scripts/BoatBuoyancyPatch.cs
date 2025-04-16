using UnityEngine;

/// <summary>
/// This script patches the BoatBuoyancy component to prevent it from automatically
/// trying to register with the WaterSimulation during its Start method.
/// Apply this component to the same GameObject as BoatBuoyancy.
/// </summary>
public class BoatBuoyancyPatch : MonoBehaviour 
{
    private BoatBuoyancy boatBuoyancy;
    
    void Awake()
    {
        // Get the BoatBuoyancy component
        boatBuoyancy = GetComponent<BoatBuoyancy>();
        
        if (boatBuoyancy != null)
        {
            // Temporarily disable the dynamic water surface integration
            // This will prevent the error during initialization
            // The ComponentInitializer will re-enable this later
            boatBuoyancy.useDynamicWaterSurface = false;
            
            Debug.Log("BoatBuoyancy patched to prevent initialization errors");
        }
        else
        {
            Debug.LogWarning("BoatBuoyancyPatch: No BoatBuoyancy component found on this GameObject!");
        }
    }
} 