using UnityEngine;

public class WaterSimulation : MonoBehaviour
{
    [Header("Mesh Settings")]
    public int gridSize = 20;                // Number of vertices per side
    public float meshSize = 10f;             // Physical size of the mesh in world units
    public float waterHeight = 0f;           // Base water height (rest position)
    
    [Header("Wave Simulation")]
    public float springConstant = 42f;       // Reduced stiffness for more natural wave movement
    public float damping = 7f;               // Increased damping for better viscosity simulation
    public float forceMultiplier = 1.5f;     // Force multiplier for external forces
    public float waveSpread = 0.12f;         // Increased wave spread for better propagation
    public float maxWaveHeight = 0.4f;       // Maximum displacement for any vertex
    public float viscosity = 0.05f;          // NEW: Controls fluid viscosity (resistance to flow)
    public float surfaceTension = 0.02f;     // NEW: Controls surface tension (cohesion at surface)
    
    [Header("Realistic Fluid Effects")]
    [Tooltip("Enable realistic directional wave propagation")]
    public bool enableDirectionalWaves = true;
    [Tooltip("Enable vorticity calculation for swirls and eddies")]
    public bool enableVorticity = true;
    [Tooltip("Simulates surface tension at wave peaks")]
    public bool enableSurfaceTension = true;
    [Range(0.1f, 2.0f)]
    public float wavePropagationSpeed = 1.0f; // How quickly waves travel through the water
    
    [Header("Visual Shader Settings")]
    public bool updateShaderParameters = true;   // Whether to update the shader based on simulation
    public float shaderWaveStrength = 1.0f;      // Strength of shader wave effect
    public float shaderWaveSpeed = 1.0f;         // Speed multiplier for shader waves
    
    [Header("Performance")]
    public bool simulateInFixedUpdate = true; // Use FixedUpdate for more stable simulation
    
    [Header("References")]
    public GazingBoxController gazingBox;    // Reference to the gazing box
    
    private MeshFilter meshFilter;
    private MeshRenderer meshRenderer;
    private Mesh waterMesh;
    private Vector3[] originalVertices;      // Original vertex positions
    private Vector3[] vertices;              // Current vertex positions
    private float[] vertexHeights;           // Current height offsets
    private float[] velocities;              // Vertex velocities
    private float[] accelerations;           // Vertex accelerations
    private Vector3 lastBoxRotation;         // Previous frame box rotation
    private Vector3 boxAngularVelocity;      // Calculated angular velocity of the box
    
    private int[] triangles;
    private Vector2[] uvs;
    
    // For shader integration
    private Vector4 dynamicWaveA;
    private Vector4 dynamicWaveB;
    private Vector4 dynamicWaveC;
    private float lastShaderUpdateTime = 0f;
    private float shaderUpdateInterval = 0.05f; // Update shader every 50ms
    private MaterialPropertyBlock propertyBlock;
    
    // New variables for enhanced fluid simulation
    private float[] previousHeights;  // Previous frame heights for acceleration calculation
    private Vector2[] flowVelocity;   // 2D flow velocity at each vertex
    private float[] vorticity;        // Vorticity at each vertex (curl of velocity field)
    
    void Start()
    {
        // Get or add required components
        meshFilter = GetComponent<MeshFilter>();
        if (meshFilter == null)
            meshFilter = gameObject.AddComponent<MeshFilter>();
            
        meshRenderer = GetComponent<MeshRenderer>();
        if (meshRenderer == null)
            meshRenderer = gameObject.AddComponent<MeshRenderer>();
        
        // Initialize property block for efficient shader updates
        propertyBlock = new MaterialPropertyBlock();
        
        // Initialize dynamic wave parameters
        dynamicWaveA = new Vector4(1f, 0f, 0.5f, 10f);
        dynamicWaveB = new Vector4(0f, 1f, 0.25f, 20f);
        dynamicWaveC = new Vector4(1f, 1f, 0.15f, 10f);
        
        // Create a new water mesh
        CreateWaterMesh();
        
        // Initialize simulation arrays
        int vertexCount = vertices.Length;
        vertexHeights = new float[vertexCount];
        velocities = new float[vertexCount];
        accelerations = new float[vertexCount];
        
        // Initialize new arrays for enhanced fluid dynamics
        previousHeights = new float[vertexCount];
        flowVelocity = new Vector2[vertexCount];
        vorticity = new float[vertexCount];
        
        // Set the water to be initially flat
        for (int i = 0; i < vertexCount; i++)
        {
            vertexHeights[i] = 0f;
            previousHeights[i] = 0f;
            velocities[i] = 0f;
            accelerations[i] = 0f;
            flowVelocity[i] = Vector2.zero;
            vorticity[i] = 0f;
        }
        
        // Initialize gazing box tracking
        if (gazingBox != null)
        {
            lastBoxRotation = gazingBox.transform.rotation.eulerAngles;
        }
    }
    
