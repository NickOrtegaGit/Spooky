using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// The player's single item slot. One item at a time; picking up a new one
/// drops the old.
/// </summary>
public class PlayerItemSlot : NetworkBehaviour
{
    [SerializeField] private float dropDistance = 0.6f;

    private CarryableItem held;
    private PlayerState state;
    private PlayerAnimator playerAnimator;

    public CarryableItem Held => held;
    public bool HasItem => held != null;

    private void Awake()
    {
        state = GetComponent<PlayerState>();
        playerAnimator = GetComponent<PlayerAnimator>();
    }

    private void Update()
    {
        if (!IsOwner) return;
        if (state != null && state.IsCaught) return;

        var keyboard = Keyboard.current;
        if (keyboard == null) return;

        if (keyboard.qKey.wasPressedThisFrame) DropServerRpc();
        if (keyboard.spaceKey.wasPressedThisFrame) UseServerRpc();
    }

    /// <summary>Server only. Swaps in a new item, dropping whatever is held.</summary>
    public void ServerPickUp(CarryableItem item)
    {
        if (!IsServer || item == null) return;

        // Swap is one server-side operation so no frame can observe an
        // in-between state where the player holds nothing.
        if (held != null) DropHeld();

        held = item;
        item.ServerSetHolder(OwnerClientId);
    }

    /// <summary>Server only. Called when the player is caught.</summary>
    public void ServerDropAll()
    {
        if (!IsServer) return;
        DropHeld();
    }

    private void DropHeld()
    {
        if (held == null) return;

        held.ServerDrop(transform.position + DropOffset());
        held = null;
    }

    private Vector3 DropOffset()
    {
        // Drop in front of the player where possible, so it does not land
        // underfoot and immediately re-prompt.
        Vector2 facing = playerAnimator != null ? playerAnimator.Facing : Vector2.down;
        if (facing == Vector2.zero) facing = Vector2.down;
        return (Vector3)(facing.normalized * dropDistance);
    }

    [Rpc(SendTo.Server)]
    private void DropServerRpc()
    {
        DropHeld();
    }

    [Rpc(SendTo.Server)]
    private void UseServerRpc()
    {
        if (held == null) return;
        held.Use(OwnerClientId);
    }
}
