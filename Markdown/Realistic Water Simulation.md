# Realistic Water Physics in a Gazing Box

## Overview

This project simulates a boat floating inside a gazing box (a container similar to a boat in a bottle). The water within the box will have its own physics, meaning that if the box is shaken, the water will slosh and dynamically apply forces to the boat, creating realistic buoyancy and wave interactions.

## Key Concepts

### 1. Dynamic Water Mesh
- **Water Plane**:  
  Create a subdivided mesh to represent the water surface inside the box. The finer the subdivisions, the more detailed the wave deformations.
- **Mesh Deformation**:  
  Deform the mesh vertices in real time to simulate wave motion.

### 2. Water Physics Simulation
- **Spring-Damper Model**:  
  Each vertex in the water mesh is treated as a damped spring. The basic equation is:  acceleration = -k * (currentHeight - restHeight) - c * velocity + externalForce
  Where:  
- **currentHeight**: The current displacement of the vertex.
- **restHeight**: The equilibrium position of the vertex.
- **k**: The spring constant (stiffness).
- **c**: The damping factor.
- **externalForce**: An additional force applied to simulate external influences (e.g., box shaking).

- **Time-Step Update**:  
For every frame:
- Compute acceleration for each vertex.
- Update the velocity and then the vertex height using the time delta.
- Optionally, smooth the results to create a continuous wave effect.

### 3. Integrating Box Shaking
- **Detecting Movement**:  
Use a Rigidbody attached to the gazing box or input tracking to measure its acceleration.
- **Applying the Force**:  
When the box is shaken, compute its acceleration and feed that as an external force to each vertex in the water simulation. This causes the water to react (i.e., slosh) and generate waves that in turn affect any objects (like the boat) in the water.

_Example in pseudo-code:_  
```csharp
// Assuming you have a variable tracking the box's acceleration (boxAcceleration)
float externalForce = boxAcceleration.y * forceMultiplier;
// Pass externalForce to each vertex in the water simulation.
float displacement = waterHeightAtPoint - boatPoint.y;
Vector3 buoyancyForce = Vector3.up * displacement * buoyancyCoefficient;
// Apply buoyancyForce to the boat's Rigidbody.
using UnityEngine;

public class WaterSimulation : MonoBehaviour
{
    public float springConstant = 50f;       // Stiffness
    public float damping = 5f;               // Damping factor
    public float forceMultiplier = 1f;       // Multiplier for external force from box shaking
    private float[] vertexHeights;           // Dynamic water height for each vertex
    private float[] velocities;              // Velocity for each vertex
    private float restHeight = 0f;           // Equilibrium height
    private Mesh mesh;

    // Assume boxAcceleration is updated by another script (e.g., reading from a Rigidbody)
    public Vector3 boxAcceleration;

    void Start()
    {
        mesh = GetComponent<MeshFilter>().mesh;
        // Initialize vertexHeights and velocities arrays based on mesh vertex count.
        int vertexCount = mesh.vertices.Length;
        vertexHeights = new float[vertexCount];
        velocities = new float[vertexCount];
        // Set restHeight for all vertices (could also be different per vertex)
        for (int i = 0; i < vertexCount; i++)
        {
            vertexHeights[i] = restHeight;
            velocities[i] = 0f;
        }
    }

    void Update()
    {
        float deltaTime = Time.deltaTime;
        float externalForce = boxAcceleration.y * forceMultiplier;

        // Update each vertex using the spring-damper model
        for (int i = 0; i < vertexHeights.Length; i++)
        {
            float displacement = vertexHeights[i] - restHeight;
            float acceleration = -springConstant * displacement - damping * velocities[i] + externalForce;
            velocities[i] += acceleration * deltaTime;
            vertexHeights[i] += velocities[i] * deltaTime;
        }

        // Update the mesh vertices with the new heights (pseudo-code)
        UpdateMeshVertices();
    }

    void UpdateMeshVertices()
    {
        Vector3[] vertices = mesh.vertices;
        for (int i = 0; i < vertices.Length; i++)
        {
            // Adjust only the y-value for water height; you may need to convert from local to world coordinates if necessary.
            vertices[i].y = vertexHeights[i];
        }
        mesh.vertices = vertices;
        mesh.RecalculateNormals();
    }
}

