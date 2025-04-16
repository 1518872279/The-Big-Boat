using UnityEngine;

/// <summary>
/// Extends WaterSimulation with additional functionality for integrating with the Stylized Water 2 shader.
/// This component should be attached to the same GameObject as the WaterSimulation component.
/// </summary>
public class WaterSimulationExtension : MonoBehaviour 
{
    [Header("Stylized Water 2 Integration")]
    [Tooltip("The renderer using the Stylized Water 2 shader")]
    public Renderer waterRenderer;
    
    [Tooltip("Automatically find renderer if not assigned")]
    public bool autoFindRenderer = true;
    
    [Header("Water Property Mappings")]
    [Tooltip("Map WaterSimulation.maxWaveHeight to this shader property")]
    public string waveHeightProperty = "_WaveHeight";
    
    [Tooltip("Map box tilt to wave direction in this shader property")]
    public string directionProperty = "_Direction";
    
    [Tooltip("Map box tilt amount to wave steepness in this shader property")]
    public string steepnessProperty = "_WaveSteepness";
    
    [Tooltip("Map simulation speed to shader wave speed in this property")]
    public string speedProperty = "_Speed";
    
    [Header("Dynamic Effects")]
    [Tooltip("Enable foam generation based on simulation")]
    public bool enableDynamicFoam = true;
    
    [Tooltip("Shader property for foam amount")]
    public string foamAmountProperty = "_FoamBaseAmount";
    
    [Tooltip("Shader property for foam tiling")]
    public string foamTilingProperty = "_FoamTiling";
    
    [Tooltip("Foam amount multiplier")]
    [Range(0.0f, 10.0f)]
    public float foamMultiplier = 0.7f;
    
    [Header("Smoothing Settings")]
    [Tooltip("Enable value smoothing to reduce jitter")]
    public bool enableSmoothing = true;
    
    [Tooltip("Smoothing speed for property changes")]
    [Range(0.1f, 5.0f)]
    public float smoothingSpeed = 1.5f;
    
    [Tooltip("Minimum change required before updating (reduces jitter)")]
    [Range(0.001f, 0.1f)]
    public float changeThreshold = 0.01f;
    
    // References
    private WaterSimulation waterSim;
    private GazingBoxController gazingBox;
    private MaterialPropertyBlock propertyBlock;
    
    // Cached values for optimization
    private Vector3 lastBoxRotation;
    private float lastMaxWaveHeight;
    private float lastWaveActivity;
    
    // Smoothed values
    private float smoothedWaveHeight;
    private float smoothedWaveActivity;
    private Vector4 smoothedWaveDirection = Vector4.zero;
    private float smoothedSteepness = 0.1f;
    
    // Previous frame data for jitter detection
    private Vector3 previousBoxRotation;
    private float boxRotationChangeRate;
    
    private void Awake()
    {
        // Get references
        waterSim = GetComponent<WaterSimulation>();
        
        if (waterSim == null)
        {
            Debug.LogError("WaterSimulationExtension requires a WaterSimulation component on the same GameObject!");
            enabled = false;
            return;
        }
        
        // Find gazing box
        gazingBox = FindObjectOfType<GazingBoxController>();
        
        // Find renderer if needed
        if (waterRenderer == null && autoFindRenderer)
        {
            waterRenderer = GetComponent<Renderer>();
            
            // If not on this object, check children
            if (waterRenderer == null && transform.childCount > 0)
            {
                for (int i = 0; i < transform.childCount; i++)
                {
                    Renderer r = transform.GetChild(i).GetComponent<Renderer>();
                    if (r != null && r.sharedMaterial != null && 
                        r.sharedMaterial.shader.name.Contains("Water"))
                    {
                        waterRenderer = r;
                        break;
                    }
                }
            }
        }
        
        // Initialize property block for efficient updates
        propertyBlock = new MaterialPropertyBlock();
    }
    
