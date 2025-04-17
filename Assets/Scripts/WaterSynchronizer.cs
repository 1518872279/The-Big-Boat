using UnityEngine;

/// <summary>
/// Coordinates synchronization between BoatBuoyancy, WaterSimulation, and the custom water shader.
/// Attach this to the same GameObject as your WaterSimulation component.
/// </summary>
public class WaterSynchronizer : MonoBehaviour
{
    [Header("Component References")]
    [Tooltip("The water simulation component (auto-assigned if on same object)")]
    public WaterSimulation waterSimulation;
    
    [Tooltip("The boat buoyancy component to synchronize with")]
    public BoatBuoyancy boatBuoyancy;
    
    [Tooltip("The gazing box controller component")]
    public GazingBoxController gazingBox;
    
    [Header("Water Shader Settings")]
    [Tooltip("The renderer with the water shader")]
    public Renderer waterRenderer;
    
    [Tooltip("Material property name for wave height")]
    public string waveHeightProperty = "_WaveHeight";
    
    [Tooltip("Material property name for wave speed")]
    public string waveSpeedProperty = "_WaveSpeed";
    
    [Tooltip("Material property name for direction")]
    public string waveDirectionProperty = "_Direction";
    
    [Tooltip("Material property name for wave steepness")]
    public string waveSteepnessProperty = "_WaveSteepness";
    
    [Header("Synchronization Settings")]
    [Tooltip("How often to update shader properties (seconds)")]
    public float updateInterval = 0.1f;
    
    [Tooltip("Multiplier for transferring simulation wave height to shader")]
    public float waveHeightMultiplier = 0.5f;
    
    [Tooltip("Multiplier for transferring simulation wave speed to shader")]
    public float waveSpeedMultiplier = 0.5f;
    
    [Tooltip("Maximum allowed deviation before updating material property")]
    public float updateThreshold = 0.01f;
    
    [Header("Smoothing Settings")]
    [Tooltip("Enable smoothing to reduce jittering")]
    public bool enableSmoothing = true;
    
    [Tooltip("How quickly shader values change (lower = smoother but less responsive)")]
    [Range(0.1f, 10f)]
    public float smoothingSpeed = 1.5f;
    
    [Tooltip("Apply extra smoothing to rotation-based effects")]
    public bool smoothRotationEffects = true;
    
    [Tooltip("How much to dampen small movements to avoid jitter")]
    [Range(0f, 0.2f)]
    public float jitterThreshold = 0.08f;
    
    [Header("Shader Effect Enhancement")]
    [Tooltip("Enable enhanced shader effects for more realistic water appearance")]
    public bool enhancedShaderEffects = true;
    
    [Tooltip("How strongly the flow dynamics affect the shader visuals")]
    [Range(0f, 1f)]
    public float flowVisualizationStrength = 0.5f;
    
    [Tooltip("Enable subtle wave refraction for more realistic appearance")]
    public bool enableWaveRefraction = true;
    
    [Tooltip("Create subtle random waves for more organic water movement")]
    public bool enableMicroWaves = true;
    
    [Range(0f, 1f)]
    public float microWaveStrength = 0.3f;
    
    // Cached values to avoid redundant material updates
    private float lastBoxRotationX = 0f;
    private float lastBoxRotationZ = 0f;
    private Vector3 lastWaveDirection = Vector3.zero;
    private float lastWaveHeight = 0f;
    private float lastWaveSpeed = 0f;
    private MaterialPropertyBlock propertyBlock;
    private float updateTimer = 0f;
    
    // Smoothed values
    private float smoothedWaveHeight;
    private float smoothedWaveSpeed;
    private Vector3 smoothedWaveDirection = Vector3.zero;
    private float smoothedSteepness = 0.1f;
    
    // Tracking for rotation smoothing
    private Vector3 boxRotationVelocity = Vector3.zero;
    private Vector3 previousBoxRotation = Vector3.zero;
    
    private Vector4 lastImpactData = Vector4.zero; // Data about recent impacts for visual effects
    private bool hasRecentImpact = false;
    private float impactVisualsTimer = 0f;
    
    private void Awake()
    {
        // Auto-assign water simulation if on same GameObject
        if (waterSimulation == null)
            waterSimulation = GetComponent<WaterSimulation>();
            
        // Create property block for efficient material updates
        propertyBlock = new MaterialPropertyBlock();
    }
    
