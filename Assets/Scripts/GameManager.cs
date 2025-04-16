using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Events;

public class GameManager : MonoBehaviour
{
    [Header("Game State")]
    [SerializeField] private bool gameActive = false;
    [SerializeField] private bool gamePaused = false;
    [SerializeField] private float gameTime = 0f;
    [SerializeField] private float gameScore = 0f;
    
    [Header("Game Settings")]
    [SerializeField] private int targetFrameRate = 60;
    [SerializeField] private float startDelay = 3f;
    [SerializeField] private float gameOverDelay = 2f;
    
    [Header("References")]
    [SerializeField] private ObstacleManager obstacleManager;
    [SerializeField] private BoatHealth boatHealth;
    [SerializeField] private Transform boxTransform;
    
    [Header("UI References")]
    [SerializeField] private GameObject mainMenuUI;
    [SerializeField] private GameObject gameUI;
    [SerializeField] private GameObject pauseMenuUI;
    [SerializeField] private GameObject gameOverUI;
    
    [Header("Events")]
    public UnityEvent OnGameStart;
    public UnityEvent OnGameOver;
    public UnityEvent OnGamePaused;
    public UnityEvent OnGameResumed;
    public UnityEvent<float> OnScoreChanged;
    
    // Singleton pattern
    public static GameManager Instance { get; private set; }
    
    // Properties
    public bool IsGameActive => gameActive;
    public bool IsGamePaused => gamePaused;
    public float GameTime => gameTime;
    public float GameScore => gameScore;
    
    // Public methods for other scripts to check game state
    public bool IsGameOver()
    {
        return !gameActive;
    }
    
    public float GetCurrentScore()
    {
        return gameScore;
    }
    
    private void Awake()
    {
        // Singleton setup
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
        
        // Set application target frame rate
        Application.targetFrameRate = targetFrameRate;
    }
    
    private void Start()
    {
        // Find references if not assigned
        if (obstacleManager == null)
            obstacleManager = FindObjectOfType<ObstacleManager>();
            
        if (boatHealth == null)
            boatHealth = FindObjectOfType<BoatHealth>();
            
        // Initialize UI state
        ShowMainMenu();
    }
    
    private void Update()
    {
        // Update game timer when active
        if (gameActive && !gamePaused)
        {
            gameTime += Time.deltaTime;
            
            // Update score based on time (can be modified for more complex scoring)
            UpdateScore(gameTime);
        }
        
        // Handle pause input
        if (Input.GetKeyDown(KeyCode.Escape) && gameActive)
        {
            TogglePause();
        }
    }
    
    public void StartGame()
    {
        StartCoroutine(StartGameRoutine());
    }
    
    private IEnumerator StartGameRoutine()
    {
        // Reset game state
        gameTime = 0f;
        gameScore = 0f;
        
        // Show game UI
        ShowGameUI();
        
        // Reset boat health
        if (boatHealth != null)
        {
            boatHealth.ResetHealth();
        }
        
        // Wait for start delay
        yield return new WaitForSeconds(startDelay);
        
        // Activate game
        gameActive = true;
        
        // Start obstacle spawning
        if (obstacleManager != null)
        {
            obstacleManager.StartSpawning();
        }
        
        // Invoke game start event
        OnGameStart?.Invoke();
    }
    
    public void GameOver()
    {
        StartCoroutine(GameOverRoutine());
    }
    
    private IEnumerator GameOverRoutine()
    {
        // Stop game activity
        gameActive = false;
        
        // Stop obstacle spawning
        if (obstacleManager != null)
        {
            obstacleManager.StopSpawning();
        }
        
        // Wait for delay before showing game over UI
        yield return new WaitForSeconds(gameOverDelay);
        
        // Show game over UI
        ShowGameOverUI();
        
        // Invoke game over event
        OnGameOver?.Invoke();
    }
    
    public void RestartGame()
    {
        // Reload current scene
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
    
    public void QuitGame()
    {
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #else
        Application.Quit();
        #endif
    }
    
    public void TogglePause()
    {
        if (gamePaused)
            ResumeGame();
        else
            PauseGame();
    }
    
    public void PauseGame()
    {
        gamePaused = true;
        Time.timeScale = 0f;
        
        // Show pause menu
        ShowPauseMenu();
        
        // Invoke pause event
        OnGamePaused?.Invoke();
    }
    
    public void ResumeGame()
    {
        gamePaused = false;
        Time.timeScale = 1f;
        
        // Show game UI
        ShowGameUI();
        
        // Invoke resume event
        OnGameResumed?.Invoke();
    }
    
    public void AddScore(float points)
    {
        if (!gameActive)
            return;
            
        gameScore += points;
        OnScoreChanged?.Invoke(gameScore);
    }
    
    private void UpdateScore(float currentTime)
    {
        // Simple scoring based on survival time
        float newScore = Mathf.Floor(currentTime * 10);
        
        if (newScore != gameScore)
        {
            gameScore = newScore;
            OnScoreChanged?.Invoke(gameScore);
        }
    }
    
    // UI Management
    private void ShowMainMenu()
    {
        if (mainMenuUI != null) mainMenuUI.SetActive(true);
        if (gameUI != null) gameUI.SetActive(false);
        if (pauseMenuUI != null) pauseMenuUI.SetActive(false);
        if (gameOverUI != null) gameOverUI.SetActive(false);
    }
    
    private void ShowGameUI()
    {
        if (mainMenuUI != null) mainMenuUI.SetActive(false);
        if (gameUI != null) gameUI.SetActive(true);
        if (pauseMenuUI != null) pauseMenuUI.SetActive(false);
        if (gameOverUI != null) gameOverUI.SetActive(false);
    }
    
    private void ShowPauseMenu()
    {
        if (pauseMenuUI != null) pauseMenuUI.SetActive(true);
        if (gameUI != null) gameUI.SetActive(false);
    }
    
    private void ShowGameOverUI()
    {
        if (mainMenuUI != null) mainMenuUI.SetActive(false);
        if (gameUI != null) gameUI.SetActive(false);
        if (pauseMenuUI != null) pauseMenuUI.SetActive(false);
        if (gameOverUI != null) gameOverUI.SetActive(true);
    }
} 