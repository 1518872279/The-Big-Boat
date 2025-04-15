using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GameManager : MonoBehaviour
{
    [Header("Game Objects")]
    public GameObject gazingBox;
    public GameObject boat;
    public GameObject waterSurface;
    
    [Header("Game Settings")]
    public float difficultyIncreaseRate = 0.1f;
    public float maxDifficulty = 3.0f;
    public float balanceThreshold = 30.0f;
    public float gameOverTiltThreshold = 60.0f;
    
    [Header("UI References")]
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI balanceText;
    public TextMeshProUGUI tiltTimerText;  // Optional - displays how long boat has been over-tilted
    public GameObject gameOverPanel;
    public Button restartButton;
    
    [Header("Audio")]
    public AudioClip waterAmbientSound;
    public AudioClip balanceWarningSound;
    public AudioClip gameOverSound;
    public AudioClip tiltWarningSound;     // Sound played when boat is critically tilted
    
    // Private fields
    private float currentScore = 0f;
    private float currentDifficulty = 1f;
    private float gameTimer = 0f;
    private bool isGameOver = false;
    private AudioSource audioSource;
    private bool isBalanceWarning = false;
    private bool isCriticalTiltWarning = false;
    
    // Components
    private GazingBoxController boxController;
    private BoatBuoyancy boatBuoyancy;
    
    private void Start()
    {
        // Get references to components
        boxController = gazingBox.GetComponent<GazingBoxController>();
        boatBuoyancy = boat.GetComponent<BoatBuoyancy>();
        
        // Subscribe to boat stability event
        if (boatBuoyancy != null)
        {
            boatBuoyancy.OnMaxTiltExceeded += HandleBoatMaxTilt;
        }
        
        // Setup audio
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        
        if (waterAmbientSound != null)
        {
            audioSource.clip = waterAmbientSound;
            audioSource.loop = true;
            audioSource.Play();
        }
        
        // Setup UI
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(false);
        }
        
        if (restartButton != null)
        {
            restartButton.onClick.AddListener(RestartGame);
        }
        
        // Make sure the boat's tilt settings match the game settings
        if (boatBuoyancy != null)
        {
            boatBuoyancy.maxAllowedTilt = gameOverTiltThreshold;
        }
        
        UpdateUI();
    }
    
    private void OnDestroy()
    {
        // Unsubscribe from events to prevent memory leaks
        if (boatBuoyancy != null)
        {
            boatBuoyancy.OnMaxTiltExceeded -= HandleBoatMaxTilt;
        }
    }
    
    private void Update()
    {
        if (isGameOver)
            return;
        
        // Increase game timer and score
        gameTimer += Time.deltaTime;
        currentScore += Time.deltaTime * currentDifficulty;
        
        // Gradually increase difficulty
        if (currentDifficulty < maxDifficulty)
        {
            currentDifficulty += difficultyIncreaseRate * Time.deltaTime;
        }
        
        // Check boat balance
        CheckBoatBalance();
        
        // Update UI
        UpdateUI();
    }
    
    private void CheckBoatBalance()
    {
        if (boat == null || boatBuoyancy == null)
            return;
            
        // Get the current tilt angle from the BoatBuoyancy component
        float tiltAngle = boatBuoyancy.GetCurrentTiltAngle();
        float overTiltTime = boatBuoyancy.GetOverTiltTime();
        
        // Show warning when the boat is tilting beyond the threshold
        if (tiltAngle > balanceThreshold && !isBalanceWarning)
        {
            isBalanceWarning = true;
            
            // Play warning sound
            if (balanceWarningSound != null && audioSource != null)
            {
                audioSource.PlayOneShot(balanceWarningSound);
            }
        }
        else if (tiltAngle <= balanceThreshold)
        {
            isBalanceWarning = false;
        }
        
        // Critical tilt warning when nearing the max allowed duration
        if (tiltAngle > gameOverTiltThreshold)
        {
            // Play critical warning sound if we're halfway to the time limit
            if (overTiltTime > boatBuoyancy.maxTiltDuration * 0.5f && !isCriticalTiltWarning)
            {
                isCriticalTiltWarning = true;
                
                if (tiltWarningSound != null && audioSource != null)
                {
                    audioSource.PlayOneShot(tiltWarningSound);
                }
            }
        }
        else
        {
            isCriticalTiltWarning = false;
        }
    }
    
    private void HandleBoatMaxTilt()
    {
        // This method is called when the boat has been tilted for too long
        if (!isGameOver)
        {
            GameOver();
        }
    }
    
    private void UpdateUI()
    {
        if (scoreText != null)
        {
            scoreText.text = "Score: " + Mathf.FloorToInt(currentScore).ToString();
        }
        
        if (balanceText != null && boatBuoyancy != null)
        {
            float tiltAngle = boatBuoyancy.GetCurrentTiltAngle();
            balanceText.text = "Balance: " + Mathf.FloorToInt(tiltAngle).ToString() + "°";
            
            // Change color based on tilt
            if (tiltAngle > gameOverTiltThreshold)
            {
                balanceText.color = Color.red;
            }
            else if (tiltAngle > balanceThreshold)
            {
                balanceText.color = new Color(1f, 0.5f, 0f); // Orange
            }
            else
            {
                balanceText.color = Color.white;
            }
        }
        
        // Update tilt timer text if available
        if (tiltTimerText != null && boatBuoyancy != null && boatBuoyancy.GetCurrentTiltAngle() > gameOverTiltThreshold)
        {
            float remainingTime = boatBuoyancy.maxTiltDuration - boatBuoyancy.GetOverTiltTime();
            
            if (remainingTime > 0)
            {
                tiltTimerText.gameObject.SetActive(true);
                tiltTimerText.text = "Stabilize: " + remainingTime.ToString("F1") + "s";
                
                // Color the text based on urgency
                float t = remainingTime / boatBuoyancy.maxTiltDuration;
                tiltTimerText.color = Color.Lerp(Color.red, Color.yellow, t);
            }
        }
        else if (tiltTimerText != null)
        {
            tiltTimerText.gameObject.SetActive(false);
        }
    }
    
    private void GameOver()
    {
        isGameOver = true;
        
        // Play game over sound
        if (gameOverSound != null && audioSource != null)
        {
            audioSource.Stop();
            audioSource.PlayOneShot(gameOverSound);
        }
        
        // Show game over UI
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
        }
        
        // Disable controls
        if (boxController != null)
        {
            boxController.enabled = false;
        }
    }
    
    public void RestartGame()
    {
        isGameOver = false;
        currentScore = 0f;
        currentDifficulty = 1f;
        gameTimer = 0f;
        isBalanceWarning = false;
        isCriticalTiltWarning = false;
        
        // Hide game over UI
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(false);
        }
        
        // Reset boat position
        if (boatBuoyancy != null)
        {
            boatBuoyancy.ResetBoat();
        }
        
        // Reset box controller
        if (boxController != null)
        {
            boxController.enabled = true;
            boxController.ResetRotation();
        }
        
        // Restart ambient sound
        if (waterAmbientSound != null && audioSource != null)
        {
            audioSource.clip = waterAmbientSound;
            audioSource.loop = true;
            audioSource.Play();
        }
        
        UpdateUI();
    }
    
    public bool IsGameOver()
    {
        return isGameOver;
    }
} 