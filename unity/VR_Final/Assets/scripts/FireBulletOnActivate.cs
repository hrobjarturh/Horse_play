using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class FireBulletOnActivate : MonoBehaviour
{
    public GameObject bullet;
    public Transform firePoint;
    public float bulletSpeed = 20f;
    public float shotDelay = 10f;

    private XRGrabInteractable grabInteractable;
    private bool gunPickedUpForFirstTime = false;
    private float nextShotTime = 0f;
    private int shotCount = 0;

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
        if (Time.time < nextShotTime) return;

        GameObject spawnedBullet = Instantiate(bullet, firePoint.position, firePoint.rotation);
        spawnedBullet.GetComponent<Rigidbody>().linearVelocity = firePoint.forward * bulletSpeed;
        Destroy(spawnedBullet, 3f);

        shotCount++;
        nextShotTime = Time.time + shotDelay;

        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlayGunshotSound(firePoint.position);

            if (shotCount % 6 == 0)
            {
                SoundManager.Instance.PlayReloadSound(firePoint.position);
                nextShotTime = 3f;
            }
        }
        else
        {
            Debug.LogWarning("FireBulletOnActivate: SoundManager.Instance not found. Cannot play sounds.");
        }
    }
}
