using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Owner-side detection of the nearest interactable, plus the interact input.
/// The actual effect happens on the server.
/// </summary>
public class PlayerInteractor : NetworkBehaviour
{
    [SerializeField] private float interactRange = 1.5f;
    [SerializeField] private LayerMask interactableLayers;

    private Interactable current;
    private PlayerState state;
    private InteractPrompt prompt;

    public Interactable Current => current;

    private void Awake()
    {
        state = GetComponent<PlayerState>();
        prompt = GetComponentInChildren<InteractPrompt>(true);
    }

    private void Update()
    {
        if (!IsOwner) return;

        bool busy = MinigameRunner.Instance != null && MinigameRunner.Instance.IsBusy;

        if ((state != null && state.IsCaught) || busy)
        {
            SetCurrent(null);
            if (prompt != null) prompt.SetInRange(false);
            return;
        }

        SetCurrent(FindNearest());

        if (prompt != null) prompt.SetInRange(current != null);

        if (current != null && Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
        {
            InteractServerRpc(current.NetworkObjectId);
            if (prompt != null) prompt.SuppressAfterInteract();
        }
    }

    private Interactable FindNearest()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, interactRange, interactableLayers);

        Interactable nearest = null;
        float nearestDistance = float.MaxValue;

        foreach (var hit in hits)
        {
            var interactable = hit.GetComponent<Interactable>();
            if (interactable == null) continue;

            float distance = DistanceTo(interactable);
            if (distance >= nearestDistance) continue;

            nearest = interactable;
            nearestDistance = distance;
        }

        return nearest;
    }

    /// <summary>
    /// Measures to the interactable's collider bounds, not its transform. A
    /// sprite that pivots somewhere other than its middle — a door hinged at
    /// its bottom edge — puts transform.position outside the thing the player
    /// is standing at, so the server would reject an interaction the client
    /// showed a prompt for.
    /// </summary>
    private float DistanceTo(Interactable interactable)
    {
        var interactableCollider = interactable.GetComponent<Collider2D>();

        Vector2 point = interactableCollider != null
            ? interactableCollider.bounds.ClosestPoint(transform.position)
            : (Vector2)interactable.transform.position;

        return Vector2.Distance(transform.position, point);
    }

    private void SetCurrent(Interactable next)
    {
        if (current == next) return;

        if (current != null) current.SetHighlighted(false);
        current = next;
        if (current != null) current.SetHighlighted(true);
    }

    [Rpc(SendTo.Server)]
    private void InteractServerRpc(ulong interactableId, RpcParams rpcParams = default)
    {
        if (!NetworkManager.SpawnManager.SpawnedObjects.TryGetValue(interactableId, out var netObj)) return;

        var interactable = netObj.GetComponent<Interactable>();
        if (interactable == null) return;

        // Server re-checks range: never trust the client's claim that it is close.
        float distance = DistanceTo(interactable);
        if (distance > interactRange * 1.5f) return;

        interactable.Interact(rpcParams.Receive.SenderClientId);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, interactRange);
    }
}
