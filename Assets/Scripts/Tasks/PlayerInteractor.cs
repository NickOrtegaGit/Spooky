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

    public Interactable Current => current;

    private void Awake()
    {
        state = GetComponent<PlayerState>();
    }

    private void Update()
    {
        if (!IsOwner) return;

        if (state != null && state.IsCaught)
        {
            SetCurrent(null);
            return;
        }

        SetCurrent(FindNearest());

        if (current != null && Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
        {
            InteractServerRpc(current.NetworkObjectId);
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

            float distance = Vector2.Distance(transform.position, hit.transform.position);
            if (distance >= nearestDistance) continue;

            nearest = interactable;
            nearestDistance = distance;
        }

        return nearest;
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
        float distance = Vector2.Distance(transform.position, interactable.transform.position);
        if (distance > interactRange * 1.5f) return;

        interactable.Interact(rpcParams.Receive.SenderClientId);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, interactRange);
    }
}
