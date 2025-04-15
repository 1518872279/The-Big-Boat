using UnityEngine;
using System.Collections.Generic;

public class BoatBuoyancy : MonoBehaviour
{
    [Header("Buoyancy Settings")]
    public float waterLevel = 0.0f;
    public float buoyancyForce = 10.0f;
    public float waterDrag = 0.99f;
    public float waterAngularDrag = 0.5f;
    
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
    
    private Rigidbody rb;
    private float[] pointsHeightFromWater;
    private Vector3 initialPosition;
    private Quaternion initialRotation;
    private Collider boatCollider;
    
    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        boatCollider = GetComponent<Collider>();
        
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
    }
    
    private void FixedUpdate()
    {
        float totalForce = 0.0f;
        int pointsUnderWater = 0;
        
        Vector3 centerOfBuoyancy = Vector3.zero;
        
        // For each floating point
        for (int i = 0; i < floatingPoints.Length; i++)
        {
            // Calculate wave height at this position (sampled from a composite of sine waves)
            float waveHeight = CalculateWaveHeight(floatingPoints[i].position);
            
            // Adjust the effective water level with the wave height
            float effectiveWaterLevel = waterLevel + waveHeight;
            
            // Get the height of the point relative to the water surface
            pointsHeightFromWater[i] = floatingPoints[i].position.y - effectiveWaterLevel;
            
            // If the point is under water
            if (pointsHeightFromWater[i] < 0)
            {
                pointsUnderWater++;
                
                // Calculate force based on depth and apply it
                Vector3 force = Vector3.up * buoyancyForce * Mathf.Abs(pointsHeightFromWater[i]);
                rb.AddForceAtPosition(force, floatingPoints[i].position);
                
                // Accumulate the buoyancy center
                centerOfBuoyancy += floatingPoints[i].position;
                
                // Accumulate total force for drag calculations
                totalForce += force.magnitude;
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
        }
        
        // Add slight natural instability (simulates real water behavior)
        if (Random.Range(0, 100) < 5)
        {
            float randomForce = Random.Range(0.1f, 0.5f);
            Vector3 randomDirection = new Vector3(Random.Range(-1f, 1f), 0, Random.Range(-1f, 1f)).normalized;
            rb.AddTorque(randomDirection * randomForce, ForceMode.Impulse);
        }
        
        // Check for collision with boundaries and apply corrective forces if needed
        CheckAndHandleBoundaryCollisions();
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
                }
            }
        }
        
        // Draw raycast gizmos for boundary detection
        if (Application.isPlaying && rb != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawRay(transform.position, rb.velocity.normalized * 0.5f);
        }
    }
} 