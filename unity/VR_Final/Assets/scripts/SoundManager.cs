// SoundManager.cs
using UnityEngine;
using System.Linq; // Required for FirstOrDefault

public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance { get; private set; }

    [Header("Theme Music")]
    [SerializeField] private AudioSource themeMusicAudioSource;
    [SerializeField] private AudioClip themeMusicClip;
    [SerializeField] [Range(0f, 1f)] private float defaultThemeVolume = 0.5f;
    [SerializeField] [Range(0f, 1f)] private float gameOverThemeVolume = 0.1f;


    [Header("Sound Effects")]
    [SerializeField] private AudioClip gunshotSoundClip;
    [SerializeField] private AudioClip gunCockingSoundClip;
    [SerializeField] private AudioClip cowboyDeathSoundClip;
    [SerializeField] private AudioClip gunReloadSoundClip;
    [SerializeField] [Range(0f, 1f)] private float sfxVolume = 0.8f; // Global SFX volume

    void Awake()
    {
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

        EnsureThemeAudioSourceSetup();
    }

    void Start()
    {
        // Stop music initially to ensure it only starts on the correct event
        if (themeMusicAudioSource != null && themeMusicAudioSource.isPlaying)
        {
            themeMusicAudioSource.Stop();
        }

        if (GameManager.Instance != null)
        {
            SubscribeToGameEvents();

            // If SoundManager starts *after* the game has already started or ended,
            // catch up to the correct music state.
            if (GameManager.Instance.IsCurrentlyGameOver)
            {
                // Game is over, music should be at game over volume (might be playing or stopped)
                HandlePlayerDied();
                // If it was supposed to be playing but isn't (e.g. after scene reload and SoundManager Start),
                // and game was already started before game over.
                if(GameManager.HasInitialGunBeenPickedUp()){ // Ensures gun was picked up before game over
                     PlayThemeMusic(gameOverThemeVolume);
                }
            }
            else if (GameManager.IsGameEffectivelyStarted) // Game has started (gun picked up) but not game over
            {
                // Game has already started, so theme music should be playing.
                PlayThemeMusic(defaultThemeVolume);
            }
            // If IsCurrentlyPreGame, music should NOT be playing yet.
            // It will be triggered by HandleGameActuallyStarted.
        }
        else
        {
            // If GameManager is not ready, music won't play yet.
            // It will be handled by event subscriptions once GameManager is available.
            Debug.LogWarning("SoundManager: GameManager.Instance not found in Start. Theme music will await game events.");
        }
    }

    void OnDestroy()
    {
        UnsubscribeFromGameEvents();
    }

    private void SubscribeToGameEvents()
    {
        GameManager.OnGamePreStart += HandleGamePreStart; // Still useful for stopping music on restart
        GameManager.OnGameActuallyStarted += HandleGameActuallyStarted; // THIS will start the theme
        GameManager.OnPlayerDied += HandlePlayerDied;
    }

    private void UnsubscribeFromGameEvents()
    {
        if (GameManager.Instance != null)
        {
            GameManager.OnGamePreStart -= HandleGamePreStart;
            GameManager.OnGameActuallyStarted -= HandleGameActuallyStarted;
            GameManager.OnPlayerDied -= HandlePlayerDied;
        }
    }

    private void EnsureThemeAudioSourceSetup()
    {
        if (themeMusicAudioSource == null)
        {
            var existingSources = GetComponents<AudioSource>();
            if (existingSources.Length > 0)
            {
                themeMusicAudioSource = existingSources.FirstOrDefault(s => !s.playOnAwake || s.clip == null || s.clip == themeMusicClip);
                if (themeMusicAudioSource == null) themeMusicAudioSource = existingSources[0];
                Debug.LogWarning("SoundManager: ThemeMusicAudioSource not assigned in Inspector. Using an existing AudioSource on this GameObject.");
            }
            
            if (themeMusicAudioSource == null)
            {
                themeMusicAudioSource = gameObject.AddComponent<AudioSource>();
                Debug.LogWarning("SoundManager: ThemeMusicAudioSource not assigned and no existing one found. Added a new AudioSource component.");
            }
        }

        themeMusicAudioSource.loop = true;
        themeMusicAudioSource.playOnAwake = false; // Ensure it doesn't play automatically
        if (themeMusicClip != null)
        {
            if (themeMusicAudioSource.clip != themeMusicClip)
            {
                themeMusicAudioSource.clip = themeMusicClip;
            }
        }
        else
        {
            Debug.LogError("SoundManager: ThemeMusicClip not assigned in Inspector!", this);
        }
    }

    private void HandleGamePreStart()
    {
        // This event signifies the game is resetting (e.g., after a restart).
        // We should ensure the theme music is stopped here, as it should only start
        // when the gun is picked up via OnGameActuallyStarted.
        Debug.Log("SoundManager: GamePreStart event received. Stopping theme music if playing.");
        StopThemeMusic();
    }
    
    private void HandleGameActuallyStarted()
    {
        // This is when the gun is picked up. START the theme music here.
        Debug.Log("SoundManager: GameActuallyStarted event received. Playing theme music.");
        PlayThemeMusic(defaultThemeVolume);
    }

    private void HandlePlayerDied()
    {
        Debug.Log("SoundManager: PlayerDied event received. Adjusting theme volume for game over.");
        if (themeMusicAudioSource != null)
        {
            // If music wasn't playing because game over happened before gun pickup somehow,
            // don't start it now. Only adjust volume if it was supposed to be playing.
            if (themeMusicAudioSource.isPlaying)
            {
                themeMusicAudioSource.volume = gameOverThemeVolume;
            }
            else if (GameManager.HasInitialGunBeenPickedUp())
            {
                // If game had started (gun picked up) but music somehow stopped (unlikely with loop),
                // play it at game over volume.
                PlayThemeMusic(gameOverThemeVolume);
            }

            // If volume is 0, you might consider pausing or stopping fully:
            // if (gameOverThemeVolume <= 0.001f && themeMusicAudioSource.isPlaying)
            // {
            //     themeMusicAudioSource.Pause(); // or StopThemeMusic();
            // }
        }
    }

    public void PlayThemeMusic(float volume)
    {
        if (themeMusicAudioSource != null && themeMusicClip != null)
        {
            themeMusicAudioSource.volume = volume;
            if (!themeMusicAudioSource.isPlaying)
            {
                themeMusicAudioSource.Play();
            }
        }
        else if (themeMusicClip == null)
        {
            Debug.LogError("SoundManager: Cannot play theme music - ThemeMusicClip is not assigned!", this);
        }
        else
        {
            Debug.LogError("SoundManager: Cannot play theme music - ThemeMusicAudioSource is not available.", this);
        }
    }

    public void StopThemeMusic()
    {
        if (themeMusicAudioSource != null && themeMusicAudioSource.isPlaying)
        {
            themeMusicAudioSource.Stop();
        }
    }

    private void PlaySFXAtPoint(AudioClip clip, Vector3 position, float volume)
    {
        if (clip != null)
        {
            AudioSource.PlayClipAtPoint(clip, position, volume);
        }
    }

    public void PlayGunshotSound(Vector3 position)
    {
        if (gunshotSoundClip == null) Debug.LogWarning("SoundManager: GunshotSoundClip not assigned in Inspector. Cannot play gunshot sound.", this);
        PlaySFXAtPoint(gunshotSoundClip, position, 1.5f * sfxVolume);
        PlaySFXAtPoint(gunCockingSoundClip, position, sfxVolume);
    }

    public void PlayCowboyDeathSound(Vector3 position)
    {
        if (cowboyDeathSoundClip == null) Debug.LogWarning("SoundManager: CowboyDeathSoundClip not assigned in Inspector. Cannot play cowboy death sound.", this);
        PlaySFXAtPoint(cowboyDeathSoundClip, position, 1.5f * sfxVolume);
    }

    public void PlayReloadSound(Vector3 position){
        if (gunReloadSoundClip == null) Debug.LogWarning("SoundManager: gunReloadSoundClip not assigned in Inspector. Cannot play reload sound.", this);
        PlaySFXAtPoint(gunReloadSoundClip, position, sfxVolume);
    }
}