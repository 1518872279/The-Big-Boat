# Boat Balance - The Big Boat

A Unity game where you control a gazing box with a boat floating on water inside it. Your goal is to balance the boat by carefully controlling the tilt of the box.

## Overview

In this game, you control a glass box containing simulated ocean water with a boat floating on it. Using mouse controls, you tilt the box to influence the water's movement and keep the boat from capsizing. As the game progresses, obstacles and sea monsters may approach the box, creating disturbances that make balancing more challenging.

## Setup Instructions

### Prerequisites

- Unity 2022.3 or newer (with Universal Render Pipeline installed)
- Basic knowledge of Unity Editor

### Installation

1. Clone or download this repository
2. Open the project in Unity
3. Open the `Scenes/Main` scene
4. Press Play to test the game

## Setting Up the Game Scene

### Basic Setup

1. Create a new 3D scene
2. Create an empty GameObject and name it "GameManager"
   - Add the `GameManager.cs` script to it
3. Create a cube and name it "GazingBox"
   - Scale it to desired size (e.g., 5x5x5)
   - Add the `GazingBoxController.cs` script to it
   - Add a Box Collider component
   - Create a new material using the `SimpleURPGlass` shader and apply it to the cube
4. Create a plane and name it "WaterSurface" inside the GazingBox
   - Position it appropriately inside the box
   - Add the `WaterSimulation.cs` script to it
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
   - Add Text element for tilt timer warning (optional)
   - Create a Game Over panel with a restart button

### Layer Setup

1. Create the following layers in your project:
   - "Boat" - For the boat object
   - "BoxBoundary" - For the gazing box colliders

2. Assign these layers:
   - Set your boat GameObject to the "Boat" layer
   - The box colliders will be automatically assigned to the "BoxBoundary" layer by the script

### Connecting Components

1. In the **GazingBoxController** component:
   - Set "Boat Layer Name" to "Boat"
   - Set "Box Collider Layer Name" to "BoxBoundary"

2. In the **WaterSimulation** component:
   - Assign the GazingBox to the "Gazing Box" field
   - Adjust mesh and wave simulation parameters as needed

3. In the **BoatBuoyancy** component:
   - Assign the WaterSimulation to the "Water Surface" field
   - Set "Use Dynamic Water Surface" to true
   - Set the "Boundary Layer Mask" to include only the "BoxBoundary" layer
   - Configure the "Direction Control" settings for boat orientation
   - Set the "Stability Monitoring" parameters to control game-over conditions

4. In the **GameManager** component:
   - Assign the GazingBox, Boat, and WaterSurface
   - Set up UI references including the optional tilt timer text
   - Add appropriate sound effects for balance warnings and critical tilt warnings

## Component Details

### GazingBoxController

This script handles the mouse-based rotation of the glass box container.

#### Parameters:

| Parameter | Description |
|-----------|-------------|
| **Control Settings** |
| Rotation Sensitivity | Controls how much the box rotates per pixel of mouse movement. Higher values make the box more responsive but harder to control. |
| Max Tilt | Maximum allowed tilt angle in degrees. Limits how far the box can tilt. |
| Return Speed | How quickly the box returns to a neutral position when not being dragged. Higher values create faster self-stabilization. |
| **Collision Settings** |
| Boat Layer Name | The layer assigned to the boat for physics interactions. |
| Box Collider Layer Name | The layer assigned to the box boundaries for collision detection. |

### WaterSimulation

This script creates a dynamic water surface that responds to the box's movement and the boat's interactions.

#### Parameters:

| Parameter | Description |
|-----------|-------------|
| **Mesh Settings** |
| Grid Size | Number of vertices per side of the water mesh. Higher values create more detailed waves but impact performance. |
| Mesh Size | Physical size of the water mesh in world units. Should match the interior dimensions of your box. |
| Water Height | Base height of the water surface at rest. |
| **Wave Simulation** |
| Spring Constant | Stiffness (k) of the spring simulation. Higher values create more rigid, responsive water. |
| Damping | Damping factor (c) controlling wave energy dissipation. Higher values reduce wave persistence. |
| Force Multiplier | Multiplier for external forces. Increases the impact of box movement on water. |
| Wave Spread | Controls how waves propagate to neighboring vertices. Higher values create smoother, more connected waves. |
| Max Wave Height | Maximum displacement for any vertex. Limits extreme wave heights. |
| **Visual Shader Settings** |
| Update Shader Parameters | Whether to update the shader based on simulation. Enable for visual-physics synchronization. |
| Shader Wave Strength | Strength of shader wave effects. Higher values create more dramatic visual waves. |
| Shader Wave Speed | Speed multiplier for shader waves. Affects the perceived flow rate of water. |
| **References** |
| Gazing Box | Reference to the GazingBoxController component. Required for the water to respond to box movement. |

