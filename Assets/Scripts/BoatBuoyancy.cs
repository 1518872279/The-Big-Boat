using UnityEngine;
using System.Collections.Generic;

public class BoatBuoyancy : MonoBehaviour
{
    [Header("Buoyancy Settings")]
    public float waterLevel = 0.0f;
    public float buoyancyForce = 10.0f;
    public float waterDrag = 0.99f;
    public float waterAngularDrag = 0.5f;
    
    [Header("Air Physics")]
    [Tooltip("Controls enhanced gravity when boat is out of water")]
    public float airGravityMultiplier = 2.5f;
    [Tooltip("Drag applied when in air")]
    public float airDrag = 0.1f;
    [Tooltip("How quickly the boat stabilizes in air")]
    public float airAngularDrag = 0.2f;
    [Tooltip("Maximum time to apply enhanced falling physics")]
    public float maxEnhancedFallTime = 5.0f;
    
    [Header("Buoyancy Points")]
    public Transform[] floatingPoints;
    
    [Header("Wave Settings")]
    public float waveHeight = 0.5f;
    public float waveFrequency = 1.0f;
    public float waveSpeed = 1.0f;
    
    [Header("Collision Handling")]
    public float bounceForce = 5.0f;
    public float bounceDamping = 0.8f;
    public LayerMask boundaryLayerMask;
    
    [Header("Direction Control")]
    public bool maintainDirection = true;
    public Vector3 targetDirection = Vector3.forward; // Default direction (Z axis)
    public float directionForce = 2.0f;
    public float directionDamping = 0.5f;
    
    [Header("Stability Monitoring")]
    public float maxAllowedTilt = 60.0f;          // Maximum tilt angle before triggering game over sequence
    public float maxTiltDuration = 3.0f;          // How long the boat can be over max tilt before game over
    [SerializeField] private float currentTiltAngle = 0.0f;  // Current tilt angle (for debugging)
    [SerializeField] private float overTiltTimer = 0.0f;     // How long the boat has been over max tilt
    
    [Header("Dynamic Water Reference")]
    public WaterSimulation waterSurface;
    public bool useDynamicWaterSurface = true;
    
    private Rigidbody rb;
    private float[] pointsHeightFromWater;
    private Vector3 initialPosition;
    private Quaternion initialRotation;
    private Collider boatCollider;
    private bool wasInWater = true;
    private float timeOutOfWater = 0f;
    private Vector3 standardGravity;
    
    // Event to notify game manager when boat has been tilted for too long
    public delegate void BoatStabilityEvent();
    public event BoatStabilityEvent OnMaxTiltExceeded;
    
    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        boatCollider = GetComponent<Collider>();
        
        // Store the standard gravity setting
        standardGravity = Physics.gravity;
        
        // If no floating points are specified, create some based on the collider
        if (floatingPoints == null || floatingPoints.Length == 0)
        {
            CreateFloatingPoints();
        }
        
        // Store initial configuration for reset
        initialPosition = transform.position;
        initialRotation = transform.rotation;
        
        // Initialize the height tracking array
        pointsHeightFromWater = new float[floatingPoints.Length];
        
        // For box collider interaction, prevent ignoring collisions between boat and box
        // by setting up physics materials with reduced friction and higher bounce
        if (boatCollider != null && boatCollider.material == null)
        {
            PhysicMaterial boatPhysicsMaterial = new PhysicMaterial("BoatPhysicsMaterial");
            boatPhysicsMaterial.dynamicFriction = 0.1f;
            boatPhysicsMaterial.staticFriction = 0.1f;
            boatPhysicsMaterial.bounciness = 0.5f;
            boatPhysicsMaterial.frictionCombine = PhysicMaterialCombine.Minimum;
            boatPhysicsMaterial.bounceCombine = PhysicMaterialCombine.Maximum;
            boatCollider.material = boatPhysicsMaterial;
        }
        
