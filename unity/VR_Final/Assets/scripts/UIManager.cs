// UIManager.cs
using UnityEngine;
using UnityEngine.UI;
using TMPro; // Make sure this is present for TextMeshPro
using System.Collections;
using UnityEngine.SceneManagement;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("UI Panels")]
    [SerializeField] private GameObject preGameInstructionsPanel;
    [SerializeField] private GameObject gameStartInstructionsPanel;
    [SerializeField] private GameObject deathScreenPanel;

    [Header("Buttons")]
    [SerializeField] private Button restartButton;

    // --- NEW ADDITION FOR COWBOYS SHOT COUNTER ---
    [Header("Gameplay UI")]
    [SerializeField] private TextMeshProUGUI cowboysShotText; // Assign this in the Inspector
    private int cowboysShotCount = 0;
    // --- END NEW ADDITION ---

    public bool IsDeathScreenActive()
    {
        if (deathScreenPanel == null)
        {
            return false;
        }
        return deathScreenPanel.activeSelf;
    }

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

        // Initial states for panels
        if (preGameInstructionsPanel != null) preGameInstructionsPanel.SetActive(true);
        else Debug.LogError("PreGameInstructionsPanel not assigned in UIManager.", this);

        if (gameStartInstructionsPanel != null) gameStartInstructionsPanel.SetActive(false);
        else Debug.LogError("GameStartInstructionsPanel not assigned in UIManager.", this);

        if (deathScreenPanel != null) deathScreenPanel.SetActive(false);
        else Debug.LogError("DeathScreenPanel not assigned in UIManager.", this);

        if (restartButton != null)
        {
            restartButton.onClick.AddListener(OnRestartButtonClicked);
        }
        else Debug.LogError("RestartButton not assigned in UIManager.", this);

        // --- NEW ADDITION: Initialize Cowboys Shot Text ---
        if (cowboysShotText != null)
        {
            cowboysShotCount = 0;
            cowboysShotText.text = "Cowboys Shot: " + cowboysShotCount;
            cowboysShotText.gameObject.SetActive(true); // Ensure it's active from the start
        }
        else
        {
            Debug.LogError("CowboysShotText not assigned in UIManager. Please assign it in the Inspector.", this);
        }
        // --- END NEW ADDITION ---
    }

    void Start()
    {
        if (GameManager.Instance != null)
        {
            GameManager.OnGamePreStart += ShowPreGameUI;
            GameManager.OnGameActuallyStarted += HandleGameActuallyStarted;
            GameManager.OnPlayerDied += ShowDeathScreenUI;
        }
        else
        {
            Debug.LogError("UIManager: GameManager.Instance is null in Start. Events won't be subscribed.");
        }
    }

    void OnDestroy()
    {
        if (GameManager.Instance != null)
        {
            GameManager.OnGamePreStart -= ShowPreGameUI;
            GameManager.OnGameActuallyStarted -= HandleGameActuallyStarted;
            GameManager.OnPlayerDied -= ShowDeathScreenUI;
        }
        if (restartButton != null)
        {
            restartButton.onClick.RemoveListener(OnRestartButtonClicked);
        }
        StopAllCoroutines();
    }

    private void ShowPreGameUI()
    {
        Debug.Log("UIManager: Showing PreGame UI.");
        if (preGameInstructionsPanel != null) preGameInstructionsPanel.SetActive(true);
        if (gameStartInstructionsPanel != null) gameStartInstructionsPanel.SetActive(false);
        if (deathScreenPanel != null) deathScreenPanel.SetActive(false);
        Time.timeScale = 1f;

        // --- NEW ADDITION: Reset and display cowboys shot count on game (re)start ---
        if (cowboysShotText != null)
        {
            cowboysShotCount = 0;
            cowboysShotText.text = "Cowboys Shot: " + cowboysShotCount;
            cowboysShotText.gameObject.SetActive(true); // Ensure it's visible
        }
        // --- END NEW ADDITION ---
    }

    private void HandleGameActuallyStarted()
    {
        Debug.Log("UIManager: Handling Game Actually Started UI.");
        if (preGameInstructionsPanel != null) preGameInstructionsPanel.SetActive(false);
        StartCoroutine(ShowGameStartInstructionsCoroutine());
    }

    private IEnumerator ShowGameStartInstructionsCoroutine()
    {
        if (gameStartInstructionsPanel != null)
        {
            Debug.Log("UIManager: Showing Game Start Instructions for 4 seconds.");
            gameStartInstructionsPanel.SetActive(true);
            yield return new WaitForSeconds(4f);
            gameStartInstructionsPanel.SetActive(false);
            Debug.Log("UIManager: Hid Game Start Instructions.");
            if (GameManager.Instance != null) {
                GameManager.Instance.TransitionToPlayingState();
            }
        }
    }

    private void ShowDeathScreenUI()
    {
        Debug.Log("UIManager: ShowDeathScreenUI called (event from GameManager).");
        if (preGameInstructionsPanel != null) preGameInstructionsPanel.SetActive(false);
        if (gameStartInstructionsPanel != null) gameStartInstructionsPanel.SetActive(false);

        if (deathScreenPanel != null)
        {
            if (!deathScreenPanel.activeSelf)
            {
                deathScreenPanel.SetActive(true);
                Debug.Log("UIManager: Death Screen Panel activated. Pausing game.");
                Time.timeScale = 0f;
            }
            else
            {
                Debug.Log("UIManager: Death Screen Panel was already active.");
            }
        }
        else
        {
            Debug.LogError("UIManager: Cannot show death screen - DeathScreenPanel is null!");
        }
    }
    
    public void NotifyPlayerReachedByCowboy()
    {
        Debug.Log("UIManager: Notified that player was reached by a cowboy. Relaying to GameManager.");
        if (GameManager.Instance != null)
        {
            GameManager.Instance.HandlePlayerReached();
        }
        else
        {
            Debug.LogError("UIManager: GameManager.Instance is null in NotifyPlayerReachedByCowboy.");
        }
    }

    public void OnRestartButtonClicked()
    {
        Debug.Log("UIManager: Restart Button Clicked. Telling GameManager to restart.");
        if (GameManager.Instance != null)
        {
            GameManager.Instance.RestartGame();
        }
        else
        {
            Debug.LogError("UIManager: GameManager instance not found for restart. Fallback: Reloading scene.");
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }

    // --- NEW PUBLIC METHOD TO INCREMENT COWBOY SHOT COUNT ---
    public void IncrementCowboysShot()
    {
        if (cowboysShotText == null)
        {
            Debug.LogWarning("CowboysShotText is not assigned in UIManager and cannot be updated.");
            return;
        }
        cowboysShotCount++;
        cowboysShotText.text = "Cowboys Shot: " + cowboysShotCount;
    }
    // --- END NEW PUBLIC METHOD ---
}