### BoatBuoyancy

This script simulates the boat's buoyancy and physics interactions with water.

#### Parameters:

| Parameter | Description |
|-----------|-------------|
| **Buoyancy Settings** |
| Water Level | Base water height (only used when not using dynamic water). |
| Buoyancy Force | Strength of the upward force applied to submerged points. Higher values make the boat more buoyant. |
| Water Drag | Resistance applied to the boat's movement in water. Higher values slow the boat more when in water. |
| Water Angular Drag | Rotational resistance in water. Higher values reduce spinning motion. |
| **Buoyancy Points** |
| Floating Points | Array of transform points used to calculate buoyancy. More points create more accurate water interaction. |
| **Wave Settings** |
| Wave Height | Height of waves (for simple wave model). Only used when not using dynamic water. |
| Wave Frequency | How closely waves are spaced (for simple wave model). |
| Wave Speed | How quickly waves move (for simple wave model). |
| **Direction Control** |
| Maintain Direction | Whether to automatically keep the boat oriented in a specific direction. |
| Target Direction | The direction vector the boat should try to face (default is forward/Z-axis). |
| Direction Force | Strength of the auto-orientation. Higher values make the boat turn more aggressively. |
| Direction Damping | Reduces oscillation when auto-orienting. Higher values create smoother turning. |
| **Stability Monitoring** |
| Max Allowed Tilt | Maximum tilt angle in degrees before triggering the game-over sequence. |
| Max Tilt Duration | How many seconds the boat can remain tilted beyond the maximum before the game ends. |
| **Collision Handling** |
| Bounce Force | Force applied when the boat collides with box boundaries. Higher values create stronger bounces. |
| Bounce Damping | Reduction in velocity after collision. Higher values make collisions less bouncy. |
| Boundary Layer Mask | Layer mask for detecting collisions with box boundaries. |
| **Dynamic Water Reference** |
| Water Surface | Reference to the WaterSimulation component for dynamic water interaction. |
| Use Dynamic Water Surface | Whether to use the advanced water simulation instead of the simple wave model. |

### GameManager

This script controls the game flow, score tracking, and win/loss conditions.

#### Parameters:

| Parameter | Description |
|-----------|-------------|
| **Game Objects** |
| Gazing Box | Reference to the glass box container GameObject. |
| Boat | Reference to the boat GameObject. |
| Water Surface | Reference to the water surface GameObject. |
| **Game Settings** |
| Difficulty Increase Rate | How quickly the game difficulty scales up over time. |
| Max Difficulty | The maximum difficulty level the game can reach. |
| Balance Threshold | Angle at which balance warnings begin to appear. |
| Game Over Tilt Threshold | Maximum tilt angle allowed for the boat (synced with BoatBuoyancy.maxAllowedTilt). |
| **UI References** |
| Score Text | Text element for displaying the current score. |
| Balance Text | Text element for displaying the current tilt angle. |
| Tilt Timer Text | Text element for displaying the time remaining before game over when critically tilted. |
| Game Over Panel | UI panel that appears when the game ends. |
| Restart Button | Button for restarting the game after game over. |
| **Audio** |
| Water Ambient Sound | Background sound for water ambience. |
| Balance Warning Sound | Sound played when exceeding the balance threshold. |
| Tilt Warning Sound | Sound played when the boat is critically tilted for a concerning duration. |
| Game Over Sound | Sound played when the game ends. |

## Shader Parameters

### SimpleURPGlass Shader

This shader creates a glass effect for the gazing box.

#### Parameters:

| Parameter | Description |
|-----------|-------------|
| Tint Color | Base color of the glass. Alpha controls base transparency. |
| Specular Color | Color of highlight reflections on the glass. |
| Specular Power | Intensity of specular highlights. Higher values create more noticeable reflections. |
| Transparency | Overall transparency of the glass. Lower values make the glass more see-through. |
| Edge Thickness | Thickness of colored edges. Higher values create more noticeable edges. |
| Edge Color | Color of the glass edges. Makes boundaries more visible. |

### OceanWaterShader

This shader creates realistic-looking water with dynamic wave patterns.

#### Parameters:

