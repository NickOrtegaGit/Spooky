using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Tracks which Room the local player is in, so everything outside it can be
/// occluded. See Docs/House Layout.md.
///
/// Purely local presentation — each client tracks its own player and nothing
/// replicates. The monster and other players are hidden by the mask like any
/// other world content, which is the point: a room you are not in shows
/// nothing, whoever is standing in it.
///
/// Room membership is deliberately **sticky**. Doorways and polygon seams
/// leave the player briefly inside no room at all, and switching to "nothing
/// visible" for those frames would flicker. Instead the last room entered
/// stays current until the player is fully inside a different one.
/// </summary>
public class RoomTracker : MonoBehaviour
{
    public static RoomTracker Instance { get; private set; }

    [Tooltip("Rooms in the scene. Leave empty to find them all on start.")]
    [SerializeField] private List<Room> rooms = new List<Room>();

    [Header("Crude mask (phase one)")]
    [Tooltip("Temporary stand-in for the real occlusion pass: a black sprite " +
             "toggled on while the player is in no known room. Proves tracking " +
             "works before any render feature exists.")]
    [SerializeField] private SpriteRenderer crudeMask;

    [Header("Transition")]
    [Tooltip("Seconds the room you just left stays visible, easing out as you " +
             "cross. 0 snaps.")]
    [SerializeField] private float transitionSeconds = 0.35f;

    [Header("Debug")]
    [SerializeField] private bool logRoomChanges = false;

    private Transform localPlayer;
    private Room currentRoom;
    private Room previousRoom;

    // CreateMesh allocates, so each room's mesh is built once and kept.
    private readonly Dictionary<Room, Mesh> roomMeshes = new Dictionary<Room, Mesh>();

    /// <summary>The room the local player is in, or null before any is entered.</summary>
    public Room CurrentRoom => currentRoom;

    /// <summary>
    /// The current room's polygon as a mesh, for the occlusion pass to stencil.
    /// Built on first use and cached.
    /// </summary>
    public Mesh CurrentRoomMesh => currentRoom != null ? MeshFor(currentRoom) : null;

    /// <summary>
    /// The room just left, still drawn while the transition runs so the two
    /// rooms overlap instead of the screen blinking between them. Null once the
    /// transition finishes.
    /// </summary>
    public Mesh PreviousRoomMesh =>
        previousRoom != null && TransitionProgress < 1f ? MeshFor(previousRoom) : null;

    /// <summary>0 at the moment of crossing, 1 once the old room is fully gone.</summary>
    public float TransitionProgress { get; private set; } = 1f;

    /// <summary>Raised when the local player becomes fully inside a different room.</summary>
    public event System.Action<Room> RoomChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        if (rooms.Count == 0)
        {
            rooms.AddRange(FindObjectsByType<Room>(FindObjectsSortMode.None));
        }

        if (rooms.Count == 0)
        {
            Debug.LogWarning("RoomTracker found no Rooms in the scene.", this);
        }
    }

    private void Update()
    {
        ResolveLocalPlayer();
        if (localPlayer == null) return;

        Room found = RoomAt(localPlayer.position);

        // Sticky: only a different room displaces the current one. Being inside
        // no room — a doorway, a gap between polygons — keeps the last one.
        if (found != null && found != currentRoom)
        {
            previousRoom = currentRoom;
            currentRoom = found;

            // Only cross-fade between two real rooms; the first room entered
            // has nothing to fade from.
            TransitionProgress = previousRoom != null && transitionSeconds > 0f ? 0f : 1f;

            if (logRoomChanges)
            {
                string from = previousRoom != null ? previousRoom.RoomName : "nowhere";
                Debug.Log($"RoomTracker: {from} -> {currentRoom.RoomName}", this);
            }

            RoomChanged?.Invoke(currentRoom);
        }

        if (TransitionProgress < 1f)
        {
            TransitionProgress = Mathf.Min(1f, TransitionProgress + Time.deltaTime / transitionSeconds);
            if (TransitionProgress >= 1f) previousRoom = null;
        }

        UpdateCrudeMask();
    }

    /// <summary>
    /// The room's polygon triangulated into a mesh, already in WORLD space —
    /// so the occlusion pass draws it with an identity matrix. Handles concave
    /// shapes; a self-intersecting polygon will triangulate into garbage.
    ///
    /// Cached, so a room that moves at runtime would need its mesh rebuilt.
    /// Rooms are static scene geometry, so that is not handled.
    /// </summary>
    private Mesh MeshFor(Room room)
    {
        if (roomMeshes.TryGetValue(room, out Mesh cached) && cached != null) return cached;

        // useDelaunay false keeps the triangulation faithful to the outline;
        // useBounds false means the polygon itself, not its bounding box.
        Mesh mesh = room.Area.CreateMesh(false, false);

        if (mesh == null)
        {
            Debug.LogWarning($"Room '{room.RoomName}' produced no mesh — check its polygon " +
                             "for self-intersecting edges.", room);
            return null;
        }

        mesh.name = $"RoomMesh_{room.RoomName}";
        roomMeshes[room] = mesh;
        return mesh;
    }

    private void OnDestroy()
    {
        foreach (Mesh mesh in roomMeshes.Values)
        {
            if (mesh != null) Destroy(mesh);
        }
        roomMeshes.Clear();
    }

    private Room RoomAt(Vector2 point)
    {
        foreach (Room room in rooms)
        {
            if (room != null && room.Contains(point)) return room;
        }

        return null;
    }

    /// <summary>
    /// Phase one only. Shows black while the player is in no room at all, which
    /// should never happen once the polygons cover the house — so a flash of it
    /// means a gap to fix.
    /// </summary>
    private void UpdateCrudeMask()
    {
        if (crudeMask == null) return;

        crudeMask.enabled = currentRoom == null;
    }

    private void ResolveLocalPlayer()
    {
        if (localPlayer != null) return;

        var nm = NetworkManager.Singleton;
        var playerObject = nm != null && nm.IsClient ? nm.LocalClient?.PlayerObject : null;
        if (playerObject != null) localPlayer = playerObject.transform;
    }
}