    void Update()
    {
        // Update box angular velocity
        if (gazingBox != null)
        {
            Vector3 currentRotation = gazingBox.transform.rotation.eulerAngles;
            boxAngularVelocity = CalculateAngularVelocity(lastBoxRotation, currentRotation);
            lastBoxRotation = currentRotation;
        }
        
        // Run simulation in Update if not using FixedUpdate
        if (!simulateInFixedUpdate)
        {
            SimulateWaves(Time.deltaTime);
            UpdateMesh();
        }
        
        // Update shader parameters if needed
        if (updateShaderParameters && Time.time > lastShaderUpdateTime + shaderUpdateInterval)
        {
            UpdateShaderParameters();
            lastShaderUpdateTime = Time.time;
        }
    }
    
    void FixedUpdate()
    {
        // Run simulation in FixedUpdate for more stable physics
        if (simulateInFixedUpdate)
        {
            SimulateWaves(Time.fixedDeltaTime);
            UpdateMesh();
        }
    }
    
    void CreateWaterMesh()
    {
        waterMesh = new Mesh();
        waterMesh.name = "WaterMesh";
        
        // Create vertices
        vertices = new Vector3[(gridSize + 1) * (gridSize + 1)];
        originalVertices = new Vector3[(gridSize + 1) * (gridSize + 1)];
        uvs = new Vector2[(gridSize + 1) * (gridSize + 1)];
        
        float stepSize = meshSize / gridSize;
        float halfMesh = meshSize / 2f;
        
        for (int z = 0; z <= gridSize; z++)
        {
            for (int x = 0; x <= gridSize; x++)
            {
                int index = z * (gridSize + 1) + x;
                float xPos = x * stepSize - halfMesh;
                float zPos = z * stepSize - halfMesh;
                
                vertices[index] = new Vector3(xPos, waterHeight, zPos);
                originalVertices[index] = vertices[index];
                
                // Generate UVs (0 to 1 range)
                uvs[index] = new Vector2((float)x / gridSize, (float)z / gridSize);
            }
        }
        
        // Create triangles
        triangles = new int[gridSize * gridSize * 6]; // 2 triangles per grid square, 3 vertices each
        
        int triangleIndex = 0;
        for (int z = 0; z < gridSize; z++)
        {
            for (int x = 0; x < gridSize; x++)
            {
                int vertexIndex = z * (gridSize + 1) + x;
                
                // First triangle
                triangles[triangleIndex++] = vertexIndex;
                triangles[triangleIndex++] = vertexIndex + (gridSize + 1);
                triangles[triangleIndex++] = vertexIndex + 1;
                
                // Second triangle
                triangles[triangleIndex++] = vertexIndex + 1;
                triangles[triangleIndex++] = vertexIndex + (gridSize + 1);
                triangles[triangleIndex++] = vertexIndex + (gridSize + 1) + 1;
            }
        }
        
        // Assign to mesh
        waterMesh.vertices = vertices;
        waterMesh.triangles = triangles;
        waterMesh.uv = uvs;
        
        waterMesh.RecalculateNormals();
        waterMesh.RecalculateBounds();
        
        // Assign to mesh filter
        meshFilter.mesh = waterMesh;
    }
    
