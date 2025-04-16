using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Obstacle : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private bool useCustomMovement = false;
    
    [Header("Rotation Settings")]
    [SerializeField] private bool randomRotation = true;
    [SerializeField] private Vector3 rotationSpeed;
    
    [Header("Collision")]
    [SerializeField] private float collisionDamage = 10f;
    [SerializeField] private bool destroyOnCollision = true;
    [SerializeField] private GameObject collisionEffectPrefab;
    
    // References
    private Transform targetBoat;
    private Vector3 moveDirection = Vector3.back;
    private Rigidbody rb;
    
    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        
        // Set random initial rotation if enabled
        if (randomRotation)
        {
            transform.rotation = Random.rotation;
            
            // Set random rotation speed if not configured
            if (rotationSpeed == Vector3.zero)
            {
                rotationSpeed = new Vector3(
                    Random.Range(-30f, 30f),
                    Random.Range(-30f, 30f),
                    Random.Range(-30f, 30f)
                );
            }
        }
    }
    
    private void Start()
    {
        // If we have a rigidbody, ensure it uses continuous collision detection
        if (rb != null)
        {
            rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        }
    }
    
    public void Initialize(float speed, Transform target)
    {
        moveSpeed = speed;
        targetBoat = target;
        
        // If target exists, set move direction toward target
        if (targetBoat != null && !useCustomMovement)
        {
            Vector3 directionToTarget = (targetBoat.position - transform.position).normalized;
            directionToTarget.y = 0; // Keep obstacle at same height
            moveDirection = directionToTarget;
        }
    }
    
    private void Update()
    {
        // Apply rotation if needed
        if (rotationSpeed != Vector3.zero)
        {
            transform.Rotate(rotationSpeed * Time.deltaTime);
        }
        
        // Move obstacle if not using physics
        if (rb == null || rb.isKinematic)
        {
            transform.position += moveDirection * moveSpeed * Time.deltaTime;
        }
    }
    
    private void FixedUpdate()
    {
        // Apply force if using physics and not kinematic
        if (rb != null && !rb.isKinematic)
        {
            rb.velocity = moveDirection * moveSpeed;
        }
    }
    
    private void OnCollisionEnter(Collision collision)
    {
        // Check if collided with player boat
        BoatHealth boatHealth = collision.gameObject.GetComponent<BoatHealth>();
        
        if (boatHealth != null)
        {
            // Deal damage to boat
            boatHealth.TakeDamage(collisionDamage);
            
            // Spawn collision effect if available
            if (collisionEffectPrefab != null)
            {
                Instantiate(collisionEffectPrefab, collision.contacts[0].point, Quaternion.identity);
            }
            
            // Destroy obstacle if configured
            if (destroyOnCollision)
            {
                Destroy(gameObject);
            }
        }
    }
    
    public void SetSpeed(float speed)
    {
        moveSpeed = speed;
    }
    
    public void SetMoveDirection(Vector3 direction)
    {
        moveDirection = direction.normalized;
    }
} 