        // Register the boat with the water simulation for efficient updates
        if (waterSurface != null && useDynamicWaterSurface)
        {
            try
            {
                // Initial registration - wrapped in try/catch to prevent startup errors
                waterSurface.UpdateBoatBuoyancy(this);
            }
            catch (System.NullReferenceException e)
            {
                Debug.LogWarning("Error registering boat with water surface: " + e.Message);
                // Continue without crashing - we'll try again during gameplay
            }
        }
        else if (useDynamicWaterSurface && waterSurface == null)
        {
            Debug.LogWarning("BoatBuoyancy has useDynamicWaterSurface set to true but no WaterSurface assigned!");
        }
        
        // Normalize target direction to ensure consistent behavior
        if (targetDirection != Vector3.zero)
        {
            targetDirection.Normalize();
        }
        else
        {
            targetDirection = Vector3.forward;
        }
    }
    
    private void FixedUpdate()
    {
        // Check if any floating point is in water
        bool isInWater = IsAnyPointInWater();
        
        // Update time tracking for air physics
        if (!isInWater)
        {
            if (wasInWater)
            {
                // Just left water
                timeOutOfWater = 0f;
            }
            timeOutOfWater += Time.fixedDeltaTime;
            ApplyAirPhysics();
        }
        else
        {
            timeOutOfWater = 0f;
            // Ensure standard gravity is restored when in water
            rb.useGravity = true;
        }
        
        wasInWater = isInWater;
        
        ApplyBuoyancyForces();
        
        // Check for collision with boundaries and apply corrective forces if needed
        CheckAndHandleBoundaryCollisions();
        
        // Maintain boat direction if enabled
        if (maintainDirection)
        {
            MaintainBoatDirection();
        }
        
        // Monitor boat stability
        MonitorBoatStability();
    }
    
    private void ApplyBuoyancyForces()
    {
        float totalForce = 0.0f;
        int pointsUnderWater = 0;
        
        Vector3 centerOfBuoyancy = Vector3.zero;
        
        // For each floating point
        for (int i = 0; i < floatingPoints.Length; i++)
        {
            float effectiveWaterLevel;
            Vector3 waterNormal = Vector3.up;
            
            if (waterSurface != null && useDynamicWaterSurface)
            {
                // Use the dynamic water simulation to determine water height and normal
                effectiveWaterLevel = waterSurface.GetWaterHeightAt(floatingPoints[i].position);
                waterNormal = waterSurface.GetWaterNormalAt(floatingPoints[i].position);
            }
            else
            {
                // Fallback to the simple sine wave model
                float waveHeight = CalculateWaveHeight(floatingPoints[i].position);
                effectiveWaterLevel = waterLevel + waveHeight;
            }
            
            // Get the height of the point relative to the water surface
            pointsHeightFromWater[i] = floatingPoints[i].position.y - effectiveWaterLevel;
            
            // If the point is under water
            if (pointsHeightFromWater[i] < 0)
            {
                pointsUnderWater++;
                
                // Calculate force based on depth and apply it
                Vector3 force = waterNormal * buoyancyForce * Mathf.Abs(pointsHeightFromWater[i]);
                rb.AddForceAtPosition(force, floatingPoints[i].position);
                
                // Accumulate the buoyancy center
                centerOfBuoyancy += floatingPoints[i].position;
                
                // Accumulate total force for drag calculations
                totalForce += force.magnitude;
                
                // Add force to the water simulation (cause splash)
                if (waterSurface != null && useDynamicWaterSurface)
                {
                    float impactVelocity = Vector3.Dot(rb.GetPointVelocity(floatingPoints[i].position), Vector3.down);
                    if (impactVelocity > 1.0f)
                    {
                        // Create splash effect proportional to velocity
                        waterSurface.AddForceAtPosition(
                            floatingPoints[i].position,
                            -impactVelocity * 0.05f, // Negative force creates a depression
                            0.3f  // Small radius for localized effect
                        );
                    }
                }
            }
        }
        
        // Apply drag when in water
        if (pointsUnderWater > 0)
        {
            // Calculate average buoyancy center
            centerOfBuoyancy /= pointsUnderWater;
            
            // Apply drag force proportional to velocity
            rb.AddForceAtPosition(-rb.velocity * waterDrag * totalForce * Time.fixedDeltaTime, centerOfBuoyancy);
            
            // Apply angular drag
            rb.angularVelocity *= (1 - waterAngularDrag * Time.fixedDeltaTime);
            
            // Reset air physics when in water
            rb.drag = 0.01f; // Low drag in water (handled manually via waterDrag)
            rb.angularDrag = 0.01f; // Low angular drag in water (handled manually)
        }
        
        // Add slight natural instability (simulates real water behavior)
        if (Random.Range(0, 100) < 5)
        {
            float randomForce = Random.Range(0.1f, 0.5f);
            Vector3 randomDirection = new Vector3(Random.Range(-1f, 1f), 0, Random.Range(-1f, 1f)).normalized;
            rb.AddTorque(randomDirection * randomForce, ForceMode.Impulse);
        }
    }
    
    private void MaintainBoatDirection()
    {
        // Project current forward direction onto the horizontal plane
        Vector3 currentDirection = Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized;
        
        // Project target direction onto the horizontal plane
        Vector3 desiredDirection = Vector3.ProjectOnPlane(targetDirection, Vector3.up).normalized;
        
        // Calculate the rotation needed to align with target direction
        float angleDifference = Vector3.SignedAngle(currentDirection, desiredDirection, Vector3.up);
        
        // Apply torque to rotate toward the target direction
        if (Mathf.Abs(angleDifference) > 1.0f)  // Only apply force if there's a significant angle difference
        {
            // Calculate torque strength based on angle difference
            float torqueStrength = Mathf.Sign(angleDifference) * directionForce * Mathf.Min(Mathf.Abs(angleDifference) / 45.0f, 1.0f);
            
            // Apply torque around the up axis
            rb.AddTorque(Vector3.up * torqueStrength, ForceMode.Force);
            
            // Apply damping to horizontal angular velocity to prevent excessive oscillation
            Vector3 angularVel = rb.angularVelocity;
            angularVel.y *= (1f - directionDamping * Time.fixedDeltaTime);
            rb.angularVelocity = angularVel;
        }
    }
    
    private void MonitorBoatStability()
    {
        // Calculate current tilt angle from upright position
        currentTiltAngle = Vector3.Angle(Vector3.up, transform.up);
        
        // Check if tilt exceeds maximum allowed
        if (currentTiltAngle > maxAllowedTilt)
        {
            // Increment timer while over max tilt
            overTiltTimer += Time.fixedDeltaTime;
            
            // If over the threshold for too long, trigger event
            if (overTiltTimer >= maxTiltDuration && OnMaxTiltExceeded != null)
            {
                OnMaxTiltExceeded.Invoke();
                overTiltTimer = 0f; // Reset to prevent repeated triggers
            }
        }
        else
        {
            // Reset timer when within acceptable tilt
            overTiltTimer = 0f;
        }
    }
    
    private void LateUpdate()
    {
        // Add wake effect when the boat is moving
        if (waterSurface != null && useDynamicWaterSurface && rb.velocity.magnitude > 0.5f)
        {
            // Create wake behind the boat
            Vector3 boatDirection = transform.forward;
            Vector3 wakePosition = transform.position - boatDirection * 0.5f;
            
            // Apply more force the faster the boat is moving
            float wakeForce = -rb.velocity.magnitude * 0.05f;
            float wakeRadius = 0.5f + rb.velocity.magnitude * 0.1f;
            
            waterSurface.AddForceAtPosition(wakePosition, wakeForce, wakeRadius);
        }
    }
    
    private void CheckAndHandleBoundaryCollisions()
    {
        // Use physics raycasts to detect and handle imminent collisions with the box boundaries
        float raycastDistance = 0.5f; // Adjust based on boat size and velocity
        RaycastHit hit;
        
        // Get boat's current velocity direction for relevant checks
        Vector3 velocityDir = rb.velocity.normalized;
        
        // Cast rays from the boat's center and edges in the velocity direction
        if (Physics.Raycast(transform.position, velocityDir, out hit, raycastDistance, boundaryLayerMask))
        {
            // Apply force in the opposite direction of the collision
            Vector3 bounceDirection = Vector3.Reflect(velocityDir, hit.normal);
            float forceMagnitude = rb.velocity.magnitude * bounceForce;
            
            // Apply bounce force
            rb.AddForce(bounceDirection * forceMagnitude, ForceMode.Impulse);
            
            // Dampen velocity to prevent excessive bouncing
            rb.velocity *= bounceDamping;
        }
    }
    
    private void OnCollisionEnter(Collision collision)
    {
        // Check if we're colliding with a gazing box boundary
        if (boundaryLayerMask == (boundaryLayerMask | (1 << collision.gameObject.layer)))
        {
            // Get the contact point and normal
            ContactPoint contact = collision.contacts[0];
            
            // Calculate reflection vector for more natural bounce
            Vector3 reflectDir = Vector3.Reflect(rb.velocity.normalized, contact.normal);
            
            // Apply force away from the wall
            rb.AddForce(reflectDir * bounceForce, ForceMode.Impulse);
            
            // Dampen velocity
            rb.velocity *= bounceDamping;
            
            // Add impact to water simulation
            if (waterSurface != null && useDynamicWaterSurface)
            {
                float impactMagnitude = collision.relativeVelocity.magnitude;
                waterSurface.AddForceAtPosition(
                    contact.point, 
                    -impactMagnitude * 0.2f, 
                    impactMagnitude * 0.3f
                );
            }
        }
    }
    
    private float CalculateWaveHeight(Vector3 position)
    {
        float x = position.x * waveFrequency;
        float z = position.z * waveFrequency;
        float time = Time.time * waveSpeed;
        
        // Combine several sine waves for more realistic water
        float height = 0;
        height += Mathf.Sin(x * 0.8f + time * 0.9f) * waveHeight * 0.5f;
        height += Mathf.Sin(z * 0.7f + time * 1.1f) * waveHeight * 0.3f;
        height += Mathf.Sin((x + z) * 0.5f + time * 0.7f) * waveHeight * 0.2f;
        
        return height;
    }
    
    private void CreateFloatingPoints()
    {
        // Create 4 floating points at the corners of the boat
        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            Vector3 center = col.bounds.center - transform.position;
            Vector3 size = col.bounds.size;
            
            List<Transform> points = new List<Transform>();
            
            // Create floating points at the bottom corners of the collider
            for (int x = -1; x <= 1; x += 2)
            {
                for (int z = -1; z <= 1; z += 2)
                {
                    GameObject point = new GameObject("FloatingPoint");
                    point.transform.parent = transform;
                    point.transform.localPosition = center + new Vector3(x * size.x * 0.5f, -size.y * 0.5f, z * size.z * 0.5f);
                    points.Add(point.transform);
                }
            }
            
            // Add a central bottom point
            GameObject centerPoint = new GameObject("FloatingPointCenter");
            centerPoint.transform.parent = transform;
            centerPoint.transform.localPosition = center + new Vector3(0, -size.y * 0.5f, 0);
            points.Add(centerPoint.transform);
            
            floatingPoints = points.ToArray();
        }
    }
    
    public void ResetBoat()
    {
        // Reset the boat to its starting position and rotation
        transform.position = initialPosition;
        transform.rotation = initialRotation;
        
        if (rb != null)
        {
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
        
        // Reset stability monitoring
        overTiltTimer = 0f;
    }
    
    public float GetCurrentTiltAngle()
    {
        return currentTiltAngle;
    }
    
    public float GetOverTiltTime()
    {
        return overTiltTimer;
    }
    
    private void OnDrawGizmos()
    {
        // Draw gizmos for the floating points
        if (floatingPoints != null)
        {
            Gizmos.color = Color.blue;
            foreach (Transform point in floatingPoints)
            {
                if (point != null)
                {
                    Gizmos.DrawSphere(point.position, 0.1f);
                    
                    // If we're using the water surface, draw the water level at each point
                    if (Application.isPlaying && waterSurface != null && useDynamicWaterSurface)
                    {
                        float waterHeight = waterSurface.GetWaterHeightAt(point.position);
                        Vector3 waterPos = new Vector3(point.position.x, waterHeight, point.position.z);
                        
                        Gizmos.color = Color.cyan;
                        Gizmos.DrawSphere(waterPos, 0.05f);
                        Gizmos.DrawLine(point.position, waterPos);
                    }
                }
            }
        }
        
        // Draw raycast gizmos for boundary detection
        if (Application.isPlaying && rb != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawRay(transform.position, rb.velocity.normalized * 0.5f);
        }
        
        // Draw target direction indicator
        if (maintainDirection)
        {
            Gizmos.color = Color.green;
            Vector3 start = transform.position;
            Vector3 end = start + targetDirection.normalized * 2f;
            Gizmos.DrawLine(start, end);
            
            // Draw a small arrow tip
            Vector3 right = Vector3.Cross(Vector3.up, targetDirection).normalized * 0.2f;
            Gizmos.DrawLine(end, end - targetDirection.normalized * 0.5f + right);
            Gizmos.DrawLine(end, end - targetDirection.normalized * 0.5f - right);
        }
    }
    
    // New method to check if any point is in water
    private bool IsAnyPointInWater()
    {
        if (floatingPoints == null || floatingPoints.Length == 0)
            return false;
        
        foreach (Transform point in floatingPoints)
        {
            if (point == null) continue;
            
            float effectiveWaterLevel;
            if (waterSurface != null && useDynamicWaterSurface)
            {
                effectiveWaterLevel = waterSurface.GetWaterHeightAt(point.position);
            }
            else
            {
                float waveHeight = CalculateWaveHeight(point.position);
                effectiveWaterLevel = waterLevel + waveHeight;
            }
            
            if (point.position.y < effectiveWaterLevel)
            {
                return true;
            }
        }
        
        return false;
    }
    
    // New method to apply air physics when boat is completely out of water
    private void ApplyAirPhysics()
    {
        // Apply enhanced falling physics for better air behavior
        float gravityMultiplier = 1.0f;
        
        // Scale gravity effect based on how long the boat has been out of water
        float normalizedTime = Mathf.Clamp01(timeOutOfWater / 0.5f); // Quick ramp-up
        gravityMultiplier = Mathf.Lerp(1.0f, airGravityMultiplier, normalizedTime);
        
        // Apply increased downward force to help boat return to water
        if (timeOutOfWater < maxEnhancedFallTime)
        {
            Vector3 enhancedGravity = standardGravity * gravityMultiplier;
            rb.AddForce(enhancedGravity * rb.mass, ForceMode.Acceleration);
        }
        
        // Apply air drag
        rb.drag = airDrag;
        rb.angularDrag = airAngularDrag;
        
        // If boat is stuck in air for too long, apply directional force toward water
        if (timeOutOfWater > 2.0f)
        {
            // Find nearest water point - approximate as the water level below the boat
            Vector3 boatPos = transform.position;
            float targetWaterHeight;
            
            if (waterSurface != null && useDynamicWaterSurface)
            {
                targetWaterHeight = waterSurface.GetWaterHeightAt(new Vector3(boatPos.x, 0, boatPos.z));
            }
            else
            {
                targetWaterHeight = waterLevel + CalculateWaveHeight(new Vector3(boatPos.x, 0, boatPos.z));
            }
            
            // Calculate direction toward water
            Vector3 directionToWater = new Vector3(0, targetWaterHeight - boatPos.y, 0).normalized;
            
            // Apply force toward water, increasing with time
            float rescueForce = Mathf.Lerp(0.5f, 2.0f, (timeOutOfWater - 2.0f) / 3.0f);
            rb.AddForce(directionToWater * rescueForce * rb.mass, ForceMode.Acceleration);
            
            // Gradually stabilize rotation in air for better landing
            Quaternion targetRotation = Quaternion.Euler(0, transform.eulerAngles.y, 0);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.fixedDeltaTime * 0.3f);
        }
    }
} 