    void SimulateWaves(float deltaTime)
    {
        Vector3 externalForce = CalculateExternalForce();
        
        // Filter out very small forces to reduce jitter
        if (externalForce.magnitude < 0.15f)
        {
            externalForce = Vector3.zero;
        }
        
        // Store current heights as previous before updating
        System.Array.Copy(vertexHeights, previousHeights, vertexHeights.Length);
        
        // First pass: calculate accelerations with enhanced fluid dynamics
        for (int i = 0; i < vertices.Length; i++)
        {
            // Apply spring-damper formula: a = -k*x - c*v + F
            float displacement = vertexHeights[i];
            float springForce = -springConstant * displacement;
            float dampingForce = -damping * velocities[i];
            
            // Calculate external force based on box rotation/movement
            float vertexForce = externalForce.y;
            
            // Apply tilt-based forces (more force applied to vertices based on box rotation)
            if (gazingBox != null)
            {
                Vector3 localPos = transform.InverseTransformPoint(originalVertices[i]);
                float xTiltEffect = externalForce.z * localPos.x * 0.08f;
                float zTiltEffect = externalForce.x * localPos.z * 0.08f;
                vertexForce += xTiltEffect + zTiltEffect;
            }
            
            // Calculate new acceleration with viscosity term
            float viscosityDamping = -viscosity * Mathf.Abs(velocities[i]) * velocities[i]; // Non-linear damping
            accelerations[i] = springForce + dampingForce + viscosityDamping + (vertexForce * forceMultiplier);
        }
        
        // Second pass: update velocities and positions
        for (int i = 0; i < vertices.Length; i++)
        {
            // Update velocity with current acceleration
            velocities[i] += accelerations[i] * deltaTime;
            
            // Update height with current velocity
            vertexHeights[i] += velocities[i] * deltaTime;
            
            // Add surface tension effect at peaks and troughs
            if (enableSurfaceTension && Mathf.Abs(vertexHeights[i]) > 0.1f)
            {
                // Surface tension tries to flatten peaks and valleys
                float tensionForce = -surfaceTension * Mathf.Sign(vertexHeights[i]) * Mathf.Pow(Mathf.Abs(vertexHeights[i]), 2);
                vertexHeights[i] += tensionForce * deltaTime;
            }
            
            // Clamp maximum wave height
            vertexHeights[i] = Mathf.Clamp(vertexHeights[i], -maxWaveHeight, maxWaveHeight);
            
            // Apply to vertex
            vertices[i] = originalVertices[i] + new Vector3(0, vertexHeights[i], 0);
        }
        
        // Third pass: apply enhanced wave propagation
        if (waveSpread > 0)
        {
            if (enableDirectionalWaves)
                ApplyDirectionalWavePropagation(deltaTime, externalForce);
            else
                ApplyWavePropagation(deltaTime);
        }
        
        // Fourth pass: calculate vorticity (curl of velocity field)
        if (enableVorticity)
            CalculateVorticity(deltaTime);
    }
    
