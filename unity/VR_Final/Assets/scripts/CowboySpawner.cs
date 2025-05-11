using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class CowboySpawner : MonoBehaviour
{
    [SerializeField] private GameObject cowboyPrefab; // THIS MUST BE A PREFAB FROM YOUR PROJECT ASSETS
    [SerializeField] private Transform spawnPoint;
    [SerializeField] private int maxCowboys = 8;
    [SerializeField] private float spawnRadiusX = 5f;
    [SerializeField] private float spawnRadiusZ = 5f;

    [Header("Spawning Collision Check")]
    [SerializeField] private LayerMask obstacleLayer;
    [SerializeField] private float spawnCheckRadius = 0.5f;
    [Tooltip("How high above the potential spawn point's base to perform the CheckSphere.")]
    [SerializeField] private float spawnCheckHeightOffset = 0.5f;
    [SerializeField] private int maxSpawnAttempts = 10;

    private List<GameObject> activeCowboys = new List<GameObject>();
    private bool initialSpawnDone = false;

    void Start()
    {
        if (cowboyPrefab == null)
        {
            Debug.LogError("CowboyPrefab not assigned in CowboySpawner! Assign a prefab from Project Assets.", this);
            enabled = false;
            return;
        }

        // A small check to see if it *might* be a scene object (not foolproof)
        if (cowboyPrefab.scene.IsValid()) {
            Debug.LogWarningFormat(this, "CowboyPrefab '{0}' in CowboySpawner appears to be a scene object, not a Project Asset. This can cause issues when the original is modified (e.g., ragdolled). Please assign a prefab from your Project window.", cowboyPrefab.name);
        }


        if (spawnPoint == null)
        {
            // Debug.LogWarning("SpawnPoint not assigned in CowboySpawner. Using Spawner's position.", this);
            spawnPoint = this.transform;
        }

        if (GameManager.Instance != null)
        {
            if (GameManager.HasInitialGunBeenPickedUp()) // Check if gun was ALREADY picked up
            {
                HandleGunPickedUpEvent(); // Renamed for clarity
            }
            else
            {
                GameManager.OnGunPickedUpToStart += HandleGunPickedUpEvent;
            }
        }
        else
        {
            Debug.LogError("GameManager Instance not found. CowboySpawner will not function correctly.", this);
            enabled = false;
        }
    }

    void OnDestroy()
    {
        if (GameManager.Instance != null)
        {
            // --- MODIFICATION ---
            GameManager.OnGunPickedUpToStart -= HandleGunPickedUpEvent;
            // --- END MODIFICATION ---
        }
        StopAllCoroutines(); // Stop any pending spawns
    }

    // --- MODIFICATION: Renamed for clarity ---
    private void HandleGunPickedUpEvent()
    {
        if (initialSpawnDone || !this.enabled) return;
        // Debug.Log("CowboySpawner: Game Started signal received. Initiating cowboy spawn.");
        initialSpawnDone = true;
        StartCoroutine(SpawnInitialCowboys());
        
        // --- MODIFICATION: Unsubscribe after handling ---
        if (GameManager.Instance != null)
        {
             GameManager.OnGunPickedUpToStart -= HandleGunPickedUpEvent;
        }
        // --- END MODIFICATION ---
    }

    private IEnumerator SpawnInitialCowboys()
    {
        // Debug.Log("CowboySpawner: Starting SpawnInitialCowboys coroutine.");
        int initialSpawnCount = Mathf.Min(maxCowboys, 8);
        for (int i = 0; i < initialSpawnCount; i++)
        {
            if (!this.enabled || activeCowboys.Count >= maxCowboys) break; // Stop if spawner disabled or max reached
            
            SpawnCowboy();
            yield return new WaitForSeconds(Random.Range(1.0f, 2.5f)); 
        }
    }

    void SpawnCowboy()
    {
        // --- MODIFICATION ---
        if (!this.enabled || !GameManager.HasInitialGunBeenPickedUp() || activeCowboys.Count >= maxCowboys)
        // --- OLD ---
        // if (!this.enabled || !GameManager.IsGameStarted || activeCowboys.Count >= maxCowboys)
        {
            return;
        }

        Vector3 potentialSpawnPosition = Vector3.zero;
        bool foundClearSpot = false;

        for (int attempt = 0; attempt < maxSpawnAttempts; attempt++)
        {
            Vector3 randomOffset = new Vector3(
                Random.Range(-spawnRadiusX, spawnRadiusX),
                0f, // Assuming spawn is on a flat plane relative to spawnPoint's y
                Random.Range(-spawnRadiusZ, spawnRadiusZ)
            );
            potentialSpawnPosition = spawnPoint.position + randomOffset;
            Vector3 checkCenter = potentialSpawnPosition + (Vector3.up * spawnCheckHeightOffset);

            if (!Physics.CheckSphere(checkCenter, spawnCheckRadius, obstacleLayer, QueryTriggerInteraction.Ignore))
            {
                foundClearSpot = true;
                break;
            }
        }

        if (!foundClearSpot)
        {
            // Debug.LogWarning($"CowboySpawner: Could not find a clear spawn position near {spawnPoint.name} after {maxSpawnAttempts} attempts.", this);
            return;
        }

        GameObject cowboyInstance = Instantiate(cowboyPrefab, potentialSpawnPosition, spawnPoint.rotation);
        // cowboyInstance.name = cowboyPrefab.name + "_" + (activeCowboys.Count + 1); // Optional: for easier debugging

        Cowboy cowboyScript = cowboyInstance.GetComponent<Cowboy>();
        if (cowboyScript != null)
        {
            cowboyScript.spawner = this;
            activeCowboys.Add(cowboyInstance);
            // Debug.Log($"Cowboy spawned: {cowboyInstance.name}. Active cowboys: {activeCowboys.Count}");
        }
        else
        {
            Debug.LogError($"Spawned cowboy prefab {cowboyPrefab.name} is missing the Cowboy script! Destroying instance.", cowboyInstance);
            Destroy(cowboyInstance);
        }
    }

    public void OnCowboyDied(GameObject cowboyGameObject) // Changed parameter to GameObject for clarity
    {
        if (cowboyGameObject == null) return;

        // It's possible the cowboy was already destroyed or removed if multiple death signals occurred
        if (!activeCowboys.Contains(cowboyGameObject))
        {
            // Debug.LogWarning($"Cowboy {cowboyGameObject.name} reported dead but not found in active list. It might have already been processed or destroyed.", this);
            if (cowboyGameObject != null && cowboyGameObject.scene.IsValid()) // Check if it's a valid scene object still
            {
                 // Destroy(cowboyGameObject); // Destroy it if it's orphaned
            }
            return;
        }

        bool removed = activeCowboys.Remove(cowboyGameObject);
        if (removed)
        {
            // Debug.Log($"Cowboy {cowboyGameObject.name} died and removed from active list. Active count: {activeCowboys.Count}");
            // The Cowboy script's OnDestroy will handle destroying its hat.
            // The Cowboy GameObject itself is destroyed here or by its own NotifyDeath if no spawner.
            // Let's ensure it's destroyed here since the spawner is notified.
            Destroy(cowboyGameObject); 
            
            if (GameManager.IsGameEffectivelyStarted && activeCowboys.Count < maxCowboys && this.enabled)
            // --- OLD ---
            // if (GameManager.IsGameStarted && activeCowboys.Count < maxCowboys && this.enabled)
            {
                StartCoroutine(DelayedSpawn(Random.Range(2.0f, 4.0f)));
            }
        }
        // No 'else' needed here as the Contains check above handles the "not in list" case.
    }

    IEnumerator DelayedSpawn(float delay)
    {
        yield return new WaitForSeconds(delay);
        // --- MODIFICATION ---
        if (this.enabled && GameManager.IsGameEffectivelyStarted && activeCowboys.Count < maxCowboys)
        {
            SpawnCowboy();
        }
    }
}