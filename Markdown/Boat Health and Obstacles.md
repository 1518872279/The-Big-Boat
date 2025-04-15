# Boat Health and Obstacle Manager Systems

## Overview

This project involves two key systems for your boat balance game:

1. **Health System for the Boat**:  
   - The boat will change its appearance based on its current health status.
   - It will have an invincible period after taking damage (e.g., when it fails to avoid an obstacle).

2. **Obstacle Manager**:  
   - Obstacles (with multiple types available) are spawned procedurally outside the gazing box and then move inside.
   - They are designed not to block the entire path ahead.
   - Obstacles deal damage to the boat upon collision.
   - Spawning parameters, such as speed, distance, and the number of obstacles per spawn, are configurable to adjust difficulty.

---

## 1. Boat Health System

### Key Features

- **Visual Health Feedback**:  
  The boat changes its sprite or material to indicate full health, damaged, or critical condition.

- **Invincibility Timer**:  
  After taking damage, the boat becomes invincible for a short period to avoid consecutive hits.

### Sample Code: BoatHealth.cs

```csharp
using UnityEngine;

public class BoatHealth : MonoBehaviour
{
    [Header("Health Settings")]
    public int maxHealth = 100;
    public int currentHealth;
    public float invincibleTime = 2f; // Duration of invincibility in seconds

    [Header("Appearance Settings")]
    public SpriteRenderer boatSpriteRenderer;
    public Sprite fullHealthSprite;
    public Sprite damagedSprite;
    public Sprite criticalSprite;

    private bool isInvincible = false;
    private float invincibleTimer = 0f;

    void Start()
    {
        currentHealth = maxHealth;
        UpdateBoatAppearance();
    }

    void Update()
    {
        // Reduce invincibility timer when active
        if (isInvincible)
        {
            invincibleTimer -= Time.deltaTime;
            if (invincibleTimer <= 0)
            {
                isInvincible = false;
            }
        }
    }

    public void TakeDamage(int damage)
    {
        if (isInvincible)
            return;

        currentHealth -= damage;
        if (currentHealth <= 0)
        {
            currentHealth = 0;
            // Implement game over or destruction logic here
        }

        UpdateBoatAppearance();

        // Trigger invincibility period after taking damage
        isInvincible = true;
        invincibleTimer = invincibleTime;
    }

    void UpdateBoatAppearance()
    {
        // Use health percentage to set the appropriate appearance
        float healthPercentage = (float)currentHealth / maxHealth;
        if (healthPercentage > 0.7f)
        {
            boatSpriteRenderer.sprite = fullHealthSprite;
        }
        else if (healthPercentage > 0.3f)
        {
            boatSpriteRenderer.sprite = damagedSprite;
        }
        else
        {
            boatSpriteRenderer.sprite = criticalSprite;
        }
    }
}