    // Enhanced wave propagation that considers direction of tilting/forces
    void ApplyDirectionalWavePropagation(float deltaTime, Vector3 externalForce)
    {
        // Create a temporary array to store the height changes
        float[] heightChanges = new float[vertices.Length];
        
        // Determine primary wave direction based on external force
        Vector2 waveDirection = new Vector2(externalForce.x, externalForce.z).normalized;
        float directionMagnitude = new Vector2(externalForce.x, externalForce.z).magnitude;
        
        // Adjust spread factor based on direction strength
        float directionalFactor = Mathf.Lerp(1.0f, 2.0f, Mathf.Clamp01(directionMagnitude / 5.0f));
        
        for (int z = 0; z <= gridSize; z++)
        {
            for (int x = 0; x <= gridSize; x++)
            {
                int index = z * (gridSize + 1) + x;
                float totalHeight = 0f;
                float totalWeight = 0f;
                
                // Check surrounding vertices with directional bias
                for (int nz = Mathf.Max(0, z - 1); nz <= Mathf.Min(gridSize, z + 1); nz++)
                {
                    for (int nx = Mathf.Max(0, x - 1); nx <= Mathf.Min(gridSize, x + 1); nx++)
                    {
                        int neighborIndex = nz * (gridSize + 1) + nx;
                        
                        // Skip self
                        if (neighborIndex != index)
                        {
                            // Calculate direction from this vertex to neighbor
                            Vector2 toNeighbor = new Vector2(nx - x, nz - z).normalized;
                            
                            // Directional weight (higher in wave direction)
                            float dirWeight = 1.0f;
                            if (directionMagnitude > 0.5f)
                            {
                                // Dot product determines how aligned neighbor is with wave direction
                                float alignment = Vector2.Dot(waveDirection, toNeighbor);
                                // Higher weight in wave direction, but still allow some sideways/backward propagation
                                dirWeight = Mathf.Lerp(0.5f, directionalFactor, Mathf.Max(0, alignment));
                            }
                            
                            // Calculate distance-based weight (closer = stronger influence)
                            float distance = Vector2.Distance(new Vector2(x, z), new Vector2(nx, nz));
                            float distWeight = 1.0f / (distance * 2f);
                            
                            // Combined weight considers both direction and distance
                            float weight = dirWeight * distWeight;
                            totalWeight += weight;
                            totalHeight += vertexHeights[neighborIndex] * weight;
                            
                            // Add flow velocity influence
                            if (enableVorticity)
                            {
                                // Propagate flow velocity between neighbors
                                flowVelocity[index] += flowVelocity[neighborIndex] * weight * 0.05f;
                            }
                        }
                    }
                }
                
                if (totalWeight > 0)
                {
                    float avgHeight = totalHeight / totalWeight;
                    
                    // Apply wave propagation with speed consideration
                    float propagationFactor = waveSpread * wavePropagationSpeed;
                    heightChanges[index] = (avgHeight - vertexHeights[index]) * propagationFactor;
                    
                    // Update flow velocity based on height differences (creates horizontal flow)
                    if (enableVorticity && x > 0 && x < gridSize && z > 0 && z < gridSize)
                    {
                        // Calculate height gradients
                        float gradientX = (vertexHeights[(z) * (gridSize + 1) + (x + 1)] - 
                                           vertexHeights[(z) * (gridSize + 1) + (x - 1)]) * 0.5f;
                        float gradientZ = (vertexHeights[(z + 1) * (gridSize + 1) + (x)] - 
                                           vertexHeights[(z - 1) * (gridSize + 1) + (x)]) * 0.5f;
                        
                        // Flow moves from high to low points (perpendicular to gradient)
                        Vector2 heightGradient = new Vector2(gradientX, gradientZ);
                        Vector2 flowDirection = new Vector2(-heightGradient.y, heightGradient.x).normalized;
                        float gradientStrength = heightGradient.magnitude;
                        
                        // Update flow velocity
                        flowVelocity[index] += flowDirection * gradientStrength * 0.1f;
                        
                        // Apply viscous damping to flow
                        flowVelocity[index] *= (1f - viscosity * deltaTime);
                    }
                }
            }
        }
        
        // Apply the changes
        for (int i = 0; i < vertices.Length; i++)
        {
            vertexHeights[i] += heightChanges[i];
            vertices[i].y = originalVertices[i].y + vertexHeights[i];
        }
    }
    
    // Original wave propagation method as a fallback
    void ApplyWavePropagation(float deltaTime)
    {
        // Create a temporary array to store the height changes to avoid immediate influence
        float[] heightChanges = new float[vertices.Length];
        
        for (int z = 0; z <= gridSize; z++)
        {
            for (int x = 0; x <= gridSize; x++)
            {
                int index = z * (gridSize + 1) + x;
                float avgNeighborHeight = 0f;
                int neighborCount = 0;
                
                // Check surrounding vertices
                for (int nz = Mathf.Max(0, z - 1); nz <= Mathf.Min(gridSize, z + 1); nz++)
                {
                    for (int nx = Mathf.Max(0, x - 1); nx <= Mathf.Min(gridSize, x + 1); nx++)
                    {
                        int neighborIndex = nz * (gridSize + 1) + nx;
                        
                        // Skip self
                        if (neighborIndex != index)
                        {
                            avgNeighborHeight += vertexHeights[neighborIndex];
                            neighborCount++;
                        }
                    }
                }
                
                if (neighborCount > 0)
                {
                    avgNeighborHeight /= neighborCount;
                    heightChanges[index] = (avgNeighborHeight - vertexHeights[index]) * waveSpread;
                }
            }
        }
        
        // Apply the changes
        for (int i = 0; i < vertices.Length; i++)
        {
            vertexHeights[i] += heightChanges[i];
            vertices[i].y = originalVertices[i].y + vertexHeights[i];
        }
    }
    