    private void Start()
    {
        if (waterRenderer == null)
        {
            Debug.LogWarning("WaterSimulationExtension: No water renderer assigned or found!");
        }
        else
        {
            // Get initial shader values
            waterRenderer.GetPropertyBlock(propertyBlock);
            lastMaxWaveHeight = waterSim.maxWaveHeight;
            smoothedWaveHeight = lastMaxWaveHeight;
            
            if (gazingBox != null)
            {
                lastBoxRotation = gazingBox.transform.eulerAngles;
                previousBoxRotation = lastBoxRotation;
            }
            
            // Initialize smoothed values
            smoothedWaveActivity = 0f;
            
            // Apply initial settings
            UpdateShaderProperties();
        }
    }
    
    private void LateUpdate()
    {
        if (waterRenderer == null || waterSim == null)
            return;
            
        bool needsUpdate = false;
        
        // Calculate rotation change rate for jitter detection
        if (gazingBox != null)
        {
            Vector3 currentRotation = gazingBox.transform.eulerAngles;
            Vector3 rotationDelta = new Vector3(
                Mathf.DeltaAngle(previousBoxRotation.x, currentRotation.x),
                Mathf.DeltaAngle(previousBoxRotation.y, currentRotation.y),
                Mathf.DeltaAngle(previousBoxRotation.z, currentRotation.z)
            );
            
            boxRotationChangeRate = rotationDelta.magnitude / Time.deltaTime;
            previousBoxRotation = currentRotation;
        }
        
        // Check if wave height has changed significantly
        if (Mathf.Abs(waterSim.maxWaveHeight - lastMaxWaveHeight) > changeThreshold)
        {
            lastMaxWaveHeight = waterSim.maxWaveHeight;
            needsUpdate = true;
        }
        
        // Check if gazing box rotation has changed significantly, but filter out jittery movements
        bool isJittering = boxRotationChangeRate > 80f;
        if (gazingBox != null && !isJittering)
        {
            Vector3 currentRotation = gazingBox.transform.eulerAngles;
            if (Vector3.Distance(currentRotation, lastBoxRotation) > 1.5f)
            {
                lastBoxRotation = currentRotation;
                needsUpdate = true;
            }
        }
        
        // Calculate wave activity (how active the water surface is)
        float waveActivity = CalculateWaveActivity();
        if (Mathf.Abs(waveActivity - lastWaveActivity) > changeThreshold)
        {
            lastWaveActivity = waveActivity;
            needsUpdate = true;
        }
        
        // Update shader if needed
        if (needsUpdate)
        {
            UpdateShaderProperties();
        }
    }
    
    /// <summary>
    /// Calculates how active/turbulent the water surface is based on simulation data
    /// </summary>
    private float CalculateWaveActivity()
    {
        if (waterSim == null) return 0f;
        
        // Calculate a normalized value representing how active the water is
        float activity = 0f;
        
        // Box tilt contribution (if available)
        if (gazingBox != null)
        {
            float tiltAngle = Vector3.Angle(gazingBox.transform.up, Vector3.up);
            
            // Apply a stronger non-linear curve with higher threshold to make small tilts even less impactful
            // but preserve the effect of larger tilts
            float normalizedTilt = tiltAngle / 70f; // Increased from 60f for reduced sensitivity
            activity += Mathf.Pow(normalizedTilt, 1.7f) * 0.7f; // Increased power to 1.7f, reduced multiplier to 0.7f
        }
        
        // Clamp and return
        return Mathf.Clamp01(activity);
    }
    