    private void Start()
    {
        if (waterSimulation == null)
        {
            Debug.LogError("WaterSynchronizer: WaterSimulation component not assigned!");
            enabled = false;
            return;
        }
        
        if (waterRenderer == null)
        {
            // Try to find the renderer on the same object
            waterRenderer = GetComponent<Renderer>();
            
            if (waterRenderer == null)
            {
                Debug.LogError("WaterSynchronizer: Water renderer not assigned!");
                enabled = false;
                return;
            }
        }
        
        // Find boat buoyancy if not assigned
        if (boatBuoyancy == null)
            boatBuoyancy = FindObjectOfType<BoatBuoyancy>();
            
        // Find gazing box if not assigned
        if (gazingBox == null)
            gazingBox = FindObjectOfType<GazingBoxController>();
            
        // Get initial values
        if (waterRenderer != null && waterRenderer.sharedMaterial != null)
        {
            waterRenderer.GetPropertyBlock(propertyBlock);
            
            if (propertyBlock.HasFloat(waveHeightProperty))
                lastWaveHeight = propertyBlock.GetFloat(waveHeightProperty);
            else
                lastWaveHeight = waterRenderer.sharedMaterial.GetFloat(waveHeightProperty);
                
            if (propertyBlock.HasFloat(waveSpeedProperty))
                lastWaveSpeed = propertyBlock.GetFloat(waveSpeedProperty);
            else
                lastWaveSpeed = waterRenderer.sharedMaterial.GetFloat(waveSpeedProperty);
                
            if (propertyBlock.HasVector(waveDirectionProperty))
                lastWaveDirection = propertyBlock.GetVector(waveDirectionProperty);
            else
                lastWaveDirection = waterRenderer.sharedMaterial.GetVector(waveDirectionProperty);
        }
        
        // Initialize smoothed values
        smoothedWaveHeight = lastWaveHeight;
        smoothedWaveSpeed = lastWaveSpeed;
        smoothedWaveDirection = lastWaveDirection;
        
        if (gazingBox != null)
        {
            previousBoxRotation = gazingBox.transform.eulerAngles;
        }
        
        // Force an initial update
        ForceUpdate();
    }
    
    private void Update()
    {
        updateTimer += Time.deltaTime;
        
        // Update at the specified interval
        if (updateTimer >= updateInterval)
        {
            UpdateWaterShader();
            updateTimer = 0f;
        }
        
        // Update buoyancy synchronization every frame
        SyncBuoyancyWithWaterSimulation();
    }
    
    /// <summary>
    /// Forces an immediate update of all synchronized properties.
    /// </summary>
    public void ForceUpdate()
    {
        UpdateWaterShader(true);
        SyncBuoyancyWithWaterSimulation();
    }
    
