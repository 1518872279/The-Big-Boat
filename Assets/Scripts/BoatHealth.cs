using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class BoatHealth : MonoBehaviour
{
    [Header("Health Settings")]
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private float currentHealth;
    [SerializeField] private float invulnerabilityTime = 1f;
    
    [Header("Damage Feedback")]
    [SerializeField] private GameObject damageEffectPrefab;
    [SerializeField] private AudioClip[] damageSounds;
    [SerializeField] private AudioClip sinkSound;
    
    [Header("Camera Shake")]
    [Tooltip("How strongly the camera shakes when taking damage")]
    [Range(0.1f, 2.0f)]
    [SerializeField] private float cameraShakeIntensity = 0.5f;
    [Tooltip("How long the camera shakes when taking damage")]
    [Range(0.1f, 1.0f)]
    [SerializeField] private float cameraShakeDuration = 0.3f;
    [Tooltip("Whether the shake intensity scales with damage amount")]
    [SerializeField] private bool scaleCameraShakeWithDamage = true;
    [Tooltip("Minimum damage needed for camera shake")]
    [SerializeField] private float minDamageForShake = 5f;
    
    [Header("Events")]
    public UnityEvent OnDamaged;
    public UnityEvent OnHealed;
    public UnityEvent OnSink;
    
    // References
    private AudioSource audioSource;
    private CameraShaker cameraShaker;
    private float lastDamageTime;
    
    // Properties
    public float MaxHealth => maxHealth;
    public float CurrentHealth => currentHealth;
    public float HealthPercentage => currentHealth / maxHealth;
    
    private void Awake()
    {
        // Initialize health
        currentHealth = maxHealth;
        
        // Get components
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        
        // Find camera shaker in scene
        cameraShaker = FindObjectOfType<CameraShaker>();
        if (cameraShaker == null)
        {
            Debug.LogWarning("No CameraShaker found in the scene. Camera shake effects will be disabled.");
        }
        
        lastDamageTime = -invulnerabilityTime; // Allow damage immediately
    }
    
    /// <summary>
    /// Apply damage to the boat
    /// </summary>
    /// <param name="damageAmount">Amount of damage to apply</param>
    /// <returns>True if damage was applied</returns>
    public bool TakeDamage(float damageAmount)
    {
        // Check if can take damage (due to invulnerability period)
        if (Time.time < lastDamageTime + invulnerabilityTime)
            return false;
            
        // Apply damage
        currentHealth -= damageAmount;
        lastDamageTime = Time.time;
        
        // Clamp health
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
        
        // Play effects
        PlayDamageEffects(damageAmount);
        
        // Fire event
        OnDamaged?.Invoke();
        
        // Check for sink
        if (currentHealth <= 0)
        {
            Sink();
        }
        
        return true;
    }
    
    public void Heal(float amount)
    {
        if (amount <= 0)
            return;
            
        float previousHealth = currentHealth;
        currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
        
        if (currentHealth > previousHealth)
        {
            OnHealed?.Invoke();
        }
    }
    
    public void ResetHealth()
    {
        currentHealth = maxHealth;
    }
    
    private void Sink()
    {
        // Play sink sound if available
        if (sinkSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(sinkSound);
        }
        
        // Trigger sink event
        OnSink?.Invoke();
        
        // Additional sink behavior can be implemented here
        // For example, disable controls, play animation, etc.
    }
    
    /// <summary>
    /// Play damage related effects (particles, sounds, camera shake)
    /// </summary>
    private void PlayDamageEffects(float damageAmount)
    {
        // Calculate normalized damage amount (0-1 scale)
        float normalizedDamage = Mathf.Clamp01(damageAmount / 20f);
        
        // Spawn damage effect
        if (damageEffectPrefab != null)
        {
            // Instantiate with random position offset and rotation
            Vector3 offset = new Vector3(
                Random.Range(-0.5f, 0.5f),
                Random.Range(0.1f, 0.5f),
                Random.Range(-0.5f, 0.5f)
            );
            
            Quaternion randomRotation = Quaternion.Euler(0, Random.Range(0, 360), 0);
            
            GameObject effect = Instantiate(
                damageEffectPrefab, 
                transform.position + offset, 
                randomRotation
            );
            
            // Scale effect based on damage amount
            float effectScale = Mathf.Lerp(0.7f, 1.5f, normalizedDamage);
            effect.transform.localScale = Vector3.one * effectScale;
            
            // Destroy after delay
            Destroy(effect, 5f);
        }
        
        // Play damage sound
        if (damageSounds != null && damageSounds.Length > 0 && audioSource != null)
        {
            // Select random damage sound and play
            AudioClip damageSound = damageSounds[Random.Range(0, damageSounds.Length)];
            
            if (damageSound != null)
            {
                float volume = Mathf.Lerp(0.5f, 1.0f, normalizedDamage);
                float pitch = Random.Range(0.9f, 1.1f);
                
                audioSource.pitch = pitch;
                audioSource.PlayOneShot(damageSound, volume);
            }
        }
        
        // Camera shake
        if (cameraShaker != null && damageAmount >= minDamageForShake)
        {
            // Determine shake intensity
            float intensity = cameraShakeIntensity;
            float duration = cameraShakeDuration;
            
            if (scaleCameraShakeWithDamage)
            {
                // Scale intensity and duration with damage amount
                intensity = Mathf.Lerp(cameraShakeIntensity * 0.5f, cameraShakeIntensity * 1.5f, normalizedDamage);
                duration = Mathf.Lerp(cameraShakeDuration * 0.8f, cameraShakeDuration * 1.2f, normalizedDamage);
            }
            
            // Apply the camera shake
            cameraShaker.ShakeCamera(intensity, duration);
        }
    }
} 