| Parameter | Description |
|-----------|-------------|
| Color | Base color of the water. Alpha controls base transparency. |
| Albedo (RGB) | Texture for the water surface. |
| Normal Map | Texture for creating small surface details. |
| Smoothness | Controls how shiny the water appears. Higher values create more reflective water. |
| Metallic | Controls the metallic look of the water. Usually kept low for water. |
| Wave A | Primary wave parameters (direction, steepness, wavelength). |
| Wave B | Secondary wave parameters moving in a different direction. |
| Wave C | Tertiary wave parameters for additional complexity. |
| Wave Speed | Speed at which waves animate. Higher values create faster-moving waves. |
| Water Depth | Simulated depth of the water. Affects coloration. |
| Depth Gradient Shallow | Color of shallow water areas. |
| Depth Gradient Deep | Color of deeper water areas. |
| Depth Maximum Distance | Distance between shallow and deep water for color gradation. |
| Foam Color | Color of foam/white caps on waves. |
| Foam Maximum Distance | Maximum distance from surface where foam appears. |
| Foam Minimum Distance | Minimum distance from surface where foam appears. |

## How to Play

- **Controls**: Use the mouse to tilt the gazing box. Click and drag to rotate.
- **Objective**: Keep the boat balanced for as long as possible to achieve a high score.
- **Challenge**: As time passes, the difficulty increases, and obstacles may appear to create disturbances.
- **Game Over**: If the boat tilts beyond the maximum threshold for longer than the allowed duration, the game ends.
- **Boat Direction**: The boat will automatically try to maintain its forward orientation in the specified direction, adding a stability challenge.

## Advanced Implementation Tips

### Water Physics Optimization

1. **Performance Balancing**:
   - Adjust `gridSize` in WaterSimulation based on your target platform
   - For mobile or lower-end devices, use 10-15
   - For desktop, 20-30 provides good detail without performance issues

2. **Wave Simulation Tuning**:
   - Start with `springConstant` around 50 and `damping` around 5
   - If water looks too stiff, decrease `springConstant`
   - If waves persist too long, increase `damping`
   - For violent, choppy water, increase `maxWaveHeight` and decrease `waveSpread`

### Boat Physics Tuning

1. **Stable Buoyancy**:
   - Create at least 5 floating points (four corners and center bottom)
   - Ensure the boat's center of mass is slightly below the midpoint for stability
   - Start with `buoyancyForce` around 10 and adjust based on boat size

2. **Realistic Collision Response**:
   - Set `bounceForce` between 3-8 depending on desired bounciness
   - Use `bounceDamping` around 0.8 to prevent excessive bouncing
   - Ensure the boat collider doesn't have sharp edges that could catch on the box

3. **Direction Control Tuning**:
   - For smoother direction maintenance, set `directionForce` between 1-3
   - Increase `directionDamping` to prevent oscillation around the target direction
   - For a more challenging game, disable `maintainDirection` or reduce `directionForce`

4. **Stability Challenge Tuning**:
   - Set `maxAllowedTilt` to match the `gameOverTiltThreshold` in GameManager
   - For a more forgiving game, increase `maxTiltDuration` to 3-5 seconds
   - For a challenging game, reduce it to 1-2 seconds

### Visual-Physics Integration

1. **Synchronized Appearance**:
   - Enable `updateShaderParameters` in WaterSimulation
   - Adjust `shaderWaveStrength` between 0.5-2.0 based on desired visual intensity
   - For calm waters that visibly respond to tilting, use lower values

2. **Impact Effects**:
   - The WaterSimulation will automatically create visual splashes when the boat impacts water
   - For more dramatic effects, place an empty GameObject with a particle effect system as a child of the boat
   - Trigger particle effects in BoatBuoyancy when detecting significant velocity changes

### UI Enhancements

1. **Tilt Warning UI**:
   - Add a UI element for the tilt duration timer that appears only when critically tilted
   - Use color transitions from yellow to red as the timer approaches zero
   - Add animated effects to draw attention to the critical warning

2. **Directional Indicators**:
   - Consider adding a UI arrow showing the target direction for the boat
   - Create a visual indicator for when the boat is fighting against the automatic direction control

## Troubleshooting

- **Pink Shaders**: Make sure you're using the Universal Render Pipeline and have the correct shaders assigned.
- **Boat Escaping Box**: Check that the layers are properly set up and the physics materials are working.
- **Unstable Boat**: Try increasing water drag and angular drag, or add more floating points to the boat.
- **Performance Issues**: Reduce the grid size of the water simulation and simplify the colliders.
- **Visuals Not Matching Physics**: Ensure the WaterSimulation is properly connected to both the shader material and BoatBuoyancy component.
- **Boat Spinning**: If the boat spins too much, increase the `directionDamping` parameter or decrease `directionForce`.
- **Game Ending Too Quickly**: Increase the `maxTiltDuration` to give players more time to recover from extreme tilts.

## Future Enhancements

Potential features to add:
- More diverse obstacles and monsters
- Power-ups that help stabilize the boat
- Multiple levels with different environments
- Wind effects that influence water behavior
- Multiplayer mode where players compete to keep their boats balanced 