    private void UpdateWaterShader(bool forceUpdate = false)
    {
        if (waterRenderer == null || waterSimulation == null)
            return;
            
        bool updateRequired = forceUpdate;
        
        // Get the property block
        waterRenderer.GetPropertyBlock(propertyBlock);
        
        // Calculate values based on water simulation
        float targetWaveHeight = waterSimulation.maxWaveHeight * waveHeightMultiplier;
        float targetWaveSpeed = waterSimulation.forceMultiplier * waveSpeedMultiplier;
        
        // Get box rotation influence on wave direction
        Vector3 targetWaveDirection = lastWaveDirection;
        float targetSteepness = smoothedSteepness;
        
        if (gazingBox != null)
        {
            // Use gazing box rotation to influence wave direction
            Quaternion boxRotation = gazingBox.transform.rotation;
            Vector3 eulerAngles = boxRotation.eulerAngles;
            
            // Calculate rotation change rate for jitter detection
            Vector3 rotationDelta = new Vector3(
                Mathf.DeltaAngle(previousBoxRotation.x, eulerAngles.x),
                Mathf.DeltaAngle(previousBoxRotation.y, eulerAngles.y),
                Mathf.DeltaAngle(previousBoxRotation.z, eulerAngles.z)
            );
            
            float deltaSpeed = rotationDelta.magnitude / Time.deltaTime;
            bool isJittering = deltaSpeed > 85f;
            
            // Only update rotation-based effects if not jittering or changes are significant
            if (!isJittering && (Mathf.Abs(eulerAngles.x - lastBoxRotationX) > updateThreshold ||
                Mathf.Abs(eulerAngles.z - lastBoxRotationZ) > updateThreshold))
            {
                // Normalize angles to -180 to 180 range
                float xAngle = eulerAngles.x > 180 ? eulerAngles.x - 360 : eulerAngles.x;
                float zAngle = eulerAngles.z > 180 ? eulerAngles.z - 360 : eulerAngles.z;
                
                // Convert angles to direction vector with reduced intensity
                Vector2 direction = new Vector2(-Mathf.Sin(zAngle * Mathf.Deg2Rad), 
                                               Mathf.Sin(xAngle * Mathf.Deg2Rad));
                
                // Apply jitter threshold - ignore tiny movements
                if (direction.magnitude > jitterThreshold)
                {
                    // Reduce intensity for gentler effect
                    direction = Vector2.ClampMagnitude(direction, 1.0f) * 0.6f;
                    
                    targetWaveDirection = new Vector4(direction.x, direction.y, 0, 0);
                    lastBoxRotationX = eulerAngles.x;
                    lastBoxRotationZ = eulerAngles.z;
                    updateRequired = true;
                }
                
                // Set wave steepness based on box tilt amount with reduced intensity
                float tiltAngle = Vector3.Angle(gazingBox.transform.up, Vector3.up);
                targetSteepness = Mathf.Lerp(0.1f, 0.35f, Mathf.Clamp01(tiltAngle / 65f) * 0.65f);
            }
            
            previousBoxRotation = eulerAngles;
        }
        
        // Apply smoothing to all values
        if (enableSmoothing)
        {
            float deltaTime = updateInterval;
            float smoothingFactor = smoothingSpeed * deltaTime;
            
            // Apply stronger smoothing to direction changes if detected as jittery
            float directionSmoothingFactor = smoothRotationEffects ? 
                smoothingFactor * 0.4f :
                smoothingFactor;
            
            // Smooth all values
            smoothedWaveHeight = Mathf.Lerp(smoothedWaveHeight, targetWaveHeight, smoothingFactor);
            smoothedWaveSpeed = Mathf.Lerp(smoothedWaveSpeed, targetWaveSpeed, smoothingFactor);
            smoothedWaveDirection = Vector3.Lerp(smoothedWaveDirection, targetWaveDirection, directionSmoothingFactor);
            smoothedSteepness = Mathf.Lerp(smoothedSteepness, targetSteepness, directionSmoothingFactor);
        }
        else
        {
            // Without smoothing, just use target values directly
            smoothedWaveHeight = targetWaveHeight;
            smoothedWaveSpeed = targetWaveSpeed;
            smoothedWaveDirection = targetWaveDirection;
            smoothedSteepness = targetSteepness;
        }
        
        // Check if values have changed enough to warrant an update
        if (forceUpdate || 
            Mathf.Abs(smoothedWaveHeight - lastWaveHeight) > updateThreshold ||
            Mathf.Abs(smoothedWaveSpeed - lastWaveSpeed) > updateThreshold ||
            Vector3.Distance(smoothedWaveDirection, lastWaveDirection) > updateThreshold)
        {
            lastWaveHeight = smoothedWaveHeight;
            lastWaveSpeed = smoothedWaveSpeed;
            lastWaveDirection = smoothedWaveDirection;
            updateRequired = true;
        }
        
        // Update shader properties if needed
        if (updateRequired)
        {
            // Update wave height and speed with smoothed values
            propertyBlock.SetFloat(waveHeightProperty, smoothedWaveHeight);
            propertyBlock.SetFloat(waveSpeedProperty, smoothedWaveSpeed);
            
            // Update wave direction with smoothed values
            propertyBlock.SetVector(waveDirectionProperty, smoothedWaveDirection);
            
            // Set wave steepness with smoothed value
            propertyBlock.SetFloat(waveSteepnessProperty, smoothedSteepness);
            
            // Apply enhanced visual effects if enabled
            if (enhancedShaderEffects)
            {
                // Add micro-wave detail for more natural water movement
                if (enableMicroWaves)
                {
                    float time = Time.time * 0.1f;
                    // Create subtle time-based variation in wave pattern
                    Vector4 microWaveParams = new Vector4(
                        Mathf.Sin(time * 0.5f) * 0.1f + 0.5f,
                        Mathf.Cos(time * 0.7f) * 0.1f + 0.5f,
                        Mathf.Sin(time * 1.1f) * microWaveStrength * 0.5f,
                        Mathf.Cos(time * 0.9f) * 0.5f + 0.5f
                    );
                    propertyBlock.SetVector("_MicroWaveParams", microWaveParams);
                }
                
                // Apply flow visualization if available in the water simulation
                if (waterSimulation && boatBuoyancy && flowVisualizationStrength > 0.01f)
                {
                    // Get boat position for proximity-based effects
                    Vector3 boatPos = boatBuoyancy.transform.position;
                    Vector3 localBoatPos = waterSimulation.transform.InverseTransformPoint(boatPos);
                    float boatDistance = new Vector2(localBoatPos.x, localBoatPos.z).magnitude;
                    
                    // Calculate normalized position on water surface
                    Vector2 normalizedBoatPos = new Vector2(
                        (localBoatPos.x / waterSimulation.meshSize) + 0.5f,
                        (localBoatPos.z / waterSimulation.meshSize) + 0.5f
                    );
                    
                    // Set boat influence parameters for shader
                    propertyBlock.SetVector("_BoatPosition", new Vector4(
                        normalizedBoatPos.x, 
                        normalizedBoatPos.y, 
                        Mathf.Min(1.0f, boatBuoyancy.GetComponent<Rigidbody>().velocity.magnitude * 0.2f),
                        flowVisualizationStrength
                    ));
                }
                
                // Apply refraction effect
                if (enableWaveRefraction)
                {
                    float refractionStrength = 0.02f * smoothedWaveHeight / waterSimulation.maxWaveHeight;
                    propertyBlock.SetFloat("_RefractionStrength", refractionStrength);
                }
                
                // Process recent impact visualization
                if (hasRecentImpact)
                {
                    impactVisualsTimer += Time.deltaTime;
                    if (impactVisualsTimer < 2.0f) // Impact effects last for 2 seconds
                    {
                        // Update impact visualization with fadeout
                        float impactProgress = 1.0f - (impactVisualsTimer / 2.0f);
                        Vector4 impactVisuals = new Vector4(
                            lastImpactData.x, 
                            lastImpactData.y, 
                            lastImpactData.z + Time.time, // Adding time for shader animation
                            lastImpactData.w * impactProgress // Fade out effect strength
                        );
                        propertyBlock.SetVector("_ImpactPosition", impactVisuals);
                    }
                    else
                    {
                        hasRecentImpact = false;
                    }
                }
            }
            
            // Apply all properties to the renderer
            waterRenderer.SetPropertyBlock(propertyBlock);
        }
    }
    
    private void SyncBuoyancyWithWaterSimulation()
    {
        if (boatBuoyancy == null || waterSimulation == null)
            return;
            
        // Make sure the boat has the water simulation reference
        if (boatBuoyancy.waterSurface != waterSimulation)
        {
            boatBuoyancy.waterSurface = waterSimulation;
            boatBuoyancy.useDynamicWaterSurface = true;
        }
        
        // Apply 2-way synchronization:
        
        // 1. Boat impacts affecting water
        if (boatBuoyancy.floatingPoints != null)
        {
            foreach (Transform point in boatBuoyancy.floatingPoints)
            {
                if (point == null) continue;
                
                // Get the velocity of this point
                Vector3 pointVelocity = boatBuoyancy.GetComponent<Rigidbody>().GetPointVelocity(point.position);
                float velocityMagnitude = pointVelocity.magnitude;
                
                // Only create waves for significant movement, with higher threshold
                if (velocityMagnitude > 1.5f)
                {
                    // Distance from point to water surface
                    float waterHeight = waterSimulation.GetWaterHeightAt(point.position);
                    float distanceToWater = Mathf.Abs(point.position.y - waterHeight);
                    
                    // If point is near water surface (entering or exiting)
                    if (distanceToWater < 0.3f)
                    {
                        // Apply force to water proportional to velocity
                        float force = -velocityMagnitude * 0.03f;
                        Vector3 localPos = waterSimulation.transform.InverseTransformPoint(point.position);
                        
                        // Record impact for enhanced visuals
                        if (enhancedShaderEffects && velocityMagnitude > 2.0f)
                        {
                            Vector2 normalizedPos = new Vector2(
                                (localPos.x / waterSimulation.meshSize) + 0.5f,
                                (localPos.z / waterSimulation.meshSize) + 0.5f
                            );
                            
                            lastImpactData = new Vector4(
                                normalizedPos.x, 
                                normalizedPos.y, 
                                0, // Time offset, will be set in update
                                velocityMagnitude * 0.1f // Impact strength
                            );
                            
                            hasRecentImpact = true;
                            impactVisualsTimer = 0f;
                        }
                        
                        waterSimulation.AddForceAtPosition(point.position, force, velocityMagnitude * 0.25f);
                    }
                }
            }
        }
        
        // 2. Water affecting boat buoyancy (already handled by WaterSimulation.UpdateBoatBuoyancy)
        waterSimulation.UpdateBoatBuoyancy(boatBuoyancy);
    }
} 