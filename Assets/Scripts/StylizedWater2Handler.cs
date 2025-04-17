using UnityEngine;
using StylizedWater2;
using System.Collections.Generic;

/// <summary>
/// Utility class to manage and initialize StylizedWater2 integration components in the scene.
/// This class helps with finding water objects and setting up integration components.
/// </summary>
public class StylizedWater2Handler : MonoBehaviour
{
    [Header("Water References")]
    [Tooltip("Reference to the main StylizedWater2 object in the scene")]
    public WaterObject mainWaterObject;
    
    [Tooltip("Automatically find all BoatBuoyancy objects and integrate them")]
    public bool autoIntegrateBoats = true;
    
    [Header("Integration Settings")]
    [Tooltip("Default settings for wave height multiplier")]
    [Range(0.1f, 2f)]
    public float defaultWaveHeightMultiplier = 1f;
    
    [Tooltip("Default settings for roll amount")]
    [Range(0f, 1f)]
    public float defaultRollAmount = 0.5f;
    
    [Tooltip("Enable foam effects by default")]
    public bool enableFoamEffects = true;
    
    [Tooltip("Enable impact waves by default")]
    public bool enableImpactWaves = true;
    
    // Static reference for easy access
    public static StylizedWater2Handler Instance { get; private set; }
    
    // Track integrated boats
    private List<BoatBuoyancyIntegration> integratedBoats = new List<BoatBuoyancyIntegration>();
    
    private void Awake()
    {
        // Set up singleton pattern
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
        
        // Find main water object if not set
        if (mainWaterObject == null)
        {
            mainWaterObject = FindMainWaterObject();
        }
        
        // Auto integrate boats if enabled
        if (autoIntegrateBoats)
        {
            IntegrateAllBoats();
        }
    }
    
    /// <summary>
    /// Finds the main water object in the scene.
    /// </summary>
    /// <returns>The found WaterObject or null if none exists</returns>
    public WaterObject FindMainWaterObject()
    {
        // Try to find any water object in the scene
        WaterObject[] waterObjects = FindObjectsOfType<WaterObject>();
        
        if (waterObjects.Length > 0)
        {
            // Return the first one found - typically there's only one main water surface
            return waterObjects[0];
        }
        
        Debug.LogWarning("No StylizedWater2 WaterObject found in the scene.");
        return null;
    }
    
    /// <summary>
    /// Finds all BoatBuoyancy components in the scene and integrates them with StylizedWater2.
    /// </summary>
    public void IntegrateAllBoats()
    {
        // Clear the list of integrated boats
        integratedBoats.Clear();
        
        // Find all BoatBuoyancy components
        BoatBuoyancy[] boatBuoyancies = FindObjectsOfType<BoatBuoyancy>();
        
        foreach (BoatBuoyancy boat in boatBuoyancies)
        {
            IntegrateBoat(boat.gameObject);
        }
        
        Debug.Log($"Integrated {integratedBoats.Count} boats with StylizedWater2.");
    }
    
    /// <summary>
    /// Integrates a specific boat GameObject with StylizedWater2.
    /// </summary>
    /// <param name="boatObject">The boat GameObject to integrate</param>
    /// <returns>The integration component or null if integration failed</returns>
    public BoatBuoyancyIntegration IntegrateBoat(GameObject boatObject)
    {
        if (boatObject == null) return null;
        
        // Check if the boat has a BoatBuoyancy component
        BoatBuoyancy boatBuoyancy = boatObject.GetComponent<BoatBuoyancy>();
        if (boatBuoyancy == null)
        {
            Debug.LogWarning($"GameObject {boatObject.name} does not have a BoatBuoyancy component.");
            return null;
        }
        
        // Check if integration already exists
        BoatBuoyancyIntegration integration = boatObject.GetComponent<BoatBuoyancyIntegration>();
        if (integration == null)
        {
            // Create new integration component
            integration = boatObject.AddComponent<BoatBuoyancyIntegration>();
        }
        
        // Configure integration component
        integration.waterObject = mainWaterObject;
        integration.waveHeightMultiplier = defaultWaveHeightMultiplier;
        integration.rollAmount = defaultRollAmount;
        integration.enableFoamEffects = enableFoamEffects;
        integration.enableImpactWaves = enableImpactWaves;
        
        // Add to tracking list
        integratedBoats.Add(integration);
        
        return integration;
    }
    
    /// <summary>
    /// Applies settings to all integrated boats.
    /// </summary>
    public void ApplySettingsToAllBoats()
    {
        foreach (BoatBuoyancyIntegration integration in integratedBoats)
        {
            if (integration != null)
            {
                integration.waterObject = mainWaterObject;
                integration.waveHeightMultiplier = defaultWaveHeightMultiplier;
                integration.rollAmount = defaultRollAmount;
                integration.enableFoamEffects = enableFoamEffects;
                integration.enableImpactWaves = enableImpactWaves;
            }
        }
    }
    
    /// <summary>
    /// Gets the main water height at a specific world position.
    /// </summary>
    /// <param name="worldPosition">The position to check</param>
    /// <returns>The water height at the specified position</returns>
    public float GetWaterHeightAtPosition(Vector3 worldPosition)
    {
        if (mainWaterObject == null || mainWaterObject.material == null) 
            return 0f;
        
        // Sample water height using StylizedWater2 API
        Vector3 normal = Vector3.up;
        return Buoyancy.SampleWaves(
            worldPosition,
            mainWaterObject.material,
            mainWaterObject.transform.position.y,
            0f, // No roll sampling needed for just height
            false, // No dynamic material updates needed for simple sampling
            out normal
        );
    }
} 