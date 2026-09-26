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
///
/// Sits on the NetworkManager GameObject so it persists with it. Nothing
/// connects until the character screen's Continue; the menu only checks a
/// code, which is why that check is separate from joining.
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

    private Task initialization;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            // A second component on the same object must not take the
            // NetworkManager down with it.
            if (Instance.gameObject == gameObject)
            {
                Debug.LogWarning("Duplicate RelayConnectionManager on one object; removing the extra.", this);
                Destroy(this);
                return;
            }

            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void Start()
    {
        // Started early so it is usually done by the time anyone presses a
        // button, and awaited by every call so an early press still waits.
        initialization = InitializeServices();
    }

    private Task EnsureInitialized()
    {
        // A failed init (no internet at launch) is retried on the next press
        // instead of leaving every later call to fail.
        if (initialization == null || (initialization.IsCompleted && !IsSignedIn()))
        {
            initialization = InitializeServices();
        }
        return initialization;
    }

    private static bool IsSignedIn() =>
        UnityServices.State == ServicesInitializationState.Initialized
        && AuthenticationService.Instance.IsSignedIn;

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

    /// <summary>Allocates, gets a join code, and starts hosting. True if the host is running.</summary>
    public async Task<bool> StartHostAsync()
    {
        if (IsBusy) return false;
        IsBusy = true;

        try
        {
            await EnsureInitialized();

            SetStatus("Creating allocation...");

            // maxPlayers - 1: the host does not consume a Relay connection slot.
            Allocation allocation = await RelayService.Instance.CreateAllocationAsync(maxPlayers - 1);

            SetStatus("Requesting join code...");
            JoinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);

            ConfigureTransport(allocation.ToRelayServerData(ConnectionType));

            if (!NetworkManager.Singleton.StartHost())
            {
                SetStatus("StartHost failed");
                return false;
            }

            SetStatus($"Hosting — code {JoinCode}");
            return true;
        }
        catch (Exception e)
        {
            SetStatus($"Host failed: {e.Message}");
            Debug.LogException(e);
            return false;
        }
        finally
        {
            IsBusy = false;
            OnStateChanged?.Invoke();
        }
    }

    /// <summary>
    /// Asks Relay whether a code belongs to a live host, without connecting.
    /// The allocation it makes is thrown away and times out on its own;
    /// joining for real makes a fresh one, because the character screen can
    /// take longer than an unused allocation lives.
    /// </summary>
    public async Task<bool> CheckJoinCodeAsync(string code)
    {
        if (IsBusy || string.IsNullOrWhiteSpace(code)) return false;
        IsBusy = true;

        try
        {
            await EnsureInitialized();

            SetStatus("Checking code...");
            await RelayService.Instance.JoinAllocationAsync(NormalizeCode(code));

            SetStatus("Code OK");
            return true;
        }
        catch (Exception e)
        {
            SetStatus($"Code check failed: {e.Message}");
            Debug.LogException(e);
            return false;
        }
        finally
        {
            IsBusy = false;
            OnStateChanged?.Invoke();
        }
    }

    /// <summary>Joins a host by code and starts the client. True if the client started.</summary>
    public async Task<bool> JoinWithCodeAsync(string code)
    {
        if (IsBusy || string.IsNullOrWhiteSpace(code)) return false;
        IsBusy = true;

        try
        {
            await EnsureInitialized();

            SetStatus("Joining...");

            JoinAllocation allocation = await RelayService.Instance.JoinAllocationAsync(NormalizeCode(code));

            ConfigureTransport(allocation.ToRelayServerData(ConnectionType));

            if (!NetworkManager.Singleton.StartClient())
            {
                SetStatus("StartClient failed");
                return false;
            }

            JoinCode = NormalizeCode(code);
            SetStatus($"Connected to {JoinCode}");
            return true;
        }
        catch (Exception e)
        {
            SetStatus($"Join failed: {e.Message}");
            Debug.LogException(e);
            return false;
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

    public static string NormalizeCode(string code) => code.Trim().ToUpperInvariant();

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
