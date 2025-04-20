using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
public class WaterManager : MonoBehaviour
{
    private MeshFilter meshFilter;
    
    [Header("Performance Settings")]
    [Tooltip("Number of frames to skip between updates")]
    public int updateInterval = 1;
    [Tooltip("Max number of vertices to update per frame")]
    public int maxVerticesPerFrame = 1000;
    [Tooltip("Distance from camera at which to stop updating vertices")]
    public float maxUpdateDistance = 150f;
    [Tooltip("Use Z coordinate in wave calculations")]
    public bool useZCoordinate = true;
    
    private int frameCount = 0;
    private int lastVertexIndex = 0;
    private Vector3[] originalVertices;
    private Camera mainCamera;

    private void Awake()
    {
        meshFilter = GetComponent<MeshFilter>();
        mainCamera = Camera.main;
        
        // Store original vertices for reference
        originalVertices = meshFilter.mesh.vertices;
    }

    private void Update()
    {
        frameCount++;
        if (frameCount < updateInterval)
            return;
            
        frameCount = 0;
        
        if (mainCamera == null)
            mainCamera = Camera.main;
            
        Vector3[] vertices = meshFilter.mesh.vertices;
        int verticesUpdated = 0;
        int totalVertices = vertices.Length;
        
        // Start from where we left off in the previous frame
        for (int i = 0; i < totalVertices && verticesUpdated < maxVerticesPerFrame; i++)
        {
            int index = (lastVertexIndex + i) % totalVertices;
            
            // Convert vertex to world position
            Vector3 worldPos = transform.TransformPoint(originalVertices[index]);
            
            // Skip vertices too far from camera
            if (mainCamera != null && Vector3.Distance(worldPos, mainCamera.transform.position) > maxUpdateDistance)
            {
                // For distant vertices, use a simpler calculation or fixed height
                vertices[index].y = 0;
                continue;
            }
            
            // Calculate new vertex height
            float z = useZCoordinate ? originalVertices[index].z : 0f;
            vertices[index].y = WaveManager.instance.GetWaveHeight(
                transform.position.x + originalVertices[index].x,
                transform.position.z + z
            );
            
            verticesUpdated++;
        }
        
        // Store the last processed index for next frame
        lastVertexIndex = (lastVertexIndex + verticesUpdated) % totalVertices;
        
        // Apply changes
        meshFilter.mesh.vertices = vertices;
        
        // Only recalculate normals once all vertices have been updated
        if (lastVertexIndex < maxVerticesPerFrame || lastVertexIndex == 0)
        {
            meshFilter.mesh.RecalculateNormals();
        }
    }
}
