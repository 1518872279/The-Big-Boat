using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraShaker : MonoBehaviour
{
    [Header("Shake Settings")]
    [SerializeField] private float defaultShakeIntensity = 0.5f;
    [SerializeField] private float defaultShakeDuration = 0.3f;
    [SerializeField] private float noiseFrequency = 15f;
    [SerializeField] private AnimationCurve shakeFalloff = AnimationCurve.EaseInOut(0, 1, 1, 0);
    
    // Internal variables
    private Vector3 originalPosition;
    private bool isShaking = false;
    private Coroutine currentShakeCoroutine;
    
    private void Awake()
    {
        originalPosition = transform.localPosition;
    }
    
    /// <summary>
    /// Shakes the camera with the default intensity and duration
    /// </summary>
    public void ShakeCamera()
    {
        ShakeCamera(defaultShakeIntensity, defaultShakeDuration);
    }
    
    /// <summary>
    /// Shakes the camera with a custom intensity and duration
    /// </summary>
    /// <param name="intensity">Shake intensity (amplitude)</param>
    /// <param name="duration">Shake duration in seconds</param>
    public void ShakeCamera(float intensity, float duration)
    {
        // Stop any current shake
        StopCurrentShake();
        
        // Start new shake
        currentShakeCoroutine = StartCoroutine(ShakeCoroutine(intensity, duration));
    }
    
    /// <summary>
    /// Immediately stops any ongoing camera shake
    /// </summary>
    public void StopShake()
    {
        StopCurrentShake();
        ResetCameraPosition();
    }
    
    private void StopCurrentShake()
    {
        if (currentShakeCoroutine != null)
        {
            StopCoroutine(currentShakeCoroutine);
            currentShakeCoroutine = null;
        }
        
        isShaking = false;
    }
    
    private void ResetCameraPosition()
    {
        transform.localPosition = originalPosition;
    }
    
    private IEnumerator ShakeCoroutine(float intensity, float duration)
    {
        isShaking = true;
        float elapsed = 0f;
        
        while (elapsed < duration)
        {
            // Calculate shake progress
            float progress = elapsed / duration;
            
            // Evaluate shake intensity using falloff curve
            float currentIntensity = intensity * shakeFalloff.Evaluate(progress);
            
            // Generate perlin noise offset
            float x = Mathf.PerlinNoise(Time.time * noiseFrequency, 0) * 2 - 1;
            float y = Mathf.PerlinNoise(0, Time.time * noiseFrequency) * 2 - 1;
            
            // Apply shake offset
            Vector3 shakeOffset = new Vector3(x, y, 0) * currentIntensity;
            transform.localPosition = originalPosition + shakeOffset;
            
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        // Reset position
        ResetCameraPosition();
        isShaking = false;
    }
} 