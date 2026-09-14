using Unity.Netcode;
using UnityEngine;

/// <summary>
/// An item that can be picked up, carried, dropped, and used. Subclass and
/// override Use for real behavior.
/// </summary>
public class CarryableItem : Interactable
{
    [SerializeField] private string itemName = "Item";
    [SerializeField] private Vector3 heldOffset = new Vector3(0.3f, 0f, 0f);

    [Header("Sprites")]
    [SerializeField] private SpriteRenderer itemSprite;
    [SerializeField] private Sprite groundSprite;
    [SerializeField] private Sprite heldSprite;

    // ulong.MaxValue means "on the ground". Replicated so every client draws
    // the item in the right hands.
    private readonly NetworkVariable<ulong> holderClientId =
        new NetworkVariable<ulong>(ulong.MaxValue);

    private Collider2D itemCollider;

    public string ItemName => itemName;
    public Vector3 HeldOffset => heldOffset;
    public bool IsHeld => holderClientId.Value != ulong.MaxValue;
    public ulong HolderClientId => holderClientId.Value;

    protected override void Awake()
    {
        base.Awake();
        itemCollider = GetComponent<Collider2D>();
        if (itemSprite == null) itemSprite = GetComponent<SpriteRenderer>();
    }

    /// <summary>Exposed so subclasses can drive flipping and sort order.</summary>
    public SpriteRenderer ItemSprite => itemSprite;

    public override void OnNetworkSpawn()
    {
        holderClientId.OnValueChanged += OnHolderChanged;
        ApplyHeldState(holderClientId.Value);
    }

    public override void OnNetworkDespawn()
    {
        holderClientId.OnValueChanged -= OnHolderChanged;
    }

    /// <summary>Interacting with a grounded item means picking it up.</summary>
    public override void Interact(ulong clientId)
    {
        if (!IsServer) return;

        // First request wins: a later one sees the item already held.
        if (IsHeld) return;

        PlayerItemSlot slot = FindSlot(clientId);
        if (slot == null) return;

        slot.ServerPickUp(this);
    }

    /// <summary>Server only. Called by PlayerItemSlot.</summary>
    public void ServerSetHolder(ulong clientId)
    {
        if (!IsServer) return;
        holderClientId.Value = clientId;
    }

    /// <summary>Server only. Drops the item at a position.</summary>
    public virtual void ServerDrop(Vector3 position)
    {
        if (!IsServer) return;
        holderClientId.Value = ulong.MaxValue;
        transform.position = position;
    }

    /// <summary>Override for real item behavior. Runs on the server.</summary>
    public virtual void Use(ulong clientId)
    {
        Debug.Log($"{itemName} used by client {clientId}.");
    }

    private void OnHolderChanged(ulong previous, ulong current) => ApplyHeldState(current);

    private void ApplyHeldState(ulong holder)
    {
        bool held = holder != ulong.MaxValue;

        // A held item must not be detected as a pickup target by anyone.
        if (itemCollider != null) itemCollider.enabled = !held;

        // Ground and held sprites are usually drawn at different angles.
        if (itemSprite != null)
        {
            Sprite wanted = held ? heldSprite : groundSprite;
            if (wanted != null) itemSprite.sprite = wanted;
            if (!held) itemSprite.flipX = false;
        }

        if (held)
        {
            SetHighlighted(false);
            var slot = FindSlot(holder);
            if (slot != null) transform.SetParent(slot.transform, false);
            transform.localPosition = heldOffset;
        }
        else
        {
            transform.SetParent(null, true);
        }

        OnHeldStateChanged(held);
    }

    /// <summary>Hook for subclasses that need to react to being picked up or dropped.</summary>
    protected virtual void OnHeldStateChanged(bool held) { }

    private static PlayerItemSlot FindSlot(ulong clientId)
    {
        if (NetworkManager.Singleton == null) return null;
        if (!NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out var client)) return null;
        if (client.PlayerObject == null) return null;
        return client.PlayerObject.GetComponent<PlayerItemSlot>();
    }
}
