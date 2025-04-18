using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class ObstacleManager : MonoBehaviour
{
    [Header("Obstacle Prefabs")]
    public GameObject[] obstaclePrefabs; // Array of different obstacle types
    
    [Header("Spawn Settings")]
    public float spawnDistanceFromBox = 5f; // Distance outside box to spawn obstacles
    public float minTimeBetweenSpawns = 3f; // Minimum time between spawns 
    public float maxTimeBetweenSpawns = 10f; // Maximum time between spawns
    public int maxConcurrentObstacles = 5; // Maximum number of obstacles at one time
    public float difficultyScaling = 0.1f; // How much spawn timing scales with game time
    public float minSpawnHeight = 0.5f; // Minimum height from water level to spawn
    public float maxSpawnHeight = 2.0f; // Maximum height from water level to spawn
    
    [Header("Obstacle Movement")]
    public float minMoveSpeed = 0.5f; // Minimum obstacle movement speed
    public float maxMoveSpeed = 3.0f; // Maximum obstacle movement speed
    public bool randomizeRotation = true; // Whether obstacles should have random rotation
    public float rotationSpeed = 30f; // Rotation speed for spinning obstacles
    
    [Header("References")]
    public Transform gazingBox; // Reference to the gazing box
    public WaterSimulation waterSurface; // Reference to water simulation for height
    
    // Private fields
    private List<GameObject> activeObstacles = new List<GameObject>();
    private float nextSpawnTime;
    private GameManager gameManager;
    private Vector3 boxCenter;
    private Vector3 boxSize;
    private bool isSpawningPaused = false;

    void Start()
    {
        // Find game manager
        gameManager = FindObjectOfType<GameManager>();
        
        // Cache box center and size
        if (gazingBox != null)
        {
            boxCenter = gazingBox.position;
            BoxCollider boxCollider = gazingBox.GetComponent<BoxCollider>();
            if (boxCollider != null)
            {
                // Get actual world space size accounting for scale
                boxSize = Vector3.Scale(boxCollider.size, gazingBox.localScale);
            }
            else
            {
                // Fallback to a default size
                boxSize = new Vector3(5f, 5f, 5f);
                Debug.LogWarning("No BoxCollider found on gazing box, using default size");
            }
        }
        else
        {
            Debug.LogError("No gazing box assigned to ObstacleManager!");
            enabled = false; // Disable this script
            return;
        }
        
        // Set initial spawn time
        SetNextSpawnTime();
    }

    void Update()
    {
        // Only spawn if game is active and spawning is not paused
        if (gameManager != null && gameManager.IsGameOver() || isSpawningPaused)
        {
            return;
        }
        
        // Check if it's time to spawn and we're under max obstacles
        if (Time.time >= nextSpawnTime && activeObstacles.Count < maxConcurrentObstacles)
        {
            SpawnObstacle();
            SetNextSpawnTime();
        }
        
        // Update all active obstacles
        UpdateObstacles();
        
        // Clean up any null/destroyed obstacles from the list
        CleanupObstacleList();
    }
    
    void SetNextSpawnTime()
    {
        // Calculate difficulty based on game time if game manager exists
        float difficultyFactor = 1.0f;
        if (gameManager != null)
        {
            // Scale spawn time based on score (higher score = faster spawns)
            difficultyFactor = Mathf.Max(0.3f, 1.0f - (gameManager.GetCurrentScore() * difficultyScaling / 100f));
        }
        
        // Adjust spawn interval based on difficulty
        float spawnInterval = Random.Range(minTimeBetweenSpawns, maxTimeBetweenSpawns) * difficultyFactor;
        nextSpawnTime = Time.time + spawnInterval;
    }
    
    void SpawnObstacle()
    {
        if (obstaclePrefabs.Length == 0 || gazingBox == null)
            return;
            
        // Select a random obstacle prefab
        GameObject obstaclePrefab = obstaclePrefabs[Random.Range(0, obstaclePrefabs.Length)];
        
        // Calculate spawn position outside the box
        Vector3 spawnPos = CalculateSpawnPosition();
        
        // Calculate direction towards box center
        Vector3 directionToBox = (boxCenter - spawnPos).normalized;
        
        // Create the obstacle
        GameObject obstacle = Instantiate(obstaclePrefab, spawnPos, Quaternion.identity);
        
        // Add obstacle tag if not already tagged
        if (!obstacle.CompareTag("Obstacle"))
        {
            obstacle.tag = "Obstacle";
        }
        
        // Add ObstacleBehavior component if not present
        ObstacleBehavior behavior = obstacle.GetComponent<ObstacleBehavior>();
        if (behavior == null)
        {
            behavior = obstacle.AddComponent<ObstacleBehavior>();
        }
        
        // Configure the obstacle
        behavior.moveDirection = directionToBox;
        behavior.moveSpeed = Random.Range(minMoveSpeed, maxMoveSpeed);
        behavior.targetBox = gazingBox;
        behavior.rotationSpeed = randomizeRotation ? Random.Range(-rotationSpeed, rotationSpeed) : 0f;
        
        // Add obstacle to tracking list
        activeObstacles.Add(obstacle);
    }
    
    Vector3 CalculateSpawnPosition()
    {
        // Decide which side of the box to spawn from (0=right, 1=left, 2=front, 3=back)
        int side = Random.Range(0, 4);
        
        // Get half extents of box plus spawn distance
        float xExtent = (boxSize.x * 0.5f) + spawnDistanceFromBox;
        float zExtent = (boxSize.z * 0.5f) + spawnDistanceFromBox;
        
        // Base spawn position at box center
        Vector3 spawnPos = boxCenter;
        
        // Modify based on chosen side
        switch (side)
        {
            case 0: // Right
                spawnPos.x += xExtent;
                spawnPos.z += Random.Range(-zExtent, zExtent) * 0.8f; // 80% of width to avoid corners
                break;
            case 1: // Left
                spawnPos.x -= xExtent;
                spawnPos.z += Random.Range(-zExtent, zExtent) * 0.8f;
                break;
            case 2: // Front
                spawnPos.z += zExtent;
                spawnPos.x += Random.Range(-xExtent, xExtent) * 0.8f;
                break;
            case 3: // Back
                spawnPos.z -= zExtent;
                spawnPos.x += Random.Range(-xExtent, xExtent) * 0.8f;
                break;
        }
        
        // Determine Y position based on water height plus random offset
        float waterHeight = 0f;
        if (waterSurface != null)
        {
            waterHeight = waterSurface.GetWaterHeightAt(new Vector3(spawnPos.x, 0, spawnPos.z));
        }
        else
        {
            // Fallback water height if water simulation not available
            waterHeight = 0f;
        }
        
        spawnPos.y = waterHeight + Random.Range(minSpawnHeight, maxSpawnHeight);
        
        return spawnPos;
    }
    
    void UpdateObstacles()
    {
        // Check for obstacles that need to be removed
        for (int i = activeObstacles.Count - 1; i >= 0; i--)
        {
            if (activeObstacles[i] == null)
                continue;
                
            ObstacleBehavior behavior = activeObstacles[i].GetComponent<ObstacleBehavior>();
            
            // Check if obstacle needs to be destroyed (far from box or timed out)
            if (behavior != null && behavior.ShouldBeDestroyed())
            {
                Destroy(activeObstacles[i]);
                activeObstacles.RemoveAt(i);
            }
        }
    }
    
    void CleanupObstacleList()
    {
        // Remove any null entries from the list
        activeObstacles.RemoveAll(item => item == null);
    }
    
    // Public methods for game manager to call
    
    public void PauseSpawning()
    {
        isSpawningPaused = true;
    }
    
    public void ResumeSpawning()
    {
        isSpawningPaused = false;
        SetNextSpawnTime(); // Reset next spawn time when resuming
    }
    
    public void StartSpawning()
    {
        isSpawningPaused = false;
        SetNextSpawnTime();
        activeObstacles.Clear();
    }
    
    public void StopSpawning()
    {
        isSpawningPaused = true;
        ClearAllObstacles();
    }
    
    public void ClearAllObstacles()
    {
        // Destroy all active obstacles
        foreach (GameObject obstacle in activeObstacles)
        {
            if (obstacle != null)
            {
                Destroy(obstacle);
            }
        }
        
        // Clear the list
        activeObstacles.Clear();
    }
} 