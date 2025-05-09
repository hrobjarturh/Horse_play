using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit; // Ensure this namespace is present

public class FireBulletOnActivate : MonoBehaviour
{
    public GameObject bullet;
    public Transform firePoint;
    public float bulletSpeed = 20f;

    // --- MODIFICATION START ---
    [Header("Sound Effects")] // Optional: to organize in Inspector
    [SerializeField] private AudioClip gunshotSound; // Assign your gunshot sound clip in the Inspector
    private AudioSource audioSource;
    // --- MODIFICATION END ---

    private UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable grabInteractable;

    void Awake()
    {
        grabInteractable = GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();
        grabInteractable.activated.AddListener(FireBullet);

        // --- MODIFICATION START ---
        // Get or add an AudioSource component
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            // If no AudioSource exists on this GameObject, add one
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        // Optional: Configure AudioSource settings if needed (e.g., spatialBlend for 3D sound)
        // audioSource.spatialBlend = 1.0f; // Makes sound 3D
        // audioSource.playOnAwake = false; // We'll trigger it manually
        // --- MODIFICATION END ---
    }

    void FireBullet(ActivateEventArgs args)
    {
        GameObject spawnedBullet = Instantiate(bullet, firePoint.position, firePoint.rotation);
        spawnedBullet.GetComponent<Rigidbody>().linearVelocity = firePoint.forward * bulletSpeed;
        Destroy(spawnedBullet, 5f);

        // --- MODIFICATION START ---
        // Play the gunshot sound
        if (gunshotSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(gunshotSound);
        }
        else if (gunshotSound == null)
        {
            Debug.LogWarning("Gunshot sound clip not assigned in FireBulletOnActivate.");
        }
        else // audioSource is null, though Awake should have handled it
        {
            Debug.LogWarning("AudioSource component not found on the gun.");
        }
        // --- MODIFICATION END ---
    }
}