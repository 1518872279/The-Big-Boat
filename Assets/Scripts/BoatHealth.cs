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
    [SerializeField] private float cameraShakeIntensity = 0.3f;
    [SerializeField] private float cameraShakeDuration = 0.2f;
    
    [Header("Events")]
    public UnityEvent OnDamaged;
    public UnityEvent OnHealed;
    public UnityEvent OnSink;
    
    // References
    private AudioSource audioSource;
    private bool isInvulnerable = false;
    
    // Properties
    public float MaxHealth => maxHealth;
    public float CurrentHealth => currentHealth;
    public float HealthPercentage => currentHealth / maxHealth;
    
    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        
        currentHealth = maxHealth;
    }
    
    public void TakeDamage(float damage)
    {
        if (isInvulnerable || damage <= 0)
            return;
            
        currentHealth = Mathf.Max(0, currentHealth - damage);
        
        // Trigger damage effects and events
        PlayDamageEffects();
        OnDamaged?.Invoke();
        
        // Apply short invulnerability period
        StartCoroutine(InvulnerabilityCoroutine());
        
        // Check if boat has been destroyed
        if (currentHealth <= 0)
        {
            Sink();
        }
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
    
    private void PlayDamageEffects()
    {
        // Play damage sound if available
        if (damageSounds != null && damageSounds.Length > 0 && audioSource != null)
        {
            AudioClip randomDamageSound = damageSounds[Random.Range(0, damageSounds.Length)];
            audioSource.PlayOneShot(randomDamageSound);
        }
        
        // Spawn damage effect if available
        if (damageEffectPrefab != null)
        {
            Instantiate(damageEffectPrefab, transform.position, Quaternion.identity);
        }
        
        // Shake camera if available
        CameraShaker shaker = Camera.main?.GetComponent<CameraShaker>();
        if (shaker != null)
        {
            shaker.ShakeCamera(cameraShakeIntensity, cameraShakeDuration);
        }
    }
    
    private IEnumerator InvulnerabilityCoroutine()
    {
        isInvulnerable = true;
        yield return new WaitForSeconds(invulnerabilityTime);
        isInvulnerable = false;
    }
} 