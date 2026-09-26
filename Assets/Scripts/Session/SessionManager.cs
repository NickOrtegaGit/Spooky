using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Owns the shape of a session: who is let in, when the house loads, and
/// where everyone goes when it ends.
///
/// Lives on the NetworkManager GameObject in MainMenu and persists with it.
/// Returning to MainMenu loads a second copy of that object; this is what
/// destroys it, so there is only ever one NetworkManager.
///
/// Players are not spawned on connect. The van shows seats, not characters,
/// and the house's SpawnManager does not exist until the house loads — so
/// the server spawns each player object itself once every client has the
/// house loaded, then tells the house's spawners to go.
/// </summary>
[RequireComponent(typeof(NetworkManager))]
public class SessionManager : MonoBehaviour
{
    public const int MaxPlayers = 4;

    public static SessionManager Instance { get; private set; }

    /// <summary>
    /// Server only. Fired once the house is loaded on every client and every
    /// player object exists. House spawners (monster, doors) listen for this
    /// instead of OnServerStarted, which now fires back in the menu.
    /// </summary>
    public static event Action HouseReady;

    /// <summary>Server only. True from Continue until the session ends; late joins are refused.</summary>
    public bool InHouse { get; private set; }

    private NetworkManager nm;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            // The MainMenu copy of the NetworkManager object, loaded again on
            // the way back to the menu. The original is still alive.
            Destroy(gameObject);
            return;
        }

        Instance = this;
        nm = GetComponent<NetworkManager>();
    }

    private void Start()
    {
        if (Instance != this) return;

        // Set here rather than trusting the Inspector checkbox: without it
        // the callback never runs and every client gets a player on connect.
        nm.NetworkConfig.ConnectionApproval = true;
        nm.ConnectionApprovalCallback = ApproveConnection;
        nm.OnServerStarted += OnServerStarted;
        nm.OnServerStopped += OnServerStopped;
        nm.OnClientDisconnectCallback += OnClientDisconnect;
    }

    private void OnDestroy()
    {
        if (Instance != this) return;
        Instance = null;

        if (nm == null) return;
        nm.ConnectionApprovalCallback = null;
        nm.OnServerStarted -= OnServerStarted;
        nm.OnServerStopped -= OnServerStopped;
        nm.OnClientDisconnectCallback -= OnClientDisconnect;
    }

    // ---- Connection ----------------------------------------------------

    private void ApproveConnection(NetworkManager.ConnectionApprovalRequest request,
                                   NetworkManager.ConnectionApprovalResponse response)
    {
        // Player objects are spawned by hand when the house loads.
        response.CreatePlayerObject = false;

        // The host approves itself through this same callback.
        bool isHost = request.ClientNetworkId == NetworkManager.ServerClientId;

        if (!isHost && InHouse)
        {
            response.Approved = false;
            response.Reason = "Too late — that job has already started.";
            return;
        }

        if (!isHost && nm.ConnectedClientsIds.Count >= MaxPlayers)
        {
            response.Approved = false;
            response.Reason = "The van is full.";
            return;
        }

        response.Approved = true;
    }

    private void OnClientDisconnect(ulong clientId)
    {
        // On the server this fires for every client that leaves; the van's
        // seats will care about that. Here only our own disconnect matters.
        if (nm.IsServer) return;

        string reason = string.IsNullOrEmpty(nm.DisconnectReason)
            ? "Lost connection to the host."
            : nm.DisconnectReason;

        ReturnToMenu(reason);
    }

    // ---- Scene flow ----------------------------------------------------

    /// <summary>Server only. Called once hosting succeeds; clients follow on connect.</summary>
    public void LoadVan()
    {
        if (!nm.IsServer) return;
        nm.SceneManager.LoadScene(SceneNames.Van, LoadSceneMode.Single);
    }

    /// <summary>Server only. The van's Continue. Everyone connected goes; nobody else can come.</summary>
    public void LoadHouse()
    {
        if (!nm.IsServer || InHouse) return;

        InHouse = true;
        nm.SceneManager.LoadScene(SceneNames.House, LoadSceneMode.Single);
    }

    /// <summary>Leaves any session and goes back to the menu, optionally with a message for it to show.</summary>
    public void ReturnToMenu(string message = "")
    {
        PlayerProfile.MenuMessage = message;

        if (RelayConnectionManager.Instance != null) RelayConnectionManager.Instance.Disconnect();
        else if (nm.IsListening) nm.Shutdown();

        SceneManager.LoadScene(SceneNames.MainMenu);
    }

    private void OnServerStarted()
    {
        // SceneManager is created per session, so subscribe per session.
        nm.SceneManager.OnLoadEventCompleted += OnLoadEventCompleted;
    }

    private void OnServerStopped(bool wasHost)
    {
        InHouse = false;
        if (nm.SceneManager != null) nm.SceneManager.OnLoadEventCompleted -= OnLoadEventCompleted;
    }

    private void OnLoadEventCompleted(string sceneName, LoadSceneMode mode,
                                      List<ulong> clientsCompleted, List<ulong> clientsTimedOut)
    {
        if (sceneName != SceneNames.House) return;

        // Anyone who never finished loading cannot be given a player in a
        // scene they do not have.
        foreach (ulong clientId in clientsTimedOut)
        {
            nm.DisconnectClient(clientId, "Took too long to load the house.");
        }

        foreach (ulong clientId in clientsCompleted)
        {
            SpawnPlayer(clientId);
        }

        HouseReady?.Invoke();
    }

    private void SpawnPlayer(ulong clientId)
    {
        if (!nm.ConnectedClients.TryGetValue(clientId, out NetworkClient client)) return;
        if (client.PlayerObject != null) return;

        GameObject prefab = nm.NetworkConfig.PlayerPrefab;
        if (prefab == null)
        {
            Debug.LogError("SessionManager: NetworkManager has no Player Prefab to spawn.", this);
            return;
        }

        // PlayerMovement asks SpawnManager for a position in OnNetworkSpawn,
        // which works now because the house is loaded.
        GameObject player = Instantiate(prefab);
        player.GetComponent<NetworkObject>().SpawnAsPlayerObject(clientId, destroyWithScene: true);
    }
}
