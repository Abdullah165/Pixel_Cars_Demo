using System.Collections.Generic;
using UnityEngine;

public class TrafficSpawner : MonoBehaviour
{
    [Header("Spawn Settings")]
    public GameObject[] vehiclePrefabs;
    public float spawnInterval = 1.5f;
    public float[] laneXPositions;

    [Header("Pool Settings")]
    public int poolSizePerPrefab = 3;

    private List<GameObject> pool = new List<GameObject>();
    private float timer;

    void Start()
    {
        InitializePool();
    }

    void InitializePool()
    {
        foreach (GameObject prefab in vehiclePrefabs)
        {
            // Safety check to ensure no empty slots in the array are processed
            if (prefab == null) continue;

            for (int i = 0; i < poolSizePerPrefab; i++)
            {
                // Instantiate directly as a child of the spawner in one operation
                GameObject obj = Instantiate(prefab, transform.position, Quaternion.identity, transform);

                obj.SetActive(false);
                pool.Add(obj);
            }
        }
    }

    void Update()
    {
        timer += Time.deltaTime;
        if (timer >= spawnInterval)
        {
            SpawnVehicle();
            timer = 0f;
        }
    }

    void SpawnVehicle()
    {
        GameObject vehicle = GetPooledObject();

        if (vehicle != null)
        {
            float randomX = laneXPositions[Random.Range(0, laneXPositions.Length)];
            vehicle.transform.position = new Vector2(randomX, transform.position.y);
            vehicle.SetActive(true);
        }
    }

    GameObject GetPooledObject()
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
        return null;
    }
}