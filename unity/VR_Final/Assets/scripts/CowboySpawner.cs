using UnityEngine;
using System.Collections.Generic;

public class CowboySpawner : MonoBehaviour
{
    [SerializeField] private GameObject cowboyPrefab;
    [SerializeField] private Transform spawnPoint;
    [SerializeField] private int maxCowboys = 8;

    private List<GameObject> activeCowboys = new List<GameObject>();

    void Start()
    {
        SpawnInitialCowboys();
    }

    void SpawnInitialCowboys()
    {
        for (int i = 0; i < maxCowboys; i++)
        {
            SpawnCowboy();
        }
    }

    void SpawnCowboy()
    {
        GameObject cowboy = Instantiate(cowboyPrefab, spawnPoint.position, spawnPoint.rotation);
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
