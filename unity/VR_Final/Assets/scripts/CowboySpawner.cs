using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class CowboySpawner : MonoBehaviour
{
    [SerializeField] private GameObject cowboyPrefab;

    [Header("Spawn Points")]
    [SerializeField] private List<Transform> spawnPoints = new List<Transform>();

    [Header("Spawn Area Radius (around selected spawn point)")]
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

        if (spawnPoints == null || spawnPoints.Count == 0)
        {
            Debug.LogWarning("SpawnPoints list is empty in CowboySpawner. Using this GameObject's position as the only spawn point.", this);
            if (spawnPoints == null) spawnPoints = new List<Transform>();
            spawnPoints.Add(this.transform);
        }


        if (GameManager.Instance != null)
        {
            if (GameManager.HasInitialGunBeenPickedUp()) // Check if gun was ALREADY picked up
            {
                HandleGunPickedUpEvent();
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
            GameManager.OnGunPickedUpToStart -= HandleGunPickedUpEvent;
        }
        StopAllCoroutines(); // Stop any pending spawns
    }

    private void HandleGunPickedUpEvent()
    {
        if (initialSpawnDone || !this.enabled) return;

        initialSpawnDone = true;
        
        // Get initial max cowboys from GameManager instead of fixed 8
        int initialSpawnCount = Mathf.Min(GameManager.Instance.CurrentMaxCowboys, 8); // Still maybe cap initial for flow? Or just use GM value directly?
        StartCoroutine(SpawnInitialCowboys(initialSpawnCount));


        if (GameManager.Instance != null)
        {
             GameManager.OnGunPickedUpToStart -= HandleGunPickedUpEvent;
        }
    }

    private IEnumerator SpawnInitialCowboys(int count)
    {
        // Debug.Log($"CowboySpawner: Starting SpawnInitialCowboys coroutine for {count} cowboys.");
        for (int i = 0; i < count; i++)
        {
            // Check if spawner disabled, game over, or max reached (using GameManager's current max)
            if (!this.enabled || !GameManager.IsGameEffectivelyStarted || activeCowboys.Count >= GameManager.Instance.CurrentMaxCowboys) break; 
            
            SpawnCowboy();
            // Use GameManager's initial delay values
            yield return new WaitForSeconds(Random.Range(GameManager.Instance.CurrentMinSpawnDelay, GameManager.Instance.CurrentMaxSpawnDelay)); 
        }
    }


    void SpawnCowboy()
    {
        if (!this.enabled || !GameManager.IsGameEffectivelyStarted || activeCowboys.Count >= GameManager.Instance.CurrentMaxCowboys)
        {
            return;
        }

        if (spawnPoints == null || spawnPoints.Count == 0)
        {
             Debug.LogError("CowboySpawner: No valid spawn points available!");
             return;
        }
        Transform selectedSpawnPoint = spawnPoints[Random.Range(0, spawnPoints.Count)];


        Vector3 potentialSpawnPosition = Vector3.zero;
        bool foundClearSpot = false;

        for (int attempt = 0; attempt < maxSpawnAttempts; attempt++)
        {
            Vector3 randomOffset = new Vector3(
                Random.Range(-spawnRadiusX, spawnRadiusX),
                0f, // Assuming spawn is on a flat plane relative to spawnPoint's y
                Random.Range(-spawnRadiusZ, spawnRadiusZ)
            );
            potentialSpawnPosition = selectedSpawnPoint.position + randomOffset;

            Vector3 checkCenter = potentialSpawnPosition + (Vector3.up * spawnCheckHeightOffset);

            if (!Physics.CheckSphere(checkCenter, spawnCheckRadius, obstacleLayer, QueryTriggerInteraction.Ignore))
            {
                foundClearSpot = true;
                break;
            }
        }

        if (!foundClearSpot)
        {
            // Debug.LogWarning($"CowboySpawner: Could not find a clear spawn position near {selectedSpawnPoint.name} after {maxSpawnAttempts} attempts.", this);
            return;
        }

        GameObject cowboyInstance = Instantiate(cowboyPrefab, potentialSpawnPosition, selectedSpawnPoint.rotation); // Instantiate using selected point's rotation

        Cowboy cowboyScript = cowboyInstance.GetComponent<Cowboy>();
        if (cowboyScript != null)
        {
            cowboyScript.spawner = this;
            if (GameManager.Instance != null)
            {
                 cowboyScript.SetSpeedMultiplier(GameManager.Instance.CurrentCowboySpeedMultiplier);
            }
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
                 // Destroy(cowboyGameObject);
            }
            return;
        }

        bool removed = activeCowboys.Remove(cowboyGameObject);
        if (removed)
        {
            // Debug.Log($"Cowboy {cowboyGameObject.name} died and removed from active list. Active count: {activeCowboys.Count}");
            Destroy(cowboyGameObject); 
            
            if (GameManager.IsGameEffectivelyStarted && activeCowboys.Count < GameManager.Instance.CurrentMaxCowboys && this.enabled)
            {
                StartCoroutine(DelayedSpawn(Random.Range(GameManager.Instance.CurrentMinSpawnDelay, GameManager.Instance.CurrentMaxSpawnDelay)));
            }
        }
    }

    IEnumerator DelayedSpawn(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (this.enabled && GameManager.IsGameEffectivelyStarted && activeCowboys.Count < GameManager.Instance.CurrentMaxCowboys)
        {
            SpawnCowboy();
        }
    }
}