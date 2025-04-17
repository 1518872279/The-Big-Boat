using UnityEngine;
using StylizedWater2;
using System.Collections.Generic;

/// <summary>
/// Integrates the BoatBuoyancy system with StylizedWater2's water system.
/// Attach this component to the same GameObject as your BoatBuoyancy component.
/// </summary>
[RequireComponent(typeof(BoatBuoyancy))]
public class BoatBuoyancyIntegration : MonoBehaviour
{
    [Header("StylizedWater2 References")]
    [Tooltip("Reference to the StylizedWater2 WaterObject. If null, will attempt to find automatically.")]
    public WaterObject waterObject;
    
    [Tooltip("Enable to automatically find the closest WaterObject during gameplay")]
    public bool autoFindWaterObject = true;
    
    [Tooltip("Enable to update water material parameters in real-time (slight performance cost)")]
    public bool dynamicMaterial = false;
    
    [Header("Buoyancy Sampling")]
    [Tooltip("Use StylizedWater2's wave system instead of the BoatBuoyancy wave system")]
    public bool useStylizedWater2Waves = true;
    
    [Tooltip("Create sampling points based on the floating points from BoatBuoyancy")]
    public bool syncSamplingPoints = true;
    
    [Tooltip("Multiplier to apply to StylizedWater2's wave height when calculating buoyancy")]
    [Range(0.1f, 2f)]
    public float waveHeightMultiplier = 1f;
    
    [Tooltip("How strongly the boat should rotate with the wave curvature")]
    [Range(0f, 1f)]
    public float rollAmount = 0.5f;
    
    [Tooltip("Height offset for the floating transform")]
    public float heightOffset = 0.0f;
    
    [Header("Visual Effects")]
    [Tooltip("Enable foam effects on the water surface around the boat")]
    public bool enableFoamEffects = true;
    
    [Tooltip("Enable dynamic wave simulation when the boat impacts water")]
    public bool enableImpactWaves = true;
    
    // Private references
    private BoatBuoyancy boatBuoyancy;
    private BoatBuoyancyEffects boatEffects;
    private Rigidbody rb;
    private Vector3[] samplePositions;
    private List<Vector3> stylizedWaterSamples = new List<Vector3>();
    private FloatingTransform floatingTransform;
    
    private void Awake()
    {
        // Get required components
        boatBuoyancy = GetComponent<BoatBuoyancy>();
        boatEffects = GetComponent<BoatBuoyancyEffects>();
        rb = GetComponent<Rigidbody>();
        
        // Create or get FloatingTransform component for visualization in editor
        floatingTransform = GetComponent<FloatingTransform>();
        if (floatingTransform == null && syncSamplingPoints)
        {
            floatingTransform = gameObject.AddComponent<FloatingTransform>();
            floatingTransform.rollAmount = rollAmount;
        }
        
        // Find water object if needed
        if (waterObject == null && autoFindWaterObject)
        {
            FindNearestWaterObject();
        }
    }
    
    private void Start()
    {
        // Initialize sample positions based on boat buoyancy floating points
        if (syncSamplingPoints && boatBuoyancy.floatingPoints != null && boatBuoyancy.floatingPoints.Length > 0)
        {
            SyncSamplingPoints();
        }
        
        // Configure the FloatingTransform component if it exists
        if (floatingTransform != null)
        {
            floatingTransform.waterObject = waterObject;
            floatingTransform.dynamicMaterial = dynamicMaterial;
            floatingTransform.waterLevelSource = FloatingTransform.WaterLevelSource.WaterObject;
            floatingTransform.rollAmount = rollAmount;
            floatingTransform.heightOffset = heightOffset;
        }
    }
    
    private void SyncSamplingPoints()
    {
        // Clear existing samples
        stylizedWaterSamples.Clear();
        
        if (floatingTransform != null)
        {
            floatingTransform.samples.Clear();
        }
        
        // Convert BoatBuoyancy floating points to local space for StylizedWater2
        foreach (Transform point in boatBuoyancy.floatingPoints)
        {
            if (point != null)
            {
                Vector3 localPos = transform.InverseTransformPoint(point.position);
                stylizedWaterSamples.Add(localPos);
                
                // Also sync with FloatingTransform for visualization
                if (floatingTransform != null)
                {
                    floatingTransform.samples.Add(localPos);
                }
            }
        }
    }
    
    private void FixedUpdate()
    {
        if (!useStylizedWater2Waves || waterObject == null) return;
        
        // Update water object reference if using auto-find
        if (autoFindWaterObject)
        {
            FindNearestWaterObject();
        }
        
        if (waterObject == null || waterObject.material == null) return;
        
        // Sample wave heights and update buoyancy
        ApplyStylizedWaterBuoyancy();
    }
    
    private void FindNearestWaterObject()
    {
        waterObject = WaterObject.Find(transform.position, false);
        
        // Update FloatingTransform reference if it exists
        if (floatingTransform != null && waterObject != null)
        {
            floatingTransform.waterObject = waterObject;
        }
    }
    
