using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class FireBulletOnActivate : MonoBehaviour
{
    public GameObject bullet;
    public Transform firePoint;
    public float bulletSpeed = 20f;

    // REMOVED: Header("Sound Effects"), gunshotSound AudioClip, and local audioSource
    // Sound will be played via SoundManager

    private XRGrabInteractable grabInteractable;
    private bool gunPickedUpForFirstTime = false;

    void Awake()
    {
        grabInteractable = GetComponent<XRGrabInteractable>();
        if (grabInteractable == null)
        {
            Debug.LogError("XRGrabInteractable not found on this GameObject.", this);
            enabled = false;
            return;
        }
        grabInteractable.activated.AddListener(FireBullet);
        grabInteractable.selectEntered.AddListener(OnGunPickedUp);

        // REMOVED: Local AudioSource setup
        // audioSource = GetComponent<AudioSource>();
        // ...
    }

    private void OnGunPickedUp(SelectEnterEventArgs args)
    {
        if (!gunPickedUpForFirstTime)
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.PlayerPickedUpGun();
                gunPickedUpForFirstTime = true;
                Debug.Log("Gun picked up by player, signaling Game Manager to start the game.");
            }
            else
            {
                Debug.LogError("GameManager Instance not found. Cannot start game via gun pickup.");
            }
        }
    }
    
    void OnDestroy()
    {
        if (grabInteractable != null)
        {
            grabInteractable.activated.RemoveListener(FireBullet);
            grabInteractable.selectEntered.RemoveListener(OnGunPickedUp);
        }
    }

    void FireBullet(ActivateEventArgs args)
    {
        // Optionally check game state:
        // if (!GameManager.IsGamePlaying) return; // Or IsGameEffectivelyStarted

        GameObject spawnedBullet = Instantiate(bullet, firePoint.position, firePoint.rotation);
        spawnedBullet.GetComponent<Rigidbody>().linearVelocity = firePoint.forward * bulletSpeed;
        Destroy(spawnedBullet, 5f);

        // Play gunshot sound via SoundManager
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlayGunshotSound(firePoint.position);
        }
        else
        {
            Debug.LogWarning("FireBulletOnActivate: SoundManager.Instance not found. Cannot play gunshot sound.");
        }
    }
}