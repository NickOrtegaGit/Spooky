using Unity.Netcode;
using UnityEngine;

/// <summary>
/// A door leaf spawned into a DoorTile's frame. Open state is
/// server-authoritative and replicated; the animation is local presentation
/// driven off that state.
///
/// Subclasses Interactable, so it inherits the proximity highlight and the
/// server-validated E press that tasks already use. See Docs/Tasks.md.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class Door : Interactable
{
    private static readonly int OpenParameter = Animator.StringToHash("IsOpen");
    private static readonly int SwingUpParameter = Animator.StringToHash("SwingUp");

    [Header("Door")]
    [SerializeField] private bool startsOpen = false;

    [Tooltip("Which way a door that starts open is already swung.")]
    [SerializeField] private bool startsSwungUp = false;

    [Tooltip("Seconds before the door can be toggled again — stops animation spam.")]
    [SerializeField] private float toggleCooldown = 0.5f;

    [Tooltip("Optional. Left empty, the door snaps open instead of animating.")]
    [SerializeField] private Animator animator;

    [Header("Sorting")]
    [Tooltip("Flip sorting order with the swing. Off: the SpriteRenderer's " +
             "authored order is left alone.")]
    [SerializeField] private bool flipSortingWithSwing = true;

    [Tooltip("Added to the prefab's authored order while swinging down, toward " +
             "the camera, so the leaf draws over the player.")]
    [SerializeField] private int swingDownSortingBoost = 10;

    private readonly NetworkVariable<bool> isOpen = new NetworkVariable<bool>(false);

    // Which way the leaf swung when it was opened. Meaningless while closed,
    // but it must persist through the close so the right clip plays on the
    // way shut — a door that opened upward closes downward regardless of
    // where the player is standing.
    private readonly NetworkVariable<bool> swingUp = new NetworkVariable<bool>(false);

    private Collider2D blocker;
    private SpriteRenderer leafRenderer;
    private int baseSortingOrder;
    private float nextToggleTime;

    public bool IsOpen => isOpen.Value;
    public bool SwungUp => swingUp.Value;

    protected override void Awake()
    {
        base.Awake();
        blocker = GetComponent<Collider2D>();

        // Only one collider may belong to the leaf. A second one — often left
        // on a child, or added alongside the box — never gets toggled and
        // blocks the doorway even while the door reads as open.
        var allColliders = GetComponentsInChildren<Collider2D>();
        if (allColliders.Length > 1)
        {
            Debug.LogWarning(
                $"Door '{name}' has {allColliders.Length} colliders; only '{blocker?.GetType().Name}' " +
                "on the root is toggled. Remove the others or the doorway stays blocked.", this);
        }
        leafRenderer = GetComponent<SpriteRenderer>();

        // Whatever the prefab was authored with is the resting order; the
        // swing only ever offsets from it.
        if (leafRenderer != null) baseSortingOrder = leafRenderer.sortingOrder;
        if (animator == null) animator = GetComponentInChildren<Animator>();
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            isOpen.Value = startsOpen;
            swingUp.Value = startsSwungUp;
        }

        isOpen.OnValueChanged += OnOpenChanged;
        swingUp.OnValueChanged += OnSwingChanged;
        ApplyState(instant: true);
    }

    public override void OnNetworkDespawn()
    {
        isOpen.OnValueChanged -= OnOpenChanged;
        swingUp.OnValueChanged -= OnSwingChanged;
    }

    public override void Interact(ulong clientId)
    {
        if (!IsServer) return;
        if (Time.time < nextToggleTime) return;

        nextToggleTime = Time.time + toggleCooldown;

        if (isOpen.Value)
        {
            // Closing: the swing direction is already set and must not change.
            // The door shuts the way it opened, wherever the player stands.
            isOpen.Value = false;
            return;
        }

        // Opening: the door swings away from whoever opened it, so a player
        // above pushes it down and a player below pushes it up.
        swingUp.Value = OpenerIsBelow(clientId);
        isOpen.Value = true;
    }

    /// <summary>
    /// Server-side. Compares the opening player's position to the door's.
    /// Defaults to swinging down if the player cannot be resolved.
    /// </summary>
    private bool OpenerIsBelow(ulong clientId)
    {
        var nm = NetworkManager.Singleton;
        if (nm == null) return false;

        if (!nm.ConnectedClients.TryGetValue(clientId, out var client)) return false;

        var playerObject = client.PlayerObject;
        if (playerObject == null) return false;

        return playerObject.transform.position.y < transform.position.y;
    }

    private void OnOpenChanged(bool previous, bool current) => ApplyState(instant: false);
    private void OnSwingChanged(bool previous, bool current) => ApplyState(instant: false);

    /// <summary>
    /// Collision flips immediately with the replicated state; the animation is
    /// only presentation. Waiting for the animation to finish would let a
    /// player walk into a door that looks open on their screen but is still
    /// solid on the host's.
    /// </summary>
    private void ApplyState(bool instant)
    {
        if (blocker != null) blocker.isTrigger = isOpen.Value;

        // A leaf swinging down comes toward the camera and should cover the
        // player. Everything else — closed, or swung up and away — sits at the
        // order the prefab was authored with.
        if (leafRenderer != null && flipSortingWithSwing)
        {
            bool drawInFront = isOpen.Value && !swingUp.Value;
            leafRenderer.sortingOrder = drawInFront
                ? baseSortingOrder + swingDownSortingBoost
                : baseSortingOrder;
        }

        if (animator == null) return;

        // Set the swing first: the transition out of Closed reads it, so it
        // has to be correct before IsOpen flips.
        animator.SetBool(SwingUpParameter, swingUp.Value);
        animator.SetBool(OpenParameter, isOpen.Value);

        // Skip the animation for the state the door spawns in.
        if (instant) animator.Update(0f);
    }
}
