using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class CowboySpawner : MonoBehaviour
{
    [SerializeField] private GameObject cowboyPrefab;
    [SerializeField] private Transform spawnPoint;
    [SerializeField] private int maxCowboys = 8;
    [SerializeField] private float spawnRadiusX = 5f;
    [SerializeField] private float spawnRadiusZ = 5f;

    // …



    private List<GameObject> activeCowboys = new List<GameObject>();

    void Start()
    {
        // Start the coroutine to spawn cowboys with a delay
        StartCoroutine(SpawnInitialCowboys());
    }

    private IEnumerator SpawnInitialCowboys()
    {
        for (int i = 0; i < maxCowboys; i++)
        {
            SpawnCowboy();
            // Wait for 1 second before spawning the next cowboy
            yield return new WaitForSeconds(1f);
        }
    }

    void SpawnCowboy()
    {
        // Instantiate the cowboy at a random position around the spawner
        
        Vector3 randomOffset = new Vector3(
            Random.Range(-spawnRadiusX, spawnRadiusX),
            0f,
            Random.Range(-spawnRadiusZ, spawnRadiusZ)
        );
        Vector3 spawnPosition = spawnPoint.position + randomOffset;

        GameObject cowboy = Instantiate(cowboyPrefab, spawnPosition, spawnPoint.rotation);
        Cowboy cowboyScript = cowboy.GetComponent<Cowboy>();
        cowboyScript.spawner = this; // Link back for callback on death
        activeCowboys.Add(cowboy);
    }

    public void OnCowboyDied(GameObject cowboy)
    {
        activeCowboys.Remove(cowboy);
        Destroy(cowboy);
        if (activeCowboys.Count < maxCowboys)
        {
            SpawnCowboy();
        }
    }
}
