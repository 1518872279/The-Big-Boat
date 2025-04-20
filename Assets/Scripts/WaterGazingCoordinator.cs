using UnityEngine;

/// <summary>
/// Coordinates between GazingBoxController and WaterSimulation to create the effect of
/// water responding to box tilting without physically rotating the water plane.
/// Attach this to the same GameObject as the WaterSimulation component.
/// </summary>
public class WaterGazingCoordinator : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Reference to the gazing box controller")]
    public GazingBoxController gazingBox;
    
    [Tooltip("Reference to water simulation (auto-assigned if on same object)")]
    public WaterSimulation waterSimulation;
    
    [Header("Force Settings")]
    [Tooltip("How strongly the gazing box influences the water")]
    [Range(0.1f, 10f)]
    public float forceMultiplier = 2f;
    
    [Tooltip("How quickly changes in tilt affect the water")]
    [Range(0.1f, 10f)]
    public float responseSpeed = 5f;
    
    [Tooltip("Minimum tilt angle required before applying forces")]
    [Range(0.1f, 5f)]
    public float minTiltThreshold = 1.5f;
    
    [Header("Wave Settings")]
    [Tooltip("Create pushing force in direction of tilt")]
    public bool createDirectionalWaves = true;
    
    [Tooltip("Apply non-uniform force based on box rotation")]
    public bool useRotationalMomentum = true;
    
    [Tooltip("Create wave patterns radiating from center when box moves")]
    public bool createImpactWaves = true;
    
    [Header("Advanced Settings")]
    [Tooltip("Gradually adjust water simulation parameters")]
    public bool dynamicallyAdjustParameters = true;
    
    // Cached values for calculations
    private Vector3 previousBoxPosition;
    private Quaternion previousBoxRotation;
    private Vector3 boxVelocity;
    private Vector3 boxAngularVelocity;
    private float timeSinceLastImpact = 0f;
    private const float MIN_IMPACT_INTERVAL = 0.2f; // Minimum time between impacts
    
    // Smoothed influence values
    private Vector3 smoothedForceDirection = Vector3.zero;
    private float smoothedForceMagnitude = 0f;
    
    // Dynamic parameters
    private float baseWaveHeight;
    private float baseSpringConstant;
    private float baseDamping;
    
    void Start()
    {
        // Auto-assign water simulation if not set
        if (waterSimulation == null)
        {
            waterSimulation = GetComponent<WaterSimulation>();
            if (waterSimulation == null)
            {
                Debug.LogError("WaterGazingCoordinator: No WaterSimulation component found!");
                enabled = false;
                return;
            }
        }
        
        // Auto-find gazing box if not assigned
        if (gazingBox == null)
        {
            gazingBox = FindObjectOfType<GazingBoxController>();
            if (gazingBox == null)
            {
                Debug.LogWarning("WaterGazingCoordinator: No GazingBoxController found in the scene!");
            }
        }
        
        // Initialize cached values
        if (gazingBox != null)
        {
            previousBoxPosition = gazingBox.transform.position;
            previousBoxRotation = gazingBox.transform.rotation;
        }
        
        // Store base simulation parameters
        if (waterSimulation != null)
        {
            baseWaveHeight = waterSimulation.maxWaveHeight;
            baseSpringConstant = waterSimulation.springConstant;
            baseDamping = waterSimulation.damping;
        }
    }
    
    void Update()
    {
        if (gazingBox == null || waterSimulation == null) return;
        
        // Track time between impacts
        timeSinceLastImpact += Time.deltaTime;
        
        // Calculate box movement
        Vector3 currentPosition = gazingBox.transform.position;
        Quaternion currentRotation = gazingBox.transform.rotation;
        
        // Calculate box velocity
        boxVelocity = (currentPosition - previousBoxPosition) / Time.deltaTime;
        
        // Calculate angular velocity (simplified)
        Quaternion deltaRotation = currentRotation * Quaternion.Inverse(previousBoxRotation);
        float angle;
        Vector3 axis;
        deltaRotation.ToAngleAxis(out angle, out axis);
        // Convert to radians per second
        if (angle > 180f) angle -= 360f;
        boxAngularVelocity = axis * (angle * Mathf.Deg2Rad / Time.deltaTime);
        
        // Apply influence based on box orientation and movement
        ApplyWaterInfluence();
        
        // Update cached values for next frame
        previousBoxPosition = currentPosition;
        previousBoxRotation = currentRotation;
    }
    
    private void ApplyWaterInfluence()
    {
        // Calculate tilt angle
        float tiltAngle = Vector3.Angle(gazingBox.transform.up, Vector3.up);
        
        // Only apply forces if tilt is significant
        if (tiltAngle > minTiltThreshold)
        {
            // Calculate tilt direction projected onto horizontal plane
            Vector3 tiltDirection = Vector3.ProjectOnPlane(gazingBox.transform.up, Vector3.up);
            if (tiltDirection.magnitude > 0.01f)
            {
                tiltDirection = -tiltDirection.normalized;
                
                // Calculate force based on tilt angle (use sine for more natural response)
                float forceMagnitude = Mathf.Sin(tiltAngle * Mathf.Deg2Rad) * forceMultiplier;
                
                // Apply smoothing
                smoothedForceDirection = Vector3.Lerp(smoothedForceDirection, tiltDirection, Time.deltaTime * responseSpeed);
                smoothedForceMagnitude = Mathf.Lerp(smoothedForceMagnitude, forceMagnitude, Time.deltaTime * responseSpeed);
                
                if (createDirectionalWaves)
                {
                    // Create a unified wave front in the direction of tilt
                    CreateDirectionalWaves(smoothedForceDirection, smoothedForceMagnitude);
                }
            }
        }
        else
        {
            // Decay force when below threshold
            smoothedForceMagnitude = Mathf.Lerp(smoothedForceMagnitude, 0f, Time.deltaTime * responseSpeed);
        }
        
        // Apply impact waves from sudden movements
        if (createImpactWaves && boxVelocity.magnitude > 0.5f && timeSinceLastImpact > MIN_IMPACT_INTERVAL)
        {
            CreateImpactWave(boxVelocity.magnitude);
            timeSinceLastImpact = 0f;
        }
        
        // Dynamically adjust water parameters based on activity
        if (dynamicallyAdjustParameters)
        {
            AdjustWaterParameters(tiltAngle);
        }
    }
    
    private void CreateDirectionalWaves(Vector3 direction, float magnitude)
    {
        // Calculate the wave center point (slightly ahead in the tilt direction)
        Vector3 waveCenter = transform.position + direction * 2f;
        
        // Apply force to create directional waves
        // This uses the water simulation's AddForceAtPosition method from a distance
        // to create a wave front traveling in the direction of the tilt
        float radius = waterSimulation.meshSize * 0.3f;
        waterSimulation.AddForceAtPosition(waveCenter, magnitude * 5f, radius);
        
        // Create secondary waves perpendicular to main direction for more natural effect
        if (magnitude > 0.3f)
        {
            Vector3 perpendicularDir = Vector3.Cross(direction, Vector3.up).normalized;
            waterSimulation.AddForceAtPosition(
                transform.position + perpendicularDir * 2f, 
                magnitude * 1.5f, 
                radius * 0.7f
            );
            waterSimulation.AddForceAtPosition(
                transform.position - perpendicularDir * 2f, 
                magnitude * 1.5f, 
                radius * 0.7f
            );
        }
    }
    
    private void CreateImpactWave(float impactForce)
    {
        // Create a radial wave emanating from the center
        waterSimulation.AddForceAtPosition(
            transform.position, 
            impactForce * 2f, 
            waterSimulation.meshSize * 0.5f
        );
    }
    
    private void AdjustWaterParameters(float tiltAngle)
    {
        // Normalize tilt angle (0-1 range)
        float tiltFactor = Mathf.Clamp01(tiltAngle / 30f);
        
        // Apply non-linear curve for more dramatic effect at higher tilts
        float effectFactor = Mathf.Pow(tiltFactor, 1.2f);
        
        // Adjust wave height based on tilt
        waterSimulation.maxWaveHeight = Mathf.Lerp(
            baseWaveHeight,
            baseWaveHeight * 2f,
            effectFactor
        );
        
        // Adjust spring constant (higher = stiffer waves)
        waterSimulation.springConstant = Mathf.Lerp(
            baseSpringConstant,
            baseSpringConstant * 0.7f, // More fluid at high tilts
            effectFactor
        );
        
        // Adjust damping (higher = more viscous)
        waterSimulation.damping = Mathf.Lerp(
            baseDamping,
            baseDamping * 0.8f, // Less damping for more active waves
            effectFactor
        );
    }
} 