# Project Summary: Boat Balance in a Gazing Box

## Overview

This project focuses on a unique gameplay mechanic: maintaining the balance of a boat floating on simulated ocean water inside a gazing box (reminiscent of a boat in a bottle). The player controls the glass box using the mouse to influence how the water moves and, in turn, how the boat behaves. The concept also lays the groundwork for later introducing dynamic obstacles and monsters approaching the box.

## Key Concepts

### 1. Gazing Box Setup
- **Visual Representation**:  
  A transparent cube represents the box, potentially enhanced with refraction or distortion shaders to emulate a real glass container.
- **Boundaries**:  
  Inner colliders ensure that neither the water nor the boat escape the box.
- **Camera**:  
  The camera can be set to track or follow the player’s inputs, reinforcing the feeling of tilting or controlling the box.

### 2. Ocean Simulation
- **Wave Dynamics**:  
  Simulate realistic ocean waves within the confined space. Techniques such as Gerstner waves or FFT-based water shaders are ideal for achieving real-time, detailed wave motion.
- **Water-Boat Interaction**:  
  The boat’s buoyancy is affected by the dynamic water surface. Multiple floatation points on the boat will sample the wave heights and normals, applying forces similar to real-life physics.

### 3. Boat Physics
- **Buoyancy and Balance**:  
  The boat's movement relies on correctly integrating buoyant forces. An unstable tilt (induced by the box's rotation) affects the boat’s stability.
- **Stability Challenges**:  
  Players must balance the boat’s tilt using the mouse to keep it afloat within the limits.

### 4. Mouse-Based Control
- **Input Mechanism**:  
  The player uses mouse drag input to rotate the gazing box:
  - **Horizontal Drag**: Tilts the box forward/backward.
  - **Vertical Drag**: Tilts the box side to side.
- **Rotation Limits**:  
  Clamp rotations (e.g., ±30°) to avoid excessive tilt and maintain realistic control.
- **Smoothing and Return**:  
  Use interpolation (e.g., `Quaternion.Slerp`) for smooth transitions and optionally allow the box to self-stabilize by returning to a neutral orientation when no input is detected.

### 5. Future Mechanics: Obstacles & Monsters
- **Obstacle Instantiation**:  
  Later development may spawn obstacles or monsters approaching the gazing box.
- **Additional Dynamics**:  
  These entities might generate disturbances or influence wave patterns when interacting with the box, further challenging player control.

## Example Code: GazingBoxController

Below is a sample Unity C# script demonstrating how to implement mouse drag control on the gazing box:

```csharp
using UnityEngine;

public class GazingBoxController : MonoBehaviour
{
    [Header("Control Settings")]
    public float rotationSensitivity = 0.1f; // How much the box rotates per pixel of mouse movement
    public float maxTilt = 30.0f;            // Maximum allowed tilt (degrees)
    public float returnSpeed = 2.0f;         // Speed at which the box returns to neutral when not dragging

    private Vector3 targetRotationOffset = Vector3.zero;
    private Quaternion neutralRotation;
    private Vector3 previousMousePosition;
    private bool isDragging = false;

    void Start()
    {
        // Cache the initial rotation as the neutral position.
        neutralRotation = transform.rotation;
    }

    void Update()
    {
        // Begin dragging on mouse button press
        if (Input.GetMouseButtonDown(0))
        {
            isDragging = true;
            previousMousePosition = Input.mousePosition;
        }
        // End dragging on mouse button release
        if (Input.GetMouseButtonUp(0))
        {
            isDragging = false;
        }

        // Process rotation during drag
        if (isDragging)
        {
            Vector3 currentMousePosition = Input.mousePosition;
            Vector3 mouseDelta = currentMousePosition - previousMousePosition;

            // Calculate rotation offsets based on mouse movement
            float tiltAroundX = mouseDelta.y * rotationSensitivity;  // Forward/backward tilt
            float tiltAroundZ = -mouseDelta.x * rotationSensitivity; // Side-to-side tilt

            targetRotationOffset.x += tiltAroundX;
            targetRotationOffset.z += tiltAroundZ;

            // Clamp the rotation offsets
            targetRotationOffset.x = Mathf.Clamp(targetRotationOffset.x, -maxTilt, maxTilt);
            targetRotationOffset.z = Mathf.Clamp(targetRotationOffset.z, -maxTilt, maxTilt);

            previousMousePosition = currentMousePosition;
        }
        else
        {
            // Gradually return to a neutral rotation when not dragging
            targetRotationOffset = Vector3.Lerp(targetRotationOffset, Vector3.zero, Time.deltaTime * returnSpeed);
        }

        // Apply the rotation offset
        Quaternion rotationOffset = Quaternion.Euler(targetRotationOffset);
        Quaternion targetRotation = neutralRotation * rotationOffset;
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 5.0f);
    }
}
