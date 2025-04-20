using UnityEngine;

/// <summary>
/// Controls the rotation of a water simulation plane based on the gazing box rotation.
/// Attach this script to the water plane GameObject.
/// </summary>
public class WaterPlaneController : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Reference to the gazing box controller")]
    public GazingBoxController gazingBox;
    
    [Header("Rotation Settings")]
    [Tooltip("How much the water plane rotates relative to the gazing box")]
    [Range(0.1f, 1.0f)]
    public float rotationMultiplier = 0.5f;
    
    [Tooltip("Maximum allowed tilt angle for the water plane")]
    [Range(1f, 30f)]
    public float maxTiltAngle = 15f;
    
    [Tooltip("How quickly the water plane rotates to match the target rotation")]
    [Range(0.1f, 10f)]
    public float rotationSpeed = 2f;
    
    [Tooltip("Apply rotation around X axis (forward/backward tilt)")]
    public bool rotateAroundX = true;
    
    [Tooltip("Apply rotation around Z axis (left/right tilt)")]
    public bool rotateAroundZ = true;
    
    // The water's neutral rotation when the box is at rest
    private Quaternion neutralRotation;
    
    // Keep track of previous rotation for smoothing
    private Quaternion currentTargetRotation;
    
    // Reference to water simulation components
    private WaterSimulation waterSimulation;
    private WaterSimulationExtension waterExtension;
    
    void Start()
    {
        // Store initial rotation as neutral
        neutralRotation = transform.rotation;
        currentTargetRotation = neutralRotation;
        
        // Try to find gazing box if not assigned
        if (gazingBox == null)
        {
            gazingBox = FindObjectOfType<GazingBoxController>();
            if (gazingBox == null)
            {
                Debug.LogWarning("WaterPlaneController: No GazingBoxController found in the scene!");
            }
        }
        
        // Get water simulation components (if they exist on this GameObject)
        waterSimulation = GetComponent<WaterSimulation>();
        waterExtension = GetComponent<WaterSimulationExtension>();
    }
    
    void LateUpdate()
    {
        if (gazingBox == null) return;
        
        // Get the gazing box tilt
        Vector3 boxTilt = GetGazingBoxTiltAngles();
        
        // Create rotation offset based on box tilt
        Vector3 targetEuler = Vector3.zero;
        
        // Apply rotation based on settings
        if (rotateAroundX)
            targetEuler.x = -boxTilt.x * rotationMultiplier; // Invert X to make tilt feel natural
            
        if (rotateAroundZ)
            targetEuler.z = -boxTilt.z * rotationMultiplier; // Invert Z to make tilt feel natural
            
        // Clamp the maximum tilt angle
        targetEuler.x = Mathf.Clamp(targetEuler.x, -maxTiltAngle, maxTiltAngle);
        targetEuler.z = Mathf.Clamp(targetEuler.z, -maxTiltAngle, maxTiltAngle);
        
        // Convert to quaternion rotation
        Quaternion targetRotation = neutralRotation * Quaternion.Euler(targetEuler);
        
        // Smoothly rotate the water plane
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * rotationSpeed);
        
        // Update wave properties to match the rotation if water simulation exists
        UpdateWaterSimulationProperties(targetEuler);
    }
    
    /// <summary>
    /// Gets the tilt angles from the gazing box
    /// </summary>
    private Vector3 GetGazingBoxTiltAngles()
    {
        if (gazingBox == null) return Vector3.zero;
        
        // Calculate tilt angles by comparing up vector with world up
        Vector3 boxUp = gazingBox.transform.up;
        Vector3 worldUp = Vector3.up;
        
        // Project the up vector onto the XZ and YZ planes to get tilt angles
        Vector3 forwardTilt = Vector3.ProjectOnPlane(boxUp, Vector3.right).normalized;
        Vector3 rightTilt = Vector3.ProjectOnPlane(boxUp, Vector3.forward).normalized;
        
        // Calculate angles
        float xAngle = Vector3.SignedAngle(forwardTilt, worldUp, Vector3.right);
        float zAngle = Vector3.SignedAngle(rightTilt, worldUp, Vector3.forward);
        
        return new Vector3(xAngle, 0f, zAngle);
    }
    
    /// <summary>
    /// Updates water simulation properties to match the plane rotation
    /// </summary>
    private void UpdateWaterSimulationProperties(Vector3 rotationAngles)
    {
        // If we have water simulation component, we can adjust its properties to match
        if (waterSimulation != null)
        {
            // Calculate normalized tilt factors (0-1 range)
            float xTiltFactor = Mathf.Abs(rotationAngles.x) / maxTiltAngle;
            float zTiltFactor = Mathf.Abs(rotationAngles.z) / maxTiltAngle;
            float maxTiltFactor = Mathf.Max(xTiltFactor, zTiltFactor);
            
            // Adjust wave height based on tilt
            // Use non-linear curve for more natural response
            float heightMultiplier = Mathf.Pow(maxTiltFactor, 1.5f);
            
            // Small tilt can slightly increase wave height
            waterSimulation.maxWaveHeight = Mathf.Lerp(
                0.2f,  // Base wave height when flat
                0.6f,  // Maximum wave height at max tilt
                heightMultiplier
            );
            
            // Increase wave spread for more turbulent water when tilted
            waterSimulation.waveSpread = Mathf.Lerp(
                0.08f, // Base spread when flat
                0.18f, // Max spread when tilted
                heightMultiplier
            );
        }
    }
} 