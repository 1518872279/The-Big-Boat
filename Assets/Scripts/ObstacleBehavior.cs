using UnityEngine;
using System.Collections;

public class ObstacleBehavior : MonoBehaviour
{
    // Public properties (set by ObstacleManager)
    [HideInInspector] public Transform targetBox;
    [HideInInspector] public Vector3 moveDirection;
    [HideInInspector] public float moveSpeed = 1.0f;
    [HideInInspector] public float rotationSpeed = 0f;
    
    // Configuration
    [Header("Obstacle Settings")]
    public float minDamage = 10f;
    public float maxDamage = 25f;
    public float lifetimeInSeconds = 60f; // Max seconds before auto-destruction
    public float destroyDistanceThreshold = 50f; // Distance from box before auto-destruction
    public bool isHostile = true; // Whether this obstacle damages the boat
    
    [Header("VFX")]
    public GameObject hitVFX; // Optional VFX for collision
    public GameObject destroyVFX; // Optional VFX for destruction
    
    [Header("SFX")]
    public AudioClip collisionSound; // Sound played on collision
    
    // Private variables
    private float lifeTimer = 0f;
    private Rigidbody rb;
    private bool hasCollidedWithBoat = false;
    private AudioSource audioSource;
    
    void Start()
    {
        // Get components
        rb = GetComponent<Rigidbody>();
        
        // Add audio source if it doesn't exist and we have sound
        if (collisionSound != null && GetComponent<AudioSource>() == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 1.0f; // 3D sound
            audioSource.maxDistance = 20f;
            audioSource.rolloffMode = AudioRolloffMode.Linear;
        }
        else
        {
            audioSource = GetComponent<AudioSource>();
        }
        
        // Add a collider if missing
        if (GetComponent<Collider>() == null)
        {
            // Default to box collider
            BoxCollider collider = gameObject.AddComponent<BoxCollider>();
            collider.size = Vector3.one; // Default size
            collider.isTrigger = false;
        }
        
        // Apply physics settings
        if (rb != null)
        {
            rb.useGravity = false; // Usually we don't want gravity for obstacles
            rb.constraints = RigidbodyConstraints.FreezePositionY; // Optional: prevent vertical movement
        }
    }
    
    void Update()
    {
        // Update lifetime timer
        lifeTimer += Time.deltaTime;
        
        // Check for auto-destruction conditions
        if (lifeTimer >= lifetimeInSeconds)
        {
            DestroyObstacle();
            return;
        }
        
        // Check distance from box for auto-destruction
        if (targetBox != null && 
            Vector3.Distance(transform.position, targetBox.position) > destroyDistanceThreshold)
        {
            DestroyObstacle();
            return;
        }
        
        // Apply rotation if needed
        if (rotationSpeed != 0)
        {
            transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime);
        }
        
        // Move obstacle if not using physics
        if (rb == null || rb.isKinematic)
        {
            // Simple movement in the assigned direction
            transform.position += moveDirection * moveSpeed * Time.deltaTime;
        }
        else
        {
            // Use physics for movement
            rb.velocity = moveDirection * moveSpeed;
        }
    }
    
    void OnCollisionEnter(Collision collision)
    {
        HandleCollision(collision.gameObject);
    }
    
    void OnTriggerEnter(Collider other)
    {
        HandleCollision(other.gameObject);
    }
    
    private void HandleCollision(GameObject collidedObject)
    {
        // Add debug log to check what we collided with
        Debug.Log("Obstacle collided with: " + collidedObject.name + " (Tag: " + collidedObject.tag + ")");
        
        // Check if we hit the boat - look for BoatHealth component first, then tags
        BoatHealth boatHealth = collidedObject.GetComponent<BoatHealth>();
        
        // If we didn't find the component directly, try checking parent objects
        if (boatHealth == null)
        {
            boatHealth = collidedObject.GetComponentInParent<BoatHealth>();
        }
        
        // Expanded tag check - accept more possible tags and log findings
        bool isBoat = collidedObject.CompareTag("Player") || 
                      collidedObject.CompareTag("Boat") || 
                      collidedObject.name.ToLower().Contains("boat");
        
        if (isBoat)
        {
            Debug.Log("Obstacle identified object as boat, hasCollidedWithBoat=" + hasCollidedWithBoat + ", isHostile=" + isHostile);
        }
        
        if ((boatHealth != null || isBoat) && !hasCollidedWithBoat && isHostile)
        {
            // Prevent multiple collisions
            hasCollidedWithBoat = true;
            
            // Apply damage to the boat if we found the health component
            if (boatHealth != null)
            {
                // Calculate damage based on speed and random factor
                float damage = CalculateDamage();
                
                Debug.Log("Applying damage to boat: " + damage);
                
                // Apply damage to boat
                boatHealth.TakeDamage(damage);
                
                // Play collision sound
                if (audioSource != null && collisionSound != null)
                {
                    audioSource.clip = collisionSound;
                    audioSource.Play();
                }
                
                // Spawn hit VFX
                if (hitVFX != null)
                {
                    Instantiate(hitVFX, transform.position, Quaternion.identity);
                }
                
                // Destroy the obstacle after a short delay
                StartCoroutine(DelayedDestroy());
            }
            else
            {
                Debug.LogWarning("Boat object found but no BoatHealth component attached!");
            }
        }
        
        // Check if we hit the box wall
        if (collidedObject.CompareTag("BoxWall"))
        {
            // Destroy the obstacle when it hits the wall 
            DestroyObstacle();
        }
    }
    
    private float CalculateDamage()
    {
        // Calculate damage based on speed and configured range
        float speedFactor = Mathf.Clamp01(moveSpeed / 3.0f); // Normalized speed factor
        float baseDamage = Mathf.Lerp(minDamage, maxDamage, speedFactor);
        
        // Add a random variation (+/- 20%)
        return baseDamage * Random.Range(0.8f, 1.2f);
    }
    
    private IEnumerator DelayedDestroy()
    {
        // Wait a short time before destroying
        yield return new WaitForSeconds(0.5f);
        DestroyObstacle();
    }
    
    public void DestroyObstacle()
    {
        // Spawn destroy VFX if available
        if (destroyVFX != null)
        {
            Instantiate(destroyVFX, transform.position, Quaternion.identity);
        }
        
        // Destroy this game object
        Destroy(gameObject);
    }
    
    // Public method to check if obstacle should be destroyed
    public bool ShouldBeDestroyed()
    {
        return lifeTimer >= lifetimeInSeconds || 
               (targetBox != null && Vector3.Distance(transform.position, targetBox.position) > destroyDistanceThreshold);
    }
} 