    // Calculate vorticity (curl of the velocity field)
    void CalculateVorticity(float deltaTime)
    {
        // Only calculate for interior vertices to avoid boundary issues
        for (int z = 1; z < gridSize; z++)
        {
            for (int x = 1; x < gridSize; x++)
            {
                int index = z * (gridSize + 1) + x;
                
                // Indices for adjacent vertices
                int right = z * (gridSize + 1) + (x + 1);
                int left = z * (gridSize + 1) + (x - 1);
                int up = (z + 1) * (gridSize + 1) + x;
                int down = (z - 1) * (gridSize + 1) + x;
                
                // Calculate curl of the flow velocity field
                float dvx_dz = (flowVelocity[up].x - flowVelocity[down].x) * 0.5f;
                float dvz_dx = (flowVelocity[right].y - flowVelocity[left].y) * 0.5f;
                
                // Vorticity is the curl (rotation) of the flow field
                vorticity[index] = dvx_dz - dvz_dx;
                
                // Apply vorticity confinement (strengthens existing vortices)
                float vortStrength = 0.05f;
                if (Mathf.Abs(vorticity[index]) > 0.01f)
                {
                    // Add a small vertical velocity based on vorticity
                    velocities[index] += vorticity[index] * vortStrength * deltaTime;
                    
                    // Create horizontal flow based on vorticity
                    float angle = vorticity[index] * 10f * Mathf.Deg2Rad;
                    flowVelocity[index] += new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * vortStrength * deltaTime;
                }
            }
        }
    }
    
    void UpdateShaderParameters()
    {
        if (meshRenderer == null || meshRenderer.sharedMaterial == null)
            return;
        
        // Get the current dynamic force information
        Vector3 externalForce = CalculateExternalForce();
        float forceAmount = externalForce.magnitude;
        
        // Adjust Gerstner wave parameters based on simulation state
        meshRenderer.GetPropertyBlock(propertyBlock);
        
        // Update Wave A (primarily affected by X tilt)
        float aAmplitude = 0.5f + Mathf.Abs(externalForce.x) * 0.3f * shaderWaveStrength;
        dynamicWaveA.z = Mathf.Clamp(aAmplitude, 0.1f, 0.8f);
        
        // Update Wave B (primarily affected by Z tilt)
        float bAmplitude = 0.25f + Mathf.Abs(externalForce.z) * 0.3f * shaderWaveStrength;
        dynamicWaveB.z = Mathf.Clamp(bAmplitude, 0.1f, 0.8f);
        
        // Update Wave C (affected by overall motion)
        float cAmplitude = 0.15f + forceAmount * 0.2f * shaderWaveStrength;
        dynamicWaveC.z = Mathf.Clamp(cAmplitude, 0.1f, 0.7f);
        
        // Adjust wave directions based on tilt
        if (forceAmount > 0.2f)
        {
            Vector2 tiltDirection = new Vector2(externalForce.x, externalForce.z).normalized;
            
            // Adjust wave directions slightly toward the tilt direction
            Vector2 waveADir = new Vector2(dynamicWaveA.x, dynamicWaveA.y);
            waveADir = Vector2.Lerp(waveADir.normalized, tiltDirection, 0.3f);
            
            Vector2 waveBDir = new Vector2(dynamicWaveB.x, dynamicWaveB.y);
            waveBDir = Vector2.Lerp(waveBDir.normalized, -tiltDirection, 0.3f); // Opposite direction
            
            dynamicWaveA.x = waveADir.x;
            dynamicWaveA.y = waveADir.y;
            dynamicWaveB.x = waveBDir.x;
            dynamicWaveB.y = waveBDir.y;
        }
        
        // Apply to shader
        propertyBlock.SetVector("_WaveA", dynamicWaveA);
        propertyBlock.SetVector("_WaveB", dynamicWaveB);
        propertyBlock.SetVector("_WaveC", dynamicWaveC);
        
        // Update wave speed based on motion amount
        float adjustedSpeed = 1.0f + forceAmount * 0.5f * shaderWaveSpeed;
        propertyBlock.SetFloat("_WaveSpeed", adjustedSpeed);
        
        // Update foam properties based on motion
        float foamAmount = Mathf.Lerp(0.04f, 0.2f, Mathf.Clamp01(forceAmount * 0.5f));
        propertyBlock.SetFloat("_FoamMinDistance", foamAmount);
        
        // Apply all properties to the renderer
        meshRenderer.SetPropertyBlock(propertyBlock);
    }
    
