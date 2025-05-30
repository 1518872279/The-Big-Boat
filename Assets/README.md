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

### BoatHealth

This script manages the boat's health, damage handling, and related effects.

#### Parameters:

| Parameter | Description |
|-----------|-------------|
| **Health Settings** |
| Max Health | Maximum health value for the boat. |
| Invulnerability Time | Time in seconds the boat remains invulnerable after taking damage. |
| **Damage Feedback** |
| Damage Effect Prefab | Particle effect spawned when the boat takes damage. |
| Damage Sounds | Array of audio clips played randomly when damage is taken. |
| Sink Sound | Sound played when the boat's health reaches zero. |
| Camera Shake Intensity | How strongly the camera shakes when taking damage. |
| Camera Shake Duration | How long the camera shakes when taking damage. |
| **Events** |
| On Health Changed | Event triggered when health value changes. Provides current and max health values. |
| On Damage Taken | Event triggered when the boat takes damage. |
| On Healed | Event triggered when the boat is healed. |
| On Sunk | Event triggered when health reaches zero. |

### ObstacleManager

This script controls the spawning and behavior of obstacles approaching the boat.

#### Parameters:

| Parameter | Description |
|-----------|-------------|
| **Obstacle Prefabs** |
| Obstacle Prefabs | Array of different obstacle types that can be spawned. |
| **Spawn Settings** |
| Spawn Distance From Box | Distance from the box edge where obstacles will spawn. |
| Min/Max Time Between Spawns | Range of time between spawning new obstacles. |
| Max Concurrent Obstacles | Maximum number of obstacles active at once. |
| Difficulty Scaling | How spawn frequency increases with game score. Higher values create faster difficulty progression. |
| Min/Max Spawn Height | Vertical range above water where obstacles can spawn. |
| **Obstacle Movement** |
| Min/Max Move Speed | Range of movement speeds for obstacles. |
| Randomize Rotation | Whether obstacles should rotate as they move. |
| Rotation Speed | How quickly obstacles rotate when rotating is enabled. |
| **References** |
| Gazing Box | Reference to the box containing the boat and water. |
| Water Surface | Reference to the water simulation for height-based spawning. |

## Additional Implementation Instructions

### Setting Up the Health System

1. Add the `BoatHealth.cs` script to your boat GameObject.
2. Configure the health settings:
   - Set `Max Health` to your desired value (default: 3)
   - Set `Invulnerability Time` to provide brief protection after taking damage (default: 1.5 seconds)
3. Set up damage feedback:
   - Assign a particle effect prefab to `Damage Effect Prefab` for visual feedback
   - Add audio clips to the `Damage Sounds` array for randomized damage sounds
   - Assign an audio clip to `Sink Sound` for when the boat sinks
   - Set appropriate values for `Camera Shake Intensity` (0.5-1.0 recommended) and `Camera Shake Duration` (0.3-0.5 seconds recommended)
4. Connect the events:
   - Connect the `On Sunk` event to the GameManager's `GameOver` method
   - Wire up UI elements to display health (optional)
5. Add a `CameraShaker` component to your main camera:
   - The BoatHealth script will automatically find and use it for damage feedback

### Setting Up the Obstacle System

1. Create a new empty GameObject and name it "ObstacleManager"
2. Add the `ObstacleManager.cs` script to this GameObject
3. Create obstacle prefabs:
   - Design at least 3-5 different obstacle types (e.g., rocks, icebergs, sea monsters)
   - Make sure each prefab has a collider component
   - Create and assign the prefabs to the `Obstacle Prefabs` array in the ObstacleManager
4. Configure spawn settings:
   - Set `Spawn Distance From Box` to determine how far away obstacles appear (5-10 units recommended)
   - Adjust `Min Time Between Spawns` and `Max Time Between Spawns` to control frequency
   - Set `Max Concurrent Obstacles` based on desired difficulty (3-7 recommended)
   - Configure `Difficulty Scaling` to increase challenge over time (0.1-0.3 recommended)
5. Configure movement settings:
   - Set appropriate speed ranges with `Min/Max Move Speed`
   - Enable `Randomize Rotation` for more variation
   - Adjust `Rotation Speed` for spinning obstacles (20-40 recommended)
6. Set up references:
   - Assign your GazingBox GameObject to the `Gazing Box` field
   - Assign your WaterSimulation component to the `Water Surface` field
7. Integrate with GameManager:
   - Ensure your GameManager has a reference to the ObstacleManager
   - The GameManager will automatically call StartSpawning and StopSpawning

### Connecting Health and Obstacle Systems

