using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Floater : MonoBehaviour
{
    public Rigidbody rigidbody;
    public float depthBeforeSubmerged = 1f;
    public float displacementAmount = 3f;
    public int floaterCount = 1;
    public float waterDrag = 0.99f;
    public float waterAngularDrag = 0.5f;
    
    [Header("Performance Settings")]
    public float updateFrequency = 0.05f; // Time between physics calculations
    public float maxForceDistance = 50f;  // Max distance to calculate detailed physics
    
    private float nextUpdateTime = 0f;
    private Transform playerTransform;
    private bool isVisible = true;
    
    private void Start()
    {
        // Find player or camera for distance-based optimization
        if (Camera.main != null)
        {
            playerTransform = Camera.main.transform;
        }
        
        // Set initial update time with small randomization to distribute calculations
        nextUpdateTime = Time.time + Random.Range(0f, updateFrequency);
    }

    private void FixedUpdate()
    {
        // Skip update if it's not time yet
        if (Time.time < nextUpdateTime)
            return;
            
        nextUpdateTime = Time.time + updateFrequency;
        
        // Check if renderer is visible to camera or within distance threshold
        if (playerTransform != null)
        {
            float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);
            
            // If too far away, use simplified physics
            if (distanceToPlayer > maxForceDistance)
            {
                // Apply only basic gravity and reduced buoyancy for distant objects
                rigidbody.AddForceAtPosition(Physics.gravity / floaterCount, transform.position, ForceMode.Acceleration);
                return;
            }
        }
        
        // Regular detailed physics calculation
        rigidbody.AddForceAtPosition(Physics.gravity / floaterCount, transform.position, ForceMode.Acceleration);
        
        // Get wave height at both X and Z coordinates
        float waveHeight = WaveManager.instance.GetWaveHeight(transform.position.x, transform.position.z);
        
        if (transform.position.y < waveHeight)
        {
            float displacementMultiplier = Mathf.Clamp01((waveHeight - transform.position.y) / depthBeforeSubmerged) * displacementAmount;
            
            // Calculate buoyancy force
            Vector3 buoyancyForce = new Vector3(0f, Mathf.Abs(Physics.gravity.y) * displacementMultiplier, 0f);
            rigidbody.AddForceAtPosition(buoyancyForce, transform.position, ForceMode.Acceleration);
            
            // Apply drag forces
            rigidbody.AddForce(displacementMultiplier * -rigidbody.velocity * waterDrag * Time.fixedDeltaTime, ForceMode.VelocityChange);
            rigidbody.AddTorque(displacementMultiplier * -rigidbody.angularVelocity * waterAngularDrag * Time.fixedDeltaTime, ForceMode.VelocityChange);
            
            // Add small sideways force based on wave gradient for larger objects
            if (floaterCount > 1)
            {
                // Calculate approximate wave gradient
                float nextXPos = transform.position.x + 0.1f;
                float heightDifference = WaveManager.instance.GetWaveHeight(nextXPos, transform.position.z) - waveHeight;
                Vector3 sideForce = new Vector3(-heightDifference * displacementMultiplier * 2f, 0f, 0f);
                rigidbody.AddForce(sideForce, ForceMode.Acceleration);
            }
        }
    }
}
