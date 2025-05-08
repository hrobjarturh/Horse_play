using UnityEngine;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Tooltip("Drag your DeathScreen Panel here")]
    [SerializeField] private GameObject deathScreen;

    void Awake()
    {
        // Simple singleton
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    /// <summary>
    /// Call this to show the YOU ARE DEAD screen.
    /// </summary>
    public void ShowDeathScreen()
    {
        deathScreen.SetActive(true);
        // optional: pause the game
        Time.timeScale = 0f;
    }
}
