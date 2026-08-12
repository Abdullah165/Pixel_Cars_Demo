using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class SceneryTrack
{
    public string trackName;
    public GameObject[] prefabs;
    public float[] xPositions;
    [Range(0f, 1f)]
    public float spawnProbability = 0.7f;
}

public class ScenerySpawner : MonoBehaviour
{
    [Header("Spawn Settings")]
    public SceneryTrack[] sceneryTracks;
    public float spawnInterval = 0.8f; 
    
    [Header("Pre-Warm Settings")]
    public float screenBottomY = -6f; // The lowest point on your screen to start spawning
    public float sceneryMoveSpeed = 5f; // This MUST match the moveSpeed on your Tree prefabs
    
    [Header("Pool Settings")]
    public int poolSizePerPrefab = 5;

    private Dictionary<GameObject, List<GameObject>> poolDictionary;
    private float timer;

    void Start()
    {
        InitializePool();
        PreWarmScenery(); // Fill the screen immediately on game start
    }

    void InitializePool()
    {
        poolDictionary = new Dictionary<GameObject, List<GameObject>>();

        foreach (SceneryTrack track in sceneryTracks)
        {
            foreach (GameObject prefab in track.prefabs)
            {
                if (prefab == null || poolDictionary.ContainsKey(prefab)) continue;

                List<GameObject> objectPool = new List<GameObject>();
                for (int i = 0; i < poolSizePerPrefab; i++)
                {
                    GameObject obj = Instantiate(prefab, transform.position, Quaternion.identity, transform);
                    obj.SetActive(false);
                    objectPool.Add(obj);
                }
                poolDictionary.Add(prefab, objectPool);
            }
        }
    }

    // Instantly fills the screen with scenery from bottom to top
    void PreWarmScenery()
    {
        // Calculate the physical distance between spawns based on speed and time
        float distanceBetweenSpawns = sceneryMoveSpeed * spawnInterval;

        // Loop from the bottom of the screen up to the spawner's current Y position
        for (float currentY = screenBottomY; currentY < transform.position.y; currentY += distanceBetweenSpawns)
        {
            SpawnSceneryAtY(currentY);
        }
    }

    void Update()
    {
        timer += Time.deltaTime;
        if (timer >= spawnInterval)
        {
            // Standard spawning at the top of the screen
            SpawnSceneryAtY(transform.position.y);
            timer = 0f;
        }
    }

    // The core spawning logic, now accepting a specific Y coordinate
    void SpawnSceneryAtY(float yPos)
    {
        foreach (SceneryTrack track in sceneryTracks)
        {
            foreach (float xPos in track.xPositions)
            {
                if (Random.value > track.spawnProbability) continue; 

                GameObject selectedPrefab = track.prefabs[Random.Range(0, track.prefabs.Length)];
                GameObject spawnedObject = GetPooledObject(selectedPrefab);
                
                if (spawnedObject != null)
                {
                    float randomYOffset = Random.Range(0f, 0.5f);
                    spawnedObject.transform.position = new Vector2(xPos, yPos + randomYOffset);
                    spawnedObject.SetActive(true);
                }
            }
        }
    }

    GameObject GetPooledObject(GameObject prefabKey)
    {
        if (poolDictionary.TryGetValue(prefabKey, out List<GameObject> pool))
        {
            int startIndex = Random.Range(0, pool.Count);
            for (int i = 0; i < pool.Count; i++)
            {
                int index = (startIndex + i) % pool.Count;
                if (!pool[index].activeInHierarchy)
                {
                    return pool[index];
                }
            }
        }
        return null; 
    }
}