// GameManager.cs
using UnityEngine;
using System;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    // Make currentGameState accessible for SoundManager's Start logic if needed,
    // but helper properties are cleaner.
    // internal GameState CurrentInternalState => currentGameState; 

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

    // Helper properties for SoundManager
    public bool IsCurrentlyPreGame => currentGameState == GameState.PreGame;
    public bool IsCurrentlyGameOver => currentGameState == GameState.GameOver;


    public static event Action OnGunPickedUpToStart; // Fired when gun is picked up, signals transition from PreGame
    public static event Action OnGamePreStart;       // Fired when game is initialized (typically at Awake)
    public static event Action OnGameActuallyStarted; // Fired when game transitions to Starting state (after gun pickup)
    public static event Action OnPlayerDied;         // Fired when game over condition is met

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            // DontDestroyOnLoad(gameObject); // Only if GameManager needs to persist across scenes
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
        OnGamePreStart?.Invoke();
        Debug.Log("GameManager: Initialized to PreGame state.");
    }

    public void PlayerPickedUpGun()
    {
        if (currentGameState != GameState.PreGame) return;

        currentGameState = GameState.Starting;
        Debug.Log("GameManager: Gun picked up! Game Starting.");
        OnGunPickedUpToStart?.Invoke(); // For systems that react specifically to the gun pickup moment
        OnGameActuallyStarted?.Invoke(); // For systems that react to the game "beginning" more broadly
        
        // UIManager handles a 4s delay for instructions, then calls TransitionToPlayingState.
        // So, no direct call to TransitionToPlayingState here.
    }
    
    // Called by UIManager after its initial instruction panel is shown
    public void TransitionToPlayingState()
    {
        if(currentGameState == GameState.Starting) 
        {
            currentGameState = GameState.Playing;
            Debug.Log("GameManager: Transitioned to Playing state.");
        }
        else
        {
            Debug.LogWarning($"GameManager: Tried to transition to Playing state from {currentGameState}, expected Starting.");
        }
    }

    public void HandlePlayerReached()
    {
        if (currentGameState == GameState.Playing || currentGameState == GameState.Starting)
        {
            currentGameState = GameState.GameOver;
            Debug.Log("GameManager: Player reached by cowboy! Triggering Game Over sequence.");
            OnPlayerDied?.Invoke(); 
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
        // currentGameState = GameState.PreGame; // This will be handled by new scene's GameManager Awake
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        // OnGamePreStart will be invoked by the new GameManager instance in the reloaded scene.
    }

    public static bool HasInitialGunBeenPickedUp()
    {
        // True if game is in Starting, Playing, or GameOver states
        return Instance != null && Instance.currentGameState != GameState.PreGame;
    }
}