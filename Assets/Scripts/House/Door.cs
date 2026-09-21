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
    private static readonly int SwingPositiveParameter = Animator.StringToHash("SwingPositive");

    /// <summary>Which axis the leaf swings along. Decides the clips and the animator.</summary>
    public enum Orientation
    {
        /// <summary>A door in a horizontal wall, swinging up or down.</summary>
        UpDown,

        /// <summary>A door in a vertical wall, swinging left or right.</summary>
        LeftRight,
    }

    [Header("Door")]
    [Tooltip("UpDown swings away along Y; LeftRight along X. Each needs its own " +
             "animator controller, with clips for that axis.")]
    [SerializeField] private Orientation orientation = Orientation.UpDown;

    [SerializeField] private bool startsOpen = false;

    [Tooltip("Which way a door that starts open is already swung — up, or right.")]
    [SerializeField] private bool startsSwungPositive = false;

    [Tooltip("Seconds before the door can be toggled again — stops animation spam.")]
    [SerializeField] private float toggleCooldown = 0.5f;

    [Header("Closing")]
    [Tooltip("Push players standing in the doorway clear when it shuts. Without " +
             "this, physics depenetration picks its own direction and can wedge " +
             "someone into a wall.")]
    [SerializeField] private bool pushPlayersOnClose = true;

    [Tooltip("How far past the doorway edge a pushed player ends up, in world units.")]
    [SerializeField] private float pushClearance = 0.6f;

    [Tooltip("Layers to sweep for players when the door shuts.")]
    [SerializeField] private LayerMask playerLayers = ~0;

    [Tooltip("Optional. Left empty, the door snaps open instead of animating.")]
    [SerializeField] private Animator animator;

    [Header("Sorting")]
    [Tooltip("Flip sorting order with the swing. Off: the SpriteRenderer's " +
             "authored order is left alone.")]
    [SerializeField] private bool flipSortingWithSwing = true;

    [Tooltip("Added to the prefab's authored order while the leaf swings toward " +
             "the camera, so it draws over the player. UpDown doors only — a " +
             "LeftRight leaf never comes forward, so this is ignored for them.")]
    [SerializeField] private int swingDownSortingBoost = 10;

    [Header("Debug")]
    [Tooltip("Logs which side the opener was on and what the Animator plays.")]
    [SerializeField] private bool logSwing = false;

    private readonly NetworkVariable<bool> isOpen = new NetworkVariable<bool>(false);

    // Which way the leaf swung when it was opened — up for UpDown doors, right
    // for LeftRight ones. Meaningless while closed, but it must persist through
    // the close so the right clip plays on the way shut: a door that opened
    // upward closes downward regardless of where the player is standing.
    private readonly NetworkVariable<bool> swingPositive = new NetworkVariable<bool>(false);

    private Collider2D blocker;
    private SpriteRenderer leafRenderer;
    private int baseSortingOrder;
    private float nextToggleTime;

    public bool IsOpen => isOpen.Value;
    public bool SwungPositive => swingPositive.Value;

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
            swingPositive.Value = startsSwungPositive;
        }

        isOpen.OnValueChanged += OnOpenChanged;
        swingPositive.OnValueChanged += OnSwingChanged;
        ApplyState(instant: true);
    }

    public override void OnNetworkDespawn()
    {
        isOpen.OnValueChanged -= OnOpenChanged;
        swingPositive.OnValueChanged -= OnSwingChanged;
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
            if (pushPlayersOnClose) ServerPushPlayersClear();
            isOpen.Value = false;
            return;
        }

        // Opening: the leaf swings away from whoever opened it. An UpDown door
        // opened from below swings up; a LeftRight door opened from the left
        // swings right.
        bool swingsPositive = OpenerIsOnNegativeSide(clientId);

        if (logSwing)
        {
            float doorCenter = DoorCenterOnAxis();
            float playerCoord = PlayerCoordOf(clientId);
            string negative = orientation == Orientation.UpDown ? "BELOW" : "LEFT OF";
            string positive = orientation == Orientation.UpDown ? "ABOVE" : "RIGHT OF";
            string swing = orientation == Orientation.UpDown
                ? (swingsPositive ? "UP" : "DOWN")
                : (swingsPositive ? "RIGHT" : "LEFT");

            Debug.Log($"[Door] {name} ({orientation}): player {playerCoord:0.00} vs door center " +
                      $"{doorCenter:0.00} -> opener is {(swingsPositive ? negative : positive)} " +
                      $"the door, swinging {swing}. SwingPositive = {swingsPositive}", this);
        }

        swingPositive.Value = swingsPositive;
        isOpen.Value = true;
    }

    /// <summary>
    /// Server-side. Moves anyone standing in the doorway out along the door's
    /// axis before it turns solid again.
    ///
    /// Left to physics, depenetration exits by the shortest overlap, which for
    /// a tall vertical door is up or down — straight into the wall. Pushing
    /// deliberately along the axis the door spans always lands in open floor.
    /// </summary>
    private void ServerPushPlayersClear()
    {
        if (blocker == null) return;

        Bounds doorway = blocker.bounds;
        Collider2D[] overlaps = Physics2D.OverlapBoxAll(doorway.center, doorway.size, 0f, playerLayers);

        foreach (Collider2D overlap in overlaps)
        {
            var playerObject = overlap.GetComponent<NetworkObject>();
            if (playerObject == null || !playerObject.IsPlayerObject) continue;

            Vector3 position = playerObject.transform.position;

            // Push along the axis the player walks THROUGH the door on — up or
            // down through an UpDown door, left or right through a LeftRight
            // one — toward whichever side they are already nearer. Pushing
            // along the doorway's span instead would shove them into the wall
            // the door sits in.
            if (orientation == Orientation.UpDown)
            {
                bool towardNegative = position.y < doorway.center.y;
                position.y = towardNegative
                    ? doorway.min.y - pushClearance
                    : doorway.max.y + pushClearance;
            }
            else
            {
                bool towardNegative = position.x < doorway.center.x;
                position.x = towardNegative
                    ? doorway.min.x - pushClearance
                    : doorway.max.x + pushClearance;
            }

            playerObject.transform.position = position;

            // The owner simulates its own movement, so a server-side move alone
            // gets overwritten on the next input frame.
            TeleportRpc(position, RpcTarget.Single(playerObject.OwnerClientId, RpcTargetUse.Temp));
        }
    }

    /// <summary>Runs on the pushed player's own client.</summary>
    [Rpc(SendTo.SpecifiedInParams)]
    private void TeleportRpc(Vector3 position, RpcParams rpcParams)
    {
        var nm = NetworkManager.Singleton;
        var playerObject = nm != null ? nm.LocalClient?.PlayerObject : null;
        if (playerObject == null) return;

        playerObject.transform.position = position;

        // Zero any carried velocity, or the player drifts back into the doorway.
        var body = playerObject.GetComponent<Rigidbody2D>();
        if (body != null) body.linearVelocity = Vector2.zero;
    }

    /// <summary>
    /// Server-side. True when the opener stands on the negative side of the
    /// door's axis — below an UpDown door, left of a LeftRight one — which is
    /// the side that pushes the leaf positive. Defaults to false if the player
    /// cannot be resolved.
    /// </summary>
    private bool OpenerIsOnNegativeSide(ulong clientId)
    {
        var nm = NetworkManager.Singleton;
        if (nm == null) return false;

        if (!nm.ConnectedClients.TryGetValue(clientId, out var client)) return false;

        var playerObject = client.PlayerObject;
        if (playerObject == null) return false;

        return PlayerCoordOf(clientId) < DoorCenterOnAxis();
    }

    /// <summary>
    /// The doorway's center along the swing axis. Not transform.position: the
    /// sprite pivots on its hinge at an edge, so the transform sits outside the
    /// opening and every player reads as being on the same side of it.
    /// </summary>
    private float DoorCenterOnAxis()
    {
        Vector3 center = blocker != null ? blocker.bounds.center : transform.position;
        return orientation == Orientation.UpDown ? center.y : center.x;
    }

    /// <summary>Server-side. The opening player's coordinate along the swing axis.</summary>
    private float PlayerCoordOf(ulong clientId)
    {
        var nm = NetworkManager.Singleton;
        if (nm == null || !nm.ConnectedClients.TryGetValue(clientId, out var client)) return float.NaN;
        if (client.PlayerObject == null) return float.NaN;

        Vector3 position = client.PlayerObject.transform.position;
        return orientation == Orientation.UpDown ? position.y : position.x;
    }

    /// <summary>
    /// Reports what the Animator actually settles into, which is not always the
    /// state the parameters imply — a state can hold the wrong clip.
    /// </summary>
    private System.Collections.IEnumerator LogAnimatorStateNextFrame()
    {
        yield return null;

        if (animator == null) yield break;

        AnimatorStateInfo info = animator.GetCurrentAnimatorStateInfo(0);
        AnimatorClipInfo[] clips = animator.GetCurrentAnimatorClipInfo(0);
        string clipName = clips.Length > 0 && clips[0].clip != null ? clips[0].clip.name : "none";

        Debug.Log($"[Door] {name}: IsOpen={isOpen.Value}, SwingPositive={swingPositive.Value} -> " +
                  $"Animator playing clip '{clipName}' (state hash {info.shortNameHash})", this);
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
            // Only an UpDown leaf swinging down comes toward the camera; a
            // LeftRight leaf stays in the wall plane whichever way it opens.
            bool drawInFront = orientation == Orientation.UpDown
                               && isOpen.Value && !swingPositive.Value;
            leafRenderer.sortingOrder = drawInFront
                ? baseSortingOrder + swingDownSortingBoost
                : baseSortingOrder;
        }

        if (animator == null) return;

        // Set the swing first: the transition out of Closed reads it, so it
        // has to be correct before IsOpen flips.
        animator.SetBool(SwingPositiveParameter, swingPositive.Value);
        animator.SetBool(OpenParameter, isOpen.Value);

        // Skip the animation for the state the door spawns in.
        if (instant) animator.Update(0f);

        // A frame later the transition has resolved, so the clip name is real.
        if (logSwing && !instant && isActiveAndEnabled)
        {
            StartCoroutine(LogAnimatorStateNextFrame());
        }
    }
}