1. Create an `ObstacleBehavior.cs` script and attach it to your obstacle prefabs:
   - This script should handle collision detection with the boat
   - Configure it to call `TakeDamage()` on the BoatHealth component when collisions occur
2. Implement damage logic:
   ```csharp
   void OnCollisionEnter(Collision collision)
   {
       if (collision.gameObject.CompareTag("Boat"))
       {
           // Get the boat health component
           BoatHealth boatHealth = collision.gameObject.GetComponent<BoatHealth>();
           if (boatHealth != null)
           {
               // Apply damage to the boat (adjust damage amount as needed)
               boatHealth.TakeDamage(1);
               
               // Destroy this obstacle or apply any other effects
               Destroy(gameObject);
           }
       }
   }
   ```
3. Test the interaction:
   - Make sure obstacles correctly damage the boat when they collide
   - Verify that damage feedback (particles, sound, camera shake) occurs
   - Confirm that the game ends when health reaches zero

### Setting Up Water Synchronization

To create realistic water interactions between the boat and water system, use these new synchronization components:

1. **WaterSynchronizer**
   - Add this component to your WaterSurface GameObject
   - This script synchronizes the BoatBuoyancy effects with the water simulation and shader
   - Ensure the component has references to:
     - The water simulation component
     - The boat buoyancy component
     - The gazing box controller
     - The renderer with the water shader

2. **WaterSimulationExtension**
   - Add this component to your WaterSurface GameObject
   - This extension specifically targets integration with the Stylized Water 2 shader
   - It maps physical simulation parameters to shader properties for consistent visuals
   - Features include:
     - Maps box tilt to wave direction
     - Adjusts foam amount based on water activity
     - Creates more dynamic and responsive water appearance

3. **BoatBuoyancyEffects**
   - Add this component to your Boat GameObject (same object as BoatBuoyancy)
   - This component creates realistic water effects from boat movement:
     - Wake trails behind the moving boat
     - Splash effects when the boat impacts water
     - Subtle bobbing effects when stationary
   - For best results, create and assign a splash particle system

### Integration Steps

1. Set up the basic scene with GazingBox, WaterSurface, and Boat as described earlier
2. Add the new components to their respective GameObjects
3. Ensure proper references between components:
   ```
   GazingBoxController → WaterSynchronizer → WaterSimulation ← BoatBuoyancy ← BoatBuoyancyEffects
   ```
4. Adjust parameters to achieve your desired visual style:
   - Increase wakeStrength for more prominent boat wakes
   - Adjust waveHeightMultiplier to control water response to tilting
   - Set appropriate foamMultiplier for foam generation

5. Test the integration by:
   - Tilting the gazing box and watching water flow in the appropriate direction
   - Moving the boat to see wake effects
   - Dropping the boat onto the water to see splash effects

### Troubleshooting Water Synchronization

- **No Visual Response to Tilting**: Ensure the WaterSynchronizer has the correct shader property names for your water shader
- **Missing Wake Effects**: Check that the boat has sufficient speed and that wakeStrength isn't too low
- **Shader Not Updating**: Verify the water renderer is properly assigned and the shader contains the expected properties
- **NullReferenceExceptions**: Make sure all component references are properly assigned and initialized

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
- **Script Compile Errors**: Check for syntax errors like missing or extra curly braces. Common errors include:
  - Extra closing braces at the end of script files
  - Missing semicolons after statements
  - Undefined references to components or variables
  - Incorrect namespace usage
- **Obstacles Not Affecting Boat**: If obstacles don't damage the boat when colliding, check these common issues:
  - Verify the boat GameObject has a tag of either "Player" or "Boat"
  - Ensure the boat has the BoatHealth component attached
  - Check Physics settings in Edit > Project Settings > Physics and ensure relevant layers can collide
  - Verify both objects have non-trigger colliders (or if using triggers, both objects have Rigidbody components)
  - Make sure obstacles have the isHostile property set to true
  - Check that damage values (minDamage and maxDamage) are not set too low
- **Water Simulation Initialization Errors**: If you see "Water simulation not fully initialized yet" warnings:
  1. Add the `ComponentInitializer` script to an empty GameObject in your scene
  2. Add the `BoatBuoyancyPatch` script to the same GameObject as your BoatBuoyancy component
  3. Set references in the ComponentInitializer or enable autoFindComponents
  4. This fixes timing issues between BoatBuoyancy and WaterSimulation initialization

## Future Enhancements

Potential features to add:
- More diverse obstacles and monsters
- Power-ups that help stabilize the boat
- Multiple levels with different environments
- Wind effects that influence water behavior
- Multiplayer mode where players compete to keep their boats balanced 