    /// <summary>
    /// Updates the water shader properties based on the simulation state
    /// </summary>
    private void UpdateShaderProperties()
    {
        if (waterRenderer == null) return;
        
        // Get current property block
        waterRenderer.GetPropertyBlock(propertyBlock);
        
        // Prepare target values
        float targetWaveHeight = waterSim.maxWaveHeight;
        Vector4 targetWaveDirection = Vector4.zero;
        float targetSteepness = 0.1f; // Default low steepness
        float targetWaveActivity = lastWaveActivity;
        
        // Update wave direction based on gazing box rotation
        if (gazingBox != null)
        {
            // Calculate wave direction based on box tilt
            Quaternion boxRotation = gazingBox.transform.rotation;
            Vector3 tiltDirection = Vector3.ProjectOnPlane(boxRotation * Vector3.down, Vector3.up).normalized;
            
            // Only apply if there's a significant tilt and not jittering
            bool isJittering = boxRotationChangeRate > 65f;
            if (tiltDirection.magnitude > 0.08f && !isJittering)
            {
                // Reduce intensity for subtler effect
                tiltDirection = Vector3.ClampMagnitude(tiltDirection, 1.0f) * 0.6f;
                targetWaveDirection = new Vector4(tiltDirection.x, tiltDirection.z, 0, 0);
            }
            
            // Get tilt angle
            float tiltAngle = Vector3.Angle(gazingBox.transform.up, Vector3.up);
            
            // Apply gentler steepness curve with higher threshold
            targetSteepness = Mathf.Lerp(0.1f, 0.35f, Mathf.Pow(Mathf.Clamp01(tiltAngle / 65f), 1.3f) * 0.6f);
        }
        
        // Apply smoothing if enabled
        if (enableSmoothing)
        {
            float smoothingFactor = smoothingSpeed * Time.deltaTime;
            
            // Apply stronger smoothing for rotation-based values to reduce jitter
            float rotationSmoothingFactor = smoothingFactor * 0.4f;
            
            // Smooth all values
            smoothedWaveHeight = Mathf.Lerp(smoothedWaveHeight, targetWaveHeight, smoothingFactor);
            smoothedWaveActivity = Mathf.Lerp(smoothedWaveActivity, targetWaveActivity, smoothingFactor);
            smoothedWaveDirection = Vector4.Lerp(smoothedWaveDirection, targetWaveDirection, rotationSmoothingFactor);
            smoothedSteepness = Mathf.Lerp(smoothedSteepness, targetSteepness, rotationSmoothingFactor);
        }
        else
        {
            // Just use target values directly
            smoothedWaveHeight = targetWaveHeight;
            smoothedWaveActivity = targetWaveActivity;
            smoothedWaveDirection = targetWaveDirection;
            smoothedSteepness = targetSteepness;
        }
        
        // Update wave height with reduced intensity
        if (!string.IsNullOrEmpty(waveHeightProperty))
        {
            propertyBlock.SetFloat(waveHeightProperty, smoothedWaveHeight);
        }
        
        // Update wave direction with smoothed values
        if (!string.IsNullOrEmpty(directionProperty) && smoothedWaveDirection.magnitude > 0.05f)
        {
            propertyBlock.SetVector(directionProperty, smoothedWaveDirection);
        }
        
        // Update wave steepness with smoothed value
        if (!string.IsNullOrEmpty(steepnessProperty))
        {
            propertyBlock.SetFloat(steepnessProperty, smoothedSteepness);
        }
        
        // Update speed parameter with reduced intensity
        if (!string.IsNullOrEmpty(speedProperty))
        {
            float speedValue = waterSim.forceMultiplier * 0.7f; // Reduced by 30%
            propertyBlock.SetFloat(speedProperty, speedValue);
        }
        
        // Update foam based on activity level with smoother transitions
        if (enableDynamicFoam)
        {
            if (!string.IsNullOrEmpty(foamAmountProperty))
            {
                float foamAmount = smoothedWaveActivity * foamMultiplier;
                propertyBlock.SetFloat(foamAmountProperty, Mathf.Clamp01(foamAmount));
            }
            
            if (!string.IsNullOrEmpty(foamTilingProperty))
            {
                // Adjust foam tiling based on activity (more active = finer foam detail)
                // Use a gentler curve
                float baseTiling = 0.2f;
                float activityTiling = baseTiling * (1.0f + smoothedWaveActivity * 0.7f);
                propertyBlock.SetFloat(foamTilingProperty, activityTiling);
            }
        }
        
        // Apply updated properties
        waterRenderer.SetPropertyBlock(propertyBlock);
    }
    
    /// <summary>
    /// API for causing a splash at a specific position with reduced intensity
    /// </summary>
    public void CreateSplash(Vector3 position, float force, float radius)
    {
        if (waterSim != null)
        {
            // Reduce force intensity by 30% for more subtle effect
            waterSim.AddForceAtPosition(position, force * 0.7f, radius);
        }
    }
} 