    private void ApplyStylizedWaterBuoyancy()
    {
        if (boatBuoyancy.floatingPoints == null || boatBuoyancy.floatingPoints.Length == 0) return;
        
        float totalForce = 0.0f;
        int pointsUnderWater = 0;
        Vector3 centerOfBuoyancy = Vector3.zero;
        
        // Process each floating point
        for (int i = 0; i < boatBuoyancy.floatingPoints.Length; i++)
        {
            Transform floatPoint = boatBuoyancy.floatingPoints[i];
            if (floatPoint == null) continue;
            
            // Sample wave height and normal using StylizedWater2's system
            Vector3 normal = Vector3.up;
            float waterHeight = Buoyancy.SampleWaves(
                floatPoint.position, 
                waterObject.material, 
                waterObject.transform.position.y, 
                rollAmount, 
                dynamicMaterial, 
                out normal
            );
            
            // Apply height multiplier
            waterHeight *= waveHeightMultiplier;
            
            // Calculate submersion depth
            float submersion = floatPoint.position.y - waterHeight;
            
            // If point is underwater
            if (submersion < 0)
            {
                pointsUnderWater++;
                
                // Calculate and apply buoyancy force
                Vector3 force = normal * boatBuoyancy.buoyancyForce * Mathf.Abs(submersion);
                rb.AddForceAtPosition(force, floatPoint.position);
                
                // Track buoyancy center and total force
                centerOfBuoyancy += floatPoint.position;
                totalForce += force.magnitude;
                
                // Create water effects if enabled
                if (enableImpactWaves)
                {
                    float impactVelocity = Vector3.Dot(rb.GetPointVelocity(floatPoint.position), Vector3.down);
                    if (impactVelocity > 1.0f)
                    {
                        // Create splash effect
                        TriggerWaterEffect(floatPoint.position, -impactVelocity * 0.05f, 0.3f);
                    }
                }
            }
        }
        
        // Apply water drag and dampening
        if (pointsUnderWater > 0)
        {
            // Calculate average buoyancy center
            centerOfBuoyancy /= pointsUnderWater;
            
            // Apply drag force
            rb.AddForceAtPosition(-rb.velocity * boatBuoyancy.waterDrag * totalForce * Time.fixedDeltaTime, centerOfBuoyancy);
            
            // Apply angular drag
            rb.angularVelocity *= (1f - boatBuoyancy.waterAngularDrag * Time.fixedDeltaTime);
        }
    }
    
    private void LateUpdate()
    {
        if (!useStylizedWater2Waves || waterObject == null || !enableFoamEffects) return;
        
        // Create wake effects
        if (rb.velocity.magnitude > 0.5f)
        {
            Vector3 boatDirection = transform.forward;
            Vector3 wakePosition = transform.position - boatDirection * 0.5f;
            
            float wakeForce = -rb.velocity.magnitude * 0.05f;
            float wakeRadius = 0.5f + rb.velocity.magnitude * 0.1f;
            
            TriggerWaterEffect(wakePosition, wakeForce, wakeRadius);
        }
    }
    
    private void TriggerWaterEffect(Vector3 position, float force, float radius)
    {
        // Add foam to the water surface using StylizedWater2's API
        if (waterObject != null && waterObject.material != null)
        {
            // Direct route for adding displacement
            StylizedWater2.Buoyancy.SampleWaves(position, waterObject, rollAmount, dynamicMaterial, out Vector3 normal);
            
            // If BoatBuoyancyEffects exists, sync with it for particle effects
            if (boatEffects != null && boatEffects.waterSimulation != null)
            {
                boatEffects.waterSimulation.AddForceAtPosition(position, force, radius);
            }
        }
    }
    
    private void OnDrawGizmosSelected()
    {
        if (!boatBuoyancy || boatBuoyancy.floatingPoints == null) return;
        
        // Draw water level from StylizedWater2
        if (waterObject != null)
        {
            Gizmos.color = new Color(0, 0.8f, 1f, 0.3f);
            Vector3 center = transform.position;
            center.y = waterObject.transform.position.y;
            Gizmos.DrawCube(center, new Vector3(3f, 0.05f, 3f));
            
            // Draw sampling points
            Gizmos.color = new Color(0, 0.8f, 1f, 0.8f);
            foreach (Transform point in boatBuoyancy.floatingPoints)
            {
                if (point == null) continue;
                
                Gizmos.DrawSphere(point.position, 0.1f);
                
                // Draw water height at point
                if (Application.isPlaying && waterObject.material != null)
                {
                    Vector3 normal = Vector3.up;
                    float waterHeight = Buoyancy.SampleWaves(
                        point.position, 
                        waterObject.material, 
                        waterObject.transform.position.y, 
                        rollAmount, 
                        dynamicMaterial, 
                        out normal
                    );
                    
                    Vector3 waterPos = point.position;
                    waterPos.y = waterHeight;
                    
                    Gizmos.color = Color.blue;
                    Gizmos.DrawLine(point.position, waterPos);
                    Gizmos.DrawSphere(waterPos, 0.05f);
                }
            }
        }
    }
} 