    void UpdateMesh()
    {
        waterMesh.vertices = vertices;
        waterMesh.RecalculateNormals();
    }
    
    Vector3 CalculateAngularVelocity(Vector3 previousRotation, Vector3 currentRotation)
    {
        // Calculate change in rotation, handling potential 360-degree wrapping
        Vector3 deltaRotation = new Vector3(
            Mathf.DeltaAngle(previousRotation.x, currentRotation.x),
            Mathf.DeltaAngle(previousRotation.y, currentRotation.y),
            Mathf.DeltaAngle(previousRotation.z, currentRotation.z)
        );
        
        // Convert to radians and normalize by time
        return deltaRotation * Mathf.Deg2Rad / (simulateInFixedUpdate ? Time.fixedDeltaTime : Time.deltaTime);
    }
    
    Vector3 CalculateExternalForce()
    {
        if (gazingBox == null)
            return Vector3.zero;
            
        // Calculate force based on box rotation and angular velocity
        Vector3 force = Vector3.zero;
        
        // Box tilt contributes to force (gravity effect)
        Vector3 tiltDirection = gazingBox.transform.up;
        float tiltAngle = Vector3.Angle(tiltDirection, Vector3.up);
        Vector3 tiltForceDirection = Vector3.ProjectOnPlane(tiltDirection, Vector3.up).normalized;
        
        // Only apply tilt force if the angle is significant
        if (tiltAngle > 2.5f) // Increased from 1f to ignore very slight tilts
        {
            // Force increases with tilt angle, but with a gentler curve
            float tiltForceMagnitude = Mathf.Pow(Mathf.Sin(tiltAngle * Mathf.Deg2Rad), 1.1f) * 8.5f; // Reduced from 9.8f for less intensity
            force += new Vector3(
                tiltForceDirection.x * tiltForceMagnitude,
                0,
                tiltForceDirection.z * tiltForceMagnitude
            );
        }
        
        // Reduce angular velocity contribution to force with smoothing
        Vector3 smoothedAngularVelocity = Vector3.Lerp(
            Vector3.zero, 
            boxAngularVelocity, 
            Mathf.Clamp01(boxAngularVelocity.magnitude / 5.0f)  // Apply non-linear smoothing
        );
        
        // Add dampened angular velocity to force
        force += new Vector3(
            smoothedAngularVelocity.x * 0.4f,  // Reduced from 0.5f
            smoothedAngularVelocity.y * 0.08f, // Reduced from 0.1f
            smoothedAngularVelocity.z * 0.4f   // Reduced from 0.5f
        );
        
        return force;
    }
    
    // Public method to sample water height at a world position
    public float GetWaterHeightAt(Vector3 worldPosition)
    {
        // Ensure the mesh is properly initialized
        if (vertices == null || vertices.Length == 0)
        {
            // Return base water height if mesh not initialized yet
            return waterHeight;
        }
        
        // Convert world position to local position on the water plane
        Vector3 localPos = transform.InverseTransformPoint(worldPosition);
        
        // Check if position is outside the water mesh bounds
        float halfSize = meshSize / 2f;
        if (localPos.x < -halfSize || localPos.x > halfSize || 
            localPos.z < -halfSize || localPos.z > halfSize)
        {
            return waterHeight; // Return base water height if outside bounds
        }
        
        // Find the grid cell that contains this position
        float normalizedX = (localPos.x + halfSize) / meshSize;
        float normalizedZ = (localPos.z + halfSize) / meshSize;
        
        int gridX = Mathf.FloorToInt(normalizedX * gridSize);
        int gridZ = Mathf.FloorToInt(normalizedZ * gridSize);
        
        // Clamp to valid grid positions
        gridX = Mathf.Clamp(gridX, 0, gridSize - 1);
        gridZ = Mathf.Clamp(gridZ, 0, gridSize - 1);
        
        // Get the four corners of the grid cell
        int bottomLeft = gridZ * (gridSize + 1) + gridX;
        int bottomRight = bottomLeft + 1;
        int topLeft = (gridZ + 1) * (gridSize + 1) + gridX;
        int topRight = topLeft + 1;
        
        // Verify indices are within bounds
        if (bottomLeft >= vertices.Length || bottomRight >= vertices.Length || 
            topLeft >= vertices.Length || topRight >= vertices.Length)
        {
            return waterHeight; // Return base water height if indices are invalid
        }
        
        // Calculate weights for bilinear interpolation
        float cellX = (normalizedX * gridSize) - gridX;
        float cellZ = (normalizedZ * gridSize) - gridZ;
        
        // Bilinear interpolation of height values
        float bottomHeight = Mathf.Lerp(
            vertices[bottomLeft].y,
            vertices[bottomRight].y,
            cellX
        );
        
        float topHeight = Mathf.Lerp(
            vertices[topLeft].y,
            vertices[topRight].y,
            cellX
        );
        
        float height = Mathf.Lerp(bottomHeight, topHeight, cellZ);
        
        return height;
    }
    
