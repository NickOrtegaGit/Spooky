using Unity.Netcode;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// Host-only. Scans a tilemap for DoorTiles and spawns a networked door leaf
/// at each one, so doors are placed by painting rather than by hand.
///
/// Same pattern as MonsterSpawner: the host spawns, clients receive.
/// </summary>
public class DoorSpawner : MonoBehaviour
{
    [Tooltip("The tilemap carrying the door markers — the Markers layer, not Walls.")]
    [SerializeField] private Tilemap tilemap;

    [Tooltip("Networked door prefab. Must be in DefaultNetworkPrefabs.asset.")]
    [SerializeField] private GameObject doorPrefab;

    private void Start()
    {
        var nm = NetworkManager.Singleton;
        if (nm == null) return;

        nm.OnServerStarted += SpawnDoors;
    }

    private void OnDestroy()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnServerStarted -= SpawnDoors;
        }
    }

    private void SpawnDoors()
    {
        if (tilemap == null || doorPrefab == null)
        {
            Debug.LogError("DoorSpawner needs both a tilemap and a door prefab.");
            return;
        }

        // cellBounds is a cached rect that can lag behind freshly painted
        // tiles, reporting an empty area and finding nothing. Recompute it.
        tilemap.CompressBounds();

        int spawned = 0;
        int tilesSeen = 0;

        foreach (Vector3Int cell in tilemap.cellBounds.allPositionsWithin)
        {
            TileBase tile = tilemap.GetTile(cell);
            if (tile != null) tilesSeen++;

            if (tile is not DoorTile doorTile) continue;

            // The marker sits on the center cell of the frame's 3x3, which is
            // already the horizontal center of the 2-wide leaf. Only the
            // vertical nudge on the tile is usually needed.
            Vector3 position = tilemap.GetCellCenterWorld(cell) + (Vector3)doorTile.LeafOffset;

            GameObject door = Instantiate(doorPrefab, position, Quaternion.identity);
            door.GetComponent<NetworkObject>().Spawn();
            spawned++;
        }

        if (spawned == 0)
        {
            Debug.LogWarning(
                $"DoorSpawner found no DoorTiles on '{tilemap.name}'. " +
                $"Scanned {tilemap.cellBounds.size.x}x{tilemap.cellBounds.size.y} cells, " +
                $"{tilesSeen} of them painted. " +
                "Check that this is the Markers tilemap and that the painted tile " +
                "is a DoorTile asset (Assets > Create > Spooky > Door Tile).");
            return;
        }

        Debug.Log($"DoorSpawner spawned {spawned} door(s) from '{tilemap.name}'.");
    }
}
