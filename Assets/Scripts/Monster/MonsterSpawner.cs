using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Host-only. Spawns the monster once the network session starts.
/// </summary>
public class MonsterSpawner : MonoBehaviour
{
    [SerializeField] private GameObject monsterPrefab;
    [SerializeField] private Transform spawnPoint;

    private void Start()
    {
        var nm = NetworkManager.Singleton;
        if (nm == null) return;

        nm.OnServerStarted += SpawnMonster;
    }

    private void OnDestroy()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnServerStarted -= SpawnMonster;
        }
    }

    private void SpawnMonster()
    {
        if (monsterPrefab == null)
        {
            Debug.LogError("MonsterSpawner has no monster prefab assigned.");
            return;
        }

        Vector3 position = spawnPoint != null ? spawnPoint.position : Vector3.zero;
        GameObject monster = Instantiate(monsterPrefab, position, Quaternion.identity);
        monster.GetComponent<NetworkObject>().Spawn();
    }
}
