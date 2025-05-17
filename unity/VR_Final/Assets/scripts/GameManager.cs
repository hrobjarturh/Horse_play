using UnityEngine;
using System;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    private enum GameState
    {
        PreGame,
        Starting, // After gun picked up, before UI message finishes
        Playing,  // After UI message, cowboys fully active
        GameOver
    }

    private GameState currentGameState = GameState.PreGame;
    public static bool IsGameEffectivelyStarted => Instance != null && (Instance.currentGameState == GameState.Starting || Instance.currentGameState == GameState.Playing);
    public static bool IsGamePlaying => Instance != null && Instance.currentGameState == GameState.Playing;

    public bool IsCurrentlyPreGame => currentGameState == GameState.PreGame;
    public bool IsCurrentlyGameOver => currentGameState == GameState.GameOver;

    public static event Action OnGunPickedUpToStart;
    public static event Action OnGamePreStart;
    public static event Action OnGameActuallyStarted;
    public static event Action OnPlayerDied;

    [Header("Difficulty Settings")]
    [SerializeField] private float difficultyIncreaseInterval = 30f; // Time in seconds between difficulty increases
    [SerializeField] private int maxCowboysIncreasePerInterval = 1; // How many more cowboys can spawn per interval
    [SerializeField] private float minSpawnDelayDecreasePerInterval = 0.2f; // Decrease min spawn delay per interval
    [SerializeField] private float maxSpawnDelayDecreasePerInterval = 0.3f; // Decrease max spawn delay per interval
    [SerializeField] private float cowboySpeedMultiplierIncreasePerInterval = 0.1f; // Increase cowboy speed multiplier per interval

    // Add limits to prevent infinite scaling or negative delays/speeds
    [SerializeField] private int maxMaxCowboysLimit = 20; 
    [SerializeField] private float minSpawnDelayLimit = 1.0f;
    [SerializeField] private float maxSpawnDelayLimit = 2.0f;
    [SerializeField] private float maxCowboySpeedMultiplier = 2.0f;

    // Current difficulty values
    private float gameTimer = 0f;
    private int currentDifficultyLevel = 0;

    // Initial values (should match Spawner defaults or be configured here)
    [Header("Initial Spawner Values")]
    [SerializeField] private int initialMaxCowboys = 8;
    [SerializeField] private float initialMinSpawnDelay = 2.0f; // Match CowboySpawner delayed spawn min
    [SerializeField] private float initialMaxSpawnDelay = 4.0f; // Match CowboySpawner delayed spawn max
    [SerializeField] private float initialCowboySpeedMultiplier = 1.0f; // Initial speed is 1x base speed

    public int CurrentMaxCowboys { get; private set; }
    public float CurrentMinSpawnDelay { get; private set; }
    public float CurrentMaxSpawnDelay { get; private set; }
    public float CurrentCowboySpeedMultiplier { get; private set; }


    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
        InitializeGame();
    }

    private void InitializeGame()
    {
        currentGameState = GameState.PreGame;
        Time.timeScale = 1f; // Ensure time scale is reset
        gameTimer = 0f;
        currentDifficultyLevel = 0;

        CurrentMaxCowboys = initialMaxCowboys;
        CurrentMinSpawnDelay = initialMinSpawnDelay;
        CurrentMaxSpawnDelay = initialMaxSpawnDelay;
        CurrentCowboySpeedMultiplier = initialCowboySpeedMultiplier;

        OnGamePreStart?.Invoke();
        Debug.Log("GameManager: Initialized to PreGame state. Difficulty reset.");
    }

    public void PlayerPickedUpGun()
    {
        if (currentGameState != GameState.PreGame) return;

        currentGameState = GameState.Starting;
        Debug.Log("GameManager: Gun picked up! Game Starting.");
        OnGunPickedUpToStart?.Invoke(); 
        OnGameActuallyStarted?.Invoke(); 
    }

    public void TransitionToPlayingState()
    {
        if (currentGameState == GameState.Starting)
        {
            currentGameState = GameState.Playing;
            gameTimer = 0f; 
            Debug.Log("GameManager: Transitioned to Playing state. Timer started.");
        }
        else
        {
            Debug.LogWarning($"GameManager: Tried to transition to Playing state from {currentGameState}, expected Starting.");
        }
    }

    void Update()
    {
        if (currentGameState == GameState.Playing)
        {
            gameTimer += Time.deltaTime;

            // Check if it's time to increase difficulty
            if (gameTimer >= (currentDifficultyLevel + 1) * difficultyIncreaseInterval)
            {
                IncreaseDifficulty();
            }
        }
    }

    private void IncreaseDifficulty()
    {
        currentDifficultyLevel++;
        Debug.Log($"GameManager: Increasing difficulty to Level {currentDifficultyLevel}! Time: {gameTimer:F1}s");

        // Increase Max Cowboys
        CurrentMaxCowboys = Mathf.Min(initialMaxCowboys + (currentDifficultyLevel * maxCowboysIncreasePerInterval), maxMaxCowboysLimit);
        Debug.Log($" - New Max Cowboys: {CurrentMaxCowboys}");

        // Decrease Spawn Delay
        CurrentMinSpawnDelay = Mathf.Max(initialMinSpawnDelay - (currentDifficultyLevel * minSpawnDelayDecreasePerInterval), minSpawnDelayLimit);
        CurrentMaxSpawnDelay = Mathf.Max(initialMaxSpawnDelay - (currentDifficultyLevel * maxSpawnDelayDecreasePerInterval), maxSpawnDelayLimit);
         if (CurrentMinSpawnDelay > CurrentMaxSpawnDelay) 
        {
            float temp = CurrentMinSpawnDelay;
            CurrentMinSpawnDelay = CurrentMaxSpawnDelay;
            CurrentMaxSpawnDelay = temp;
        }
         if (CurrentMinSpawnDelay < minSpawnDelayLimit) CurrentMinSpawnDelay = minSpawnDelayLimit;
         if (CurrentMaxSpawnDelay < minSpawnDelayLimit) CurrentMaxSpawnDelay = minSpawnDelayLimit + 0.5f; // Keep some range

        Debug.Log($" - New Spawn Delay Range: {CurrentMinSpawnDelay:F2}s to {CurrentMaxSpawnDelay:F2}s");

        CurrentCowboySpeedMultiplier = Mathf.Min(initialCowboySpeedMultiplier + (currentDifficultyLevel * cowboySpeedMultiplierIncreasePerInterval), maxCowboySpeedMultiplier);
        Debug.Log($" - New Cowboy Speed Multiplier: {CurrentCowboySpeedMultiplier:F2}");
    }


    public void HandlePlayerReached()
    {
        if (currentGameState == GameState.Playing || currentGameState == GameState.Starting)
        {
            currentGameState = GameState.GameOver;
            Debug.Log("GameManager: Player reached by cowboy! Triggering Game Over sequence.");
            OnPlayerDied?.Invoke();
            gameTimer = 0; // Reset timer
        }
        else if (currentGameState == GameState.GameOver)
        {
            // Debug.Log("GameManager: HandlePlayerReached called, but game is already over.");
        }
        else
        {
            Debug.Log($"GameManager: HandlePlayerReached called in unexpected state: {currentGameState}. No action taken.");
        }
    }

    public void RestartGame()
    {
        Debug.Log("GameManager: Restarting game...");
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public static bool HasInitialGunBeenPickedUp()
    {
        // True if game is in Starting, Playing, or GameOver states
        return Instance != null && Instance.currentGameState != GameState.PreGame;
    }
}