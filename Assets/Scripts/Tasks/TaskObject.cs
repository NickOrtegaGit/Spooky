using Unity.Netcode;
using UnityEngine;

/// <summary>
/// An interactable that counts toward the round's task list.
///
/// Completion and occupancy are server-authoritative and replicated; the
/// minigame itself runs locally on the player doing it. See Docs/Tasks.md.
/// </summary>
public class TaskObject : Interactable
{
    [SerializeField] private string taskDescription = "Do the thing";
    [SerializeField] private Minigame minigamePrefab;

    private readonly NetworkVariable<bool> isComplete = new NetworkVariable<bool>(false);

    // ulong.MaxValue means nobody is working on this.
    private readonly NetworkVariable<ulong> occupantClientId =
        new NetworkVariable<ulong>(ulong.MaxValue);

    private Collider2D taskCollider;

    public string TaskDescription => taskDescription;
    public bool IsComplete => isComplete.Value;
    public bool IsOccupied => occupantClientId.Value != ulong.MaxValue;

    protected override void Awake()
    {
        base.Awake();
        taskCollider = GetComponent<Collider2D>();
    }

    public override void OnNetworkSpawn()
    {
        isComplete.OnValueChanged += OnCompleteChanged;
        ApplyCompleteState(isComplete.Value);
    }

    public override void OnNetworkDespawn()
    {
        isComplete.OnValueChanged -= OnCompleteChanged;
    }

    private void OnCompleteChanged(bool previous, bool current) => ApplyCompleteState(current);

    /// <summary>A finished task stops being detectable, so no prompt or highlight.</summary>
    private void ApplyCompleteState(bool complete)
    {
        if (complete) SetHighlighted(false);
        if (taskCollider != null) taskCollider.enabled = !complete;
    }

    public override void Interact(ulong clientId)
    {
        if (!IsServer) return;
        if (isComplete.Value || IsOccupied) return;

        occupantClientId.Value = clientId;
        BeginMinigameRpc(RpcTarget.Single(clientId, RpcTargetUse.Temp));
    }

    /// <summary>Runs only on the client that started the task.</summary>
    [Rpc(SendTo.SpecifiedInParams)]
    private void BeginMinigameRpc(RpcParams rpcParams)
    {
        if (minigamePrefab == null)
        {
            Debug.LogWarning($"{name} has no minigame prefab; completing immediately.");
            ReportCompleteServerRpc();
            return;
        }

        MinigameRunner.Instance.Run(minigamePrefab, this);
    }

    /// <summary>Called locally by the minigame when the player fails.</summary>
    [Rpc(SendTo.Server)]
    public void ReportFailServerRpc()
    {
        occupantClientId.Value = ulong.MaxValue;
    }

    /// <summary>Called locally by the minigame when the player succeeds.</summary>
    [Rpc(SendTo.Server)]
    public void ReportCompleteServerRpc()
    {
        if (isComplete.Value) return;

        isComplete.Value = true;
        occupantClientId.Value = ulong.MaxValue;
        TaskTracker.Instance?.NotifyTaskCompleted();
    }

    /// <summary>Server only. Frees the task if its occupant is caught or leaves.</summary>
    public void ServerReleaseIfOccupiedBy(ulong clientId)
    {
        if (!IsServer) return;
        if (occupantClientId.Value == clientId) occupantClientId.Value = ulong.MaxValue;
    }
}
