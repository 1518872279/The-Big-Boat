using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WaveManager : MonoBehaviour
{
    public static WaveManager instance;

    [Header("Primary Wave")]
    public float amplitude = 1f;
    public float length = 2f;
    public float speed = 1f;
    public float offset = 0f;

    [Header("Secondary Wave")]
    public bool useSecondaryWave = false;
    public float secondaryAmplitude = 0.5f;
    public float secondaryLength = 1f;
    public float secondarySpeed = 0.5f;
    public float secondaryOffset = 0f;

    [Header("Performance Settings")]
    public float distanceFalloff = 100f;  // Distance beyond which waves are less detailed
    public Transform playerTransform;     // For distance-based optimization
    
    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else if (instance != this)
        {
            Debug.Log("Instance already exists, destroying object!");
            Destroy(this);
        }
    }

    void Update()
    {
        offset += Time.deltaTime * speed;
        if (useSecondaryWave)
        {
            secondaryOffset += Time.deltaTime * secondarySpeed;
        }
    }

    public float GetWaveHeight(float _x, float _z = 0f)
    {
        // Optional distance-based optimization
        if (playerTransform != null)
        {
            Vector2 position2D = new Vector2(_x, _z);
            Vector2 playerPos2D = new Vector2(playerTransform.position.x, playerTransform.position.z);
            float distanceToPlayer = Vector2.Distance(position2D, playerPos2D);
            
            if (distanceToPlayer > distanceFalloff)
            {
                // Simpler wave calculation for distant areas
                return amplitude * 0.5f * Mathf.Sin(_x / (length * 2) + offset);
            }
        }

        float height = amplitude * Mathf.Sin(_x / length + offset);
        
        // Add secondary wave if enabled
        if (useSecondaryWave)
        {
            height += secondaryAmplitude * Mathf.Sin(_x / secondaryLength + secondaryOffset + Mathf.Sin(_z / (secondaryLength * 0.8f)));
        }
        
        // Add subtle variation based on Z coordinate if provided
        if (_z != 0f)
        {
            height += amplitude * 0.1f * Mathf.Sin(_z / (length * 1.5f) + offset * 0.8f);
        }
        
        return height;
    }
}
