using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Host-only. Spawns the monster once the house is loaded on every client.
/// </summary>
public class MonsterSpawner : MonoBehaviour
{
    [SerializeField] private GameObject monsterPrefab;
    [SerializeField] private Transform spawnPoint;

    // Awake, not Start: with only the host in the session, the house can
    // finish loading everywhere before this scene's Start runs.
    // HouseReady only fires on the server, so clients never spawn anything.
    private void Awake()
    {
        SessionManager.HouseReady += SpawnMonster;
    }

    private void OnDestroy()
    {
        SessionManager.HouseReady -= SpawnMonster;
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