    // Method to get the water normal at a specific position (useful for boat physics)
    public Vector3 GetWaterNormalAt(Vector3 worldPosition)
    {
        // Convert world position to local position
        Vector3 localPos = transform.InverseTransformPoint(worldPosition);
        
        // Find the grid cell
        float halfSize = meshSize / 2f;
        float normalizedX = (localPos.x + halfSize) / meshSize;
        float normalizedZ = (localPos.z + halfSize) / meshSize;
        
        int gridX = Mathf.FloorToInt(normalizedX * gridSize);
        int gridZ = Mathf.FloorToInt(normalizedZ * gridSize);
        
        // Clamp to valid grid positions
        gridX = Mathf.Clamp(gridX, 0, gridSize - 1);
        gridZ = Mathf.Clamp(gridZ, 0, gridSize - 1);
        
        // Get the vertex indices for the cell corners
        int bottomLeft = gridZ * (gridSize + 1) + gridX;
        int bottomRight = bottomLeft + 1;
        int topLeft = (gridZ + 1) * (gridSize + 1) + gridX;
        int topRight = topLeft + 1;
        
        // Create two vectors to compute normal
        Vector3 v1 = vertices[topLeft] - vertices[bottomLeft];
        Vector3 v2 = vertices[bottomRight] - vertices[bottomLeft];
        
        // Calculate normal via cross product
        Vector3 normal = Vector3.Cross(v1, v2).normalized;
        
        // Transform normal to world space
        return transform.TransformDirection(normal);
    }
    
    // Enhanced version of AddForceAtPosition that creates more realistic splash physics
    public void AddForceAtPosition(Vector3 worldPosition, float force, float radius)
    {
        Vector3 localPos = transform.InverseTransformPoint(worldPosition);
        float halfSize = meshSize / 2f;
        
        // Skip if position is outside mesh bounds
        if (localPos.x < -halfSize || localPos.x > halfSize || 
            localPos.z < -halfSize || localPos.z > halfSize)
        {
            return;
        }
        
        // Filter out extremely small forces
        if (Mathf.Abs(force) < 0.05f)
        {
            return;
        }
        
        // Find the impact center grid coordinates
        float normalizedX = (localPos.x + halfSize) / meshSize;
        float normalizedZ = (localPos.z + halfSize) / meshSize;
        int centerX = Mathf.FloorToInt(normalizedX * gridSize);
        int centerZ = Mathf.FloorToInt(normalizedZ * gridSize);
        
        // Create ripple wave pattern with realistic physics
        for (int i = 0; i < vertices.Length; i++)
        {
            Vector3 vertexLocalPos = originalVertices[i];
            float distance = Vector2.Distance(
                new Vector2(vertexLocalPos.x, vertexLocalPos.z),
                new Vector2(localPos.x, localPos.z)
            );
            
            if (distance <= radius)
            {
                // Create initial depression (negative force) for realistic splash
                float falloff;
                bool isUpwardForce = force > 0;
                
                if (isUpwardForce)
                {
                    // For upward forces (like object exiting water)
                    // Create upward peak with slight depression ring
                    if (distance < radius * 0.3f)
                    {
                        // Central peak
                        falloff = Mathf.Pow(1f - (distance / (radius * 0.3f)), 1.2f);
                        velocities[i] += force * falloff;
                    }
                    else
                    {
                        // Surrounding depression ring
                        float ringFalloff = 1f - ((distance - radius * 0.3f) / (radius * 0.7f));
                        ringFalloff = Mathf.Pow(ringFalloff, 1.5f);
                        velocities[i] += -force * 0.2f * ringFalloff; // Smaller negative force for the ring
                    }
                }
                else
                {
                    // For downward forces (like impact)
                    // Create initial depression followed by wave propagation
                    falloff = Mathf.Pow(1f - (distance / radius), 1.2f);
                    
                    // Initial depression at impact point
                    if (distance < radius * 0.5f)
                    {
                        velocities[i] += force * falloff;
                        
                        // Create outward flow from impact point
                        if (enableVorticity && i != centerZ * (gridSize + 1) + centerX)
                        {
                            // Calculate direction away from impact
                            Vector2 impactPos = new Vector2(localPos.x, localPos.z);
                            Vector2 vertexPos = new Vector2(vertexLocalPos.x, vertexLocalPos.z);
                            Vector2 outwardDir = (vertexPos - impactPos).normalized;
                            
                            // Add outward flow velocity proportional to force and proximity
                            flowVelocity[i] += outwardDir * Mathf.Abs(force) * falloff * 0.5f;
                        }
                    }
                }
            }
        }
        
        // Update shader parameters immediately when a significant force is applied
        if (Mathf.Abs(force) > 0.5f && updateShaderParameters)
        {
            // Create a temporary wave effect at the impact position
            if (meshRenderer != null && meshRenderer.sharedMaterial != null)
            {
                meshRenderer.GetPropertyBlock(propertyBlock);
                
                // Increase foam at impact site with radial pattern
                float foamAmount = Mathf.Lerp(0.04f, 0.4f, Mathf.Clamp01(Mathf.Abs(force)));
                propertyBlock.SetFloat("_FoamMinDistance", foamAmount);
                
                // Set impact position for shader to create radial pattern
                propertyBlock.SetVector("_ImpactPosition", new Vector4(normalizedX, normalizedZ, Time.time, Mathf.Abs(force)));
                
                // Increase wave steepness temporarily
                float waveImpact = Mathf.Clamp01(Mathf.Abs(force) * 0.25f);
                Vector4 tempWaveA = propertyBlock.GetVector("_WaveA");
                tempWaveA.z += waveImpact;
                propertyBlock.SetVector("_WaveA", tempWaveA);
                
                meshRenderer.SetPropertyBlock(propertyBlock);
                
                // Update shader parameters next frame
                lastShaderUpdateTime = 0f;
            }
        }
    }
    
    // Update method that integrates with BoatBuoyancy
    public void UpdateBoatBuoyancy(BoatBuoyancy boat)
    {
        if (boat == null)
        {
            Debug.LogWarning("UpdateBoatBuoyancy called with null boat reference");
            return;
        }
        
        // Ensure water simulation is properly initialized
        if (vertices == null || vertices.Length == 0)
        {
            Debug.LogWarning("Water simulation not fully initialized yet. Skipping boat update.");
            return;
        }
        
        if (boat.floatingPoints == null)
        {
            Debug.LogWarning("Boat has no floating points defined");
            return;
        }
        
        foreach (Transform floatPoint in boat.floatingPoints)
        {
            if (floatPoint == null) continue;
            
            // Get water height at each floating point
            float waterHeightAtPoint = GetWaterHeightAt(floatPoint.position);
            
            // Update the wave height in the BoatBuoyancy script
            // This assumes the boat is checking these points against waterLevel + waveHeight
            boat.waterLevel = waterHeightAtPoint;
        }
    }
    
    void OnDrawGizmos()
    {
        // Visualize the water surface area
        if (!Application.isPlaying)
        {
            Gizmos.color = new Color(0, 0.8f, 1, 0.2f);
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawCube(new Vector3(0, 0, 0), new Vector3(meshSize, 0.1f, meshSize));
            
            // Draw wave directions
            Gizmos.color = Color.blue;
            Vector3 center = Vector3.zero;
            Gizmos.DrawLine(center, center + new Vector3(dynamicWaveA.x, 0, dynamicWaveA.y).normalized * 1.5f);
            
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(center, center + new Vector3(dynamicWaveB.x, 0, dynamicWaveB.y).normalized * 1.5f);
            
            Gizmos.color = Color.white;
            Gizmos.DrawLine(center, center + new Vector3(dynamicWaveC.x, 0, dynamicWaveC.y).normalized * 1.5f);
        }
    }
} 