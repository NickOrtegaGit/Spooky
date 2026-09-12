using System.Collections.Generic;
using UnityEngine;

public class SpawnManager : MonoBehaviour
{
    public static SpawnManager Instance { get; private set; }

    [SerializeField] private List<Transform> spawnPoints = new List<Transform>();

    private int nextIndex;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public Vector3 GetSpawnPosition()
    {
        if (spawnPoints.Count == 0)
        {
            Debug.LogWarning("SpawnManager has no spawn points assigned; spawning at origin.");
            return Vector3.zero;
        }

        Transform point = spawnPoints[nextIndex % spawnPoints.Count];
        nextIndex++;
        return point.position;
    }
}
