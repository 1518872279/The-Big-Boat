using UnityEngine;

/// <summary>
/// Creates enhanced visual water effects based on boat movement.
/// Attach this component to the same GameObject as your BoatBuoyancy component.
/// </summary>
public class BoatBuoyancyEffects : MonoBehaviour
{
    [Header("References")]
    [Tooltip("The BoatBuoyancy component (auto-assigned if on same object)")]
    public BoatBuoyancy boatBuoyancy;
    
    [Tooltip("The WaterSimulation component")]
    public WaterSimulation waterSimulation;
    
    [Tooltip("Optional particle effect for water splashes")]
    public ParticleSystem splashParticles;
    
    [Header("Wake Effect Settings")]
    [Tooltip("Enable wake effects behind the boat")]
    public bool enableWakeEffects = true;
    
    [Tooltip("Minimum speed required for wake to appear")]
    public float minWakeSpeed = 0.5f;
    
    [Tooltip("How strong the wake effect is")]
    [Range(0.1f, 5.0f)]
    public float wakeStrength = 0.8f;
    
    [Tooltip("Width of the wake behind the boat")]
    [Range(0.1f, 5.0f)]
    public float wakeWidth = 1.0f;
    
    [Header("Impact Effect Settings")]
    [Tooltip("Enable splash effects when boat impacts water")]
    public bool enableImpactEffects = true;
    
    [Tooltip("Minimum velocity for splash to appear")]
    public float minImpactVelocity = 0.75f;
    
    [Tooltip("How splash size scales with impact force")]
    [Range(0.1f, 5.0f)]
    public float impactScale = 0.7f;
    
    [Header("Bobbing Effect Settings")]
    [Tooltip("Enable subtle random bobbing when stationary")]
    public bool enableBobbingEffects = true;
    
    [Tooltip("How often to create subtle random bobbing when stationary")]
    [Range(0.5f, 10.0f)]
    public float bobbingInterval = 3.0f;
    
    [Tooltip("Strength of the bobbing effect")]
    [Range(0.01f, 1.0f)]
    public float bobbingStrength = 0.2f;
    
    [Header("Effect Smoothing")]
    [Tooltip("Enable smoothing to reduce jittery effects")]
    public bool enableSmoothing = true;
    
    [Tooltip("Smoothing speed for velocity calculations")]
    [Range(0.1f, 10.0f)]
    public float velocitySmoothingFactor = 2.0f;
    
    [Tooltip("Minimum time between repeated effects")]
    [Range(0.05f, 1.0f)]
    public float effectCooldown = 0.15f;
    
    // Private variables
    private Rigidbody rb;
    private float lastBobbingTime;
    private float effectTimer = 0f;
    private bool wasUnderwater = false;
    private Vector3 lastVelocity = Vector3.zero;
    private Vector3 smoothedVelocity = Vector3.zero;
    private float lastEffectTime = 0f;
    
    private void Awake()
    {
        // Auto-assign references
        if (boatBuoyancy == null)
            boatBuoyancy = GetComponent<BoatBuoyancy>();
            
        if (boatBuoyancy == null)
        {
            Debug.LogError("BoatBuoyancyEffects requires a BoatBuoyancy component!");
            enabled = false;
            return;
        }
        
        rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            Debug.LogError("BoatBuoyancyEffects requires a Rigidbody component!");
            enabled = false;
            return;
        }
        
        // Auto-find water simulation if not assigned
        if (waterSimulation == null && boatBuoyancy.waterSurface != null)
        {
            waterSimulation = boatBuoyancy.waterSurface;
        }
        
        lastBobbingTime = Time.time;
    }
    
    private void Start()
    {
        if (waterSimulation == null)
        {
            waterSimulation = FindObjectOfType<WaterSimulation>();
            
            if (waterSimulation == null)
            {
                Debug.LogWarning("BoatBuoyancyEffects: No WaterSimulation component found!");
            }
        }
    }
    
    private void Update()
    {
        if (waterSimulation == null || boatBuoyancy == null || rb == null)
            return;
            
        if (boatBuoyancy.floatingPoints == null || boatBuoyancy.floatingPoints.Length == 0)
            return;
            
        // Update effect timer
        effectTimer += Time.deltaTime;
        
        // Update smoothed velocity
        if (enableSmoothing)
        {
            smoothedVelocity = Vector3.Lerp(smoothedVelocity, rb.velocity, 
                Time.deltaTime * velocitySmoothingFactor);
        }
        else
        {
            smoothedVelocity = rb.velocity;
        }
        
        // Get boat data from buoyancy component
        bool isUnderwater = CheckIfBoatIsUnderwater();
        
        // Create wake effects based on speed
        if (enableWakeEffects && effectTimer > effectCooldown)
        {
            CreateWakeEffects();
        }
        
        // Check for impacts with water
        if (enableImpactEffects && effectTimer > effectCooldown)
        {
            CheckForWaterImpacts(isUnderwater);
        }
        
        // Create subtle bobbing when mostly stationary
        if (enableBobbingEffects && Time.time > lastBobbingTime + bobbingInterval && 
            smoothedVelocity.magnitude < 0.5f)
        {
            CreateBobbingEffect();
            lastBobbingTime = Time.time;
        }
        
        // Save state for next frame
        wasUnderwater = isUnderwater;
        lastVelocity = rb.velocity;
    }
    
    private void CreateWakeEffects()
    {
        // Only create wake above minimum speed
        float speed = smoothedVelocity.magnitude;
        if (speed < minWakeSpeed)
            return;
        
        // Calculate normalized wake strength based on speed
        float normalizedSpeed = Mathf.Clamp01((speed - minWakeSpeed) / 10f);
        float currentWakeStrength = wakeStrength * normalizedSpeed * 0.8f; // Additional 20% reduction
        
        // Calculate wake points behind the boat
        Vector3 boatDirection = transform.forward;
        Vector3 wakeCenter = transform.position - boatDirection * 1.5f;
        
        // Apply wake force to water in a V shape
        Vector3 leftWakePoint = wakeCenter - transform.right * wakeWidth * 0.5f;
        Vector3 rightWakePoint = wakeCenter + transform.right * wakeWidth * 0.5f;
        
        // Create the wake with reduced intensity if we have sufficient movement
        if (currentWakeStrength > 0.1f && Time.time > lastEffectTime + effectCooldown)
        {
            // Apply forces with a slight delay between them to reduce computational load
            waterSimulation.AddForceAtPosition(leftWakePoint, currentWakeStrength, 0.5f);
            waterSimulation.AddForceAtPosition(rightWakePoint, currentWakeStrength, 0.5f);
            
            lastEffectTime = Time.time;
            effectTimer = 0f;
        }
    }
    
    private void CheckForWaterImpacts(bool isUnderwater)
    {
        if (boatBuoyancy.floatingPoints == null) return;
        
        foreach (Transform point in boatBuoyancy.floatingPoints)
        {
            if (point == null) continue;
            
            // Get water height at this point
            float waterHeight = waterSimulation.GetWaterHeightAt(point.position);
            
            // Check if this point is entering water
            bool isUnderwaterAtPoint = point.position.y < waterHeight;
            
            // If it's just entered water and has sufficient velocity
            if (isUnderwaterAtPoint && !wasUnderwater)
            {
                Vector3 pointVelocity = rb.GetPointVelocity(point.position);
                float impactVelocity = Mathf.Abs(pointVelocity.y);
                
                if (impactVelocity > minImpactVelocity && Time.time > lastEffectTime + effectCooldown)
                {
                    // Create a splash at this position
                    CreateSplashEffect(impactVelocity);
                    lastEffectTime = Time.time;
                    effectTimer = 0f;
                }
            }
            
            wasUnderwater = isUnderwaterAtPoint;
        }
    }
    
    private void CreateSplashEffect(float impactVelocity)
    {
        // Scale the splash based on impact velocity with reduced intensity
        float splashForce = Mathf.Clamp(impactVelocity * impactScale * 0.8f, 0.5f, 10f);
        float splashRadius = Mathf.Lerp(1f, 3f, splashForce / 10f);
        
        // Create the splash at the boat's position
        waterSimulation.AddForceAtPosition(transform.position, splashForce, splashRadius);
        
        // Play particle effect if available
        if (splashParticles != null)
        {
            // Scale particles based on splash size
            float particleScale = Mathf.Lerp(0.5f, 1.5f, splashForce / 10f);
            splashParticles.transform.localScale = Vector3.one * particleScale;
            
            // Play the effect
            splashParticles.Play();
        }
    }
    
    private void CreateBobbingEffect()
    {
        // Only apply if boat is fairly still
        if (smoothedVelocity.magnitude > 0.5f)
            return;

        // Choose a random point on the boat to apply the force
        Vector3 bobbingPoint = transform.position + 
            new Vector3(
                Random.Range(-1f, 1f), 
                0, 
                Random.Range(-1f, 1f)
            ) * 0.5f;
        
        // Apply a gentle force
        float force = Random.Range(0.1f, 0.3f) * bobbingStrength;
        waterSimulation.AddForceAtPosition(bobbingPoint, force, 1f);
    }
    
    private bool CheckIfBoatIsUnderwater()
    {
        if (boatBuoyancy == null || boatBuoyancy.floatingPoints == null || boatBuoyancy.floatingPoints.Length == 0 || waterSimulation == null)
            return false;
            
        // Check if any floating points are underwater
        int underwaterPoints = 0;
        foreach (Transform point in boatBuoyancy.floatingPoints)
        {
            if (point == null) continue;
            
            float waterHeight = waterSimulation.GetWaterHeightAt(point.position);
            if (point.position.y < waterHeight)
            {
                underwaterPoints++;
            }
        }
        
        // Consider the boat underwater if at least half of the floating points are underwater
        return underwaterPoints >= boatBuoyancy.floatingPoints.Length / 2;
    }
} 