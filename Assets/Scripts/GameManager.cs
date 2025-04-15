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
    public GameObject gameOverPanel;
    public Button restartButton;
    
    [Header("Audio")]
    public AudioClip waterAmbientSound;
    public AudioClip balanceWarningSound;
    public AudioClip gameOverSound;
    
    // Private fields
    private float currentScore = 0f;
    private float currentDifficulty = 1f;
    private float gameTimer = 0f;
    private bool isGameOver = false;
    private AudioSource audioSource;
    private bool isBalanceWarning = false;
    
    // Components
    private GazingBoxController boxController;
    private BoatBuoyancy boatBuoyancy;
    
    private void Start()
    {
        // Get references to components
        boxController = gazingBox.GetComponent<GazingBoxController>();
        boatBuoyancy = boat.GetComponent<BoatBuoyancy>();
        
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
        
        UpdateUI();
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
        if (boat == null)
            return;
            
        // Calculate the boat's current tilt angle from the upright position
        float tiltAngle = Vector3.Angle(Vector3.up, boat.transform.up);
        
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
        
        // Game over if the boat tilts too far
        if (tiltAngle > gameOverTiltThreshold)
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
        
        if (balanceText != null)
        {
            float tiltAngle = Vector3.Angle(Vector3.up, boat.transform.up);
            balanceText.text = "Balance: " + Mathf.FloorToInt(tiltAngle).ToString() + "°";
            
            // Change color based on tilt
            if (tiltAngle > balanceThreshold)
            {
                balanceText.color = Color.red;
            }
            else
            {
                balanceText.color = Color.white;
            }
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