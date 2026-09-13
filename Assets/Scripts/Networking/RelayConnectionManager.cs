using System;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Networking.Transport.Relay;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;

/// <summary>
/// Host/join over Unity Relay with a join code. Written WebSocket-ready: a
/// WebGL build must use wss, because a page served over HTTPS is not allowed
/// to open an insecure connection.
/// </summary>
public class RelayConnectionManager : MonoBehaviour
{
    public static RelayConnectionManager Instance { get; private set; }

#if UNITY_WEBGL && !UNITY_EDITOR
    private const string ConnectionType = "wss";
    private const bool UseWebSockets = true;
#else
    private const string ConnectionType = "dtls";
    private const bool UseWebSockets = false;
#endif

    [SerializeField] private int maxPlayers = 4;

    public string JoinCode { get; private set; } = string.Empty;
    public string Status { get; private set; } = "Not connected";
    public bool IsBusy { get; private set; }

    public event Action OnStateChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private async void Start()
    {
        await InitializeServices();
    }

    private async Task InitializeServices()
    {
        try
        {
            SetStatus("Initializing services...");

            if (UnityServices.State != ServicesInitializationState.Initialized)
            {
                await UnityServices.InitializeAsync();
            }

            if (!AuthenticationService.Instance.IsSignedIn)
            {
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
            }

            SetStatus("Ready");
        }
        catch (Exception e)
        {
            SetStatus($"Service init failed: {e.Message}");
            Debug.LogException(e);
        }
    }

    public async void StartHost()
    {
        if (IsBusy) return;
        IsBusy = true;

        try
        {
            SetStatus("Creating allocation...");

            // maxPlayers - 1: the host does not consume a Relay connection slot.
            Allocation allocation = await RelayService.Instance.CreateAllocationAsync(maxPlayers - 1);

            SetStatus("Requesting join code...");
            JoinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);

            ConfigureTransport(allocation.ToRelayServerData(ConnectionType));

            if (!NetworkManager.Singleton.StartHost())
            {
                SetStatus("StartHost failed");
                return;
            }

            SetStatus($"Hosting — code {JoinCode}");
        }
        catch (Exception e)
        {
            SetStatus($"Host failed: {e.Message}");
            Debug.LogException(e);
        }
        finally
        {
            IsBusy = false;
            OnStateChanged?.Invoke();
        }
    }

    public async void JoinWithCode(string code)
    {
        if (IsBusy || string.IsNullOrWhiteSpace(code)) return;
        IsBusy = true;

        try
        {
            SetStatus("Joining...");

            JoinAllocation allocation = await RelayService.Instance.JoinAllocationAsync(code.Trim().ToUpperInvariant());

            ConfigureTransport(allocation.ToRelayServerData(ConnectionType));

            if (!NetworkManager.Singleton.StartClient())
            {
                SetStatus("StartClient failed");
                return;
            }

            JoinCode = code.Trim().ToUpperInvariant();
            SetStatus($"Connected to {JoinCode}");
        }
        catch (Exception e)
        {
            SetStatus($"Join failed: {e.Message}");
            Debug.LogException(e);
        }
        finally
        {
            IsBusy = false;
            OnStateChanged?.Invoke();
        }
    }

    public void Disconnect()
    {
        if (NetworkManager.Singleton != null && (NetworkManager.Singleton.IsClient || NetworkManager.Singleton.IsServer))
        {
            NetworkManager.Singleton.Shutdown();
        }

        JoinCode = string.Empty;
        SetStatus("Ready");
    }

    private void ConfigureTransport(RelayServerData relayData)
    {
        var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
        transport.UseWebSockets = UseWebSockets;
        transport.SetRelayServerData(relayData);
    }

    private void SetStatus(string status)
    {
        Status = status;
        Debug.Log($"[Relay] {status}");
        OnStateChanged?.Invoke();
    }
}
