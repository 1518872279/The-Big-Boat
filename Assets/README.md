# Boat Balance - The Big Boat

A Unity game where you control a gazing box with a boat floating on water inside it. Your goal is to balance the boat by carefully controlling the tilt of the box.

## Overview

In this game, you control a glass box containing simulated ocean water with a boat floating on it. Using mouse controls, you tilt the box to influence the water's movement and keep the boat from capsizing. As the game progresses, obstacles and sea monsters may approach the box, creating disturbances that make balancing more challenging.

## Setup Instructions

### Prerequisites

- Unity 2020.3 or newer
- Basic knowledge of Unity Editor

### Installation

1. Clone or download this repository
2. Open the project in Unity
3. Open the `Scenes/Main` scene
4. Press Play to test the game

## Setting Up the Game Scene

If you're building the scene from scratch:

1. Create a new 3D scene
2. Create an empty GameObject and name it "GameManager"
   - Add the `GameManager.cs` script to it
3. Create a cube and name it "GazingBox"
   - Scale it to desired size (e.g., 5x5x5)
   - Add the `GazingBoxController.cs` script to it
   - Add a Box Collider component
   - Create a new material using the `GlassBoxShader` shader and apply it to the cube
4. Create a plane and name it "WaterSurface" inside the GazingBox
   - Position it appropriately inside the box
   - Scale it to fit inside the box
   - Create a new material using the `OceanWaterShader` shader and apply it to the plane
5. Create a 3D model for the boat or use a simple cube temporarily
   - Position it above the water surface
   - Add a Rigidbody component
   - Add a Box Collider component
   - Add the `BoatBuoyancy.cs` script to it
6. Create an empty GameObject and name it "ObstacleManager"
   - Add the `ObstacleManager.cs` script to it
7. Set up the UI elements:
   - Create a Canvas
   - Add Text elements for score and balance
   - Create a Game Over panel with a restart button
8. Set up references in the Inspector:
   - In the GameManager, assign the GazingBox, Boat, and WaterSurface
   - In the ObstacleManager, assign the GazingBox and GameManager

## How to Play

- **Controls**: Use the mouse to tilt the gazing box. Click and drag to rotate.
- **Objective**: Keep the boat balanced for as long as possible to achieve a high score.
- **Challenge**: As time passes, the difficulty increases, and obstacles may appear to create disturbances.
- **Game Over**: If the boat tilts beyond a certain threshold, the game ends.

## Customization

### Adjusting Difficulty

You can adjust various parameters in the Inspector to customize the game difficulty:

- In **GameManager**:
  - `difficultyIncreaseRate`: How quickly the game gets harder
  - `balanceThreshold`: Angle at which balance warning appears
  - `gameOverTiltThreshold`: Angle at which the game ends

- In **GazingBoxController**:
  - `rotationSensitivity`: How responsive the box is to mouse movement
  - `maxTilt`: Maximum angle the box can tilt

- In **BoatBuoyancy**:
  - `buoyancyForce`: How strongly the water pushes the boat up
  - `waterDrag`: How much the water slows the boat's movement
  - `waveHeight`, `waveFrequency`, and `waveSpeed`: Controls the water wave behavior

- In **ObstacleManager**:
  - `minSpawnInterval` and `maxSpawnInterval`: How often obstacles appear
  - `moveSpeed`: How quickly obstacles approach the box
  - `maxActiveObstacles`: Maximum number of obstacles at once

### Shaders

The project includes two custom shaders:

1. **OceanWaterShader**: Creates realistic water with Gerstner waves
   - Adjust parameters like wave height, speed, and water color in the material settings

2. **GlassBoxShader**: Creates a transparent glass effect with refraction
   - Adjust parameters like refraction strength, reflection, and thickness in the material settings

## Development Notes

- The water simulation uses Gerstner waves for realistic ocean movement
- The boat uses multiple floating points to calculate buoyancy forces
- Obstacles/monsters can approach the box and create disturbances
- The glass box shader includes refraction and reflection effects

## Future Enhancements

Potential features to add:
- More diverse obstacles and monsters
- Power-ups that help stabilize the boat
- Multiple levels with different environments
- Wind effects that influence water behavior
- Multiplayer mode where players compete to keep their boats balanced 