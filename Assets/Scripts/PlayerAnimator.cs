using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Drives the Animator from replicated movement, so every client sees every
/// player animate — not just their own.
/// </summary>
[RequireComponent(typeof(Animator))]
public class PlayerAnimator : NetworkBehaviour
{
    private static readonly int MoveXHash = Animator.StringToHash("MoveX");
    private static readonly int MoveYHash = Animator.StringToHash("MoveY");
    private static readonly int IsMovingHash = Animator.StringToHash("IsMoving");
    private static readonly int IsSprintingHash = Animator.StringToHash("IsSprinting");

    [SerializeField] private float movingThreshold = 0.05f;

    // Server writes, everyone reads. Facing is kept separate from velocity so
    // the character keeps facing the last direction walked when it stops.
    private readonly NetworkVariable<Vector2> facing =
        new NetworkVariable<Vector2>(Vector2.down);
    private readonly NetworkVariable<bool> isMoving =
        new NetworkVariable<bool>(false);
    private readonly NetworkVariable<bool> isSprinting =
        new NetworkVariable<bool>(false);

    private Animator animator;
    private Rigidbody2D rb;

    // The owner's own view of whether it is sprinting, ahead of the server's.
    private bool ownerSprinting;

    /// <summary>Replicated facing direction, for anything that needs to know
    /// which way the player is pointing (item drops, future item use).</summary>
    public Vector2 Facing => facing.Value;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        rb = GetComponent<Rigidbody2D>();
    }

    private void Update()
    {
        // Applied on every instance, from replicated values.
        animator.SetFloat(MoveXHash, facing.Value.x);
        animator.SetFloat(MoveYHash, facing.Value.y);
        animator.SetBool(IsMovingHash, isMoving.Value);

        // The owner animates from its own input rather than waiting for the
        // server's value to come back. Movement hides that round trip behind
        // acceleration; an animation switch does not, so the delay shows.
        animator.SetBool(IsSprintingHash, IsOwner ? ownerSprinting : isSprinting.Value);
    }

    /// <summary>
    /// Owner only. Called from PlayerMovement the moment sprint input changes,
    /// so the local animation does not wait on the server.
    /// </summary>
    public void OwnerSetSprinting(bool value)
    {
        if (!IsOwner) return;
        ownerSprinting = value;
    }

    private void FixedUpdate()
    {
        if (!IsServer) return;

        Vector2 velocity = rb.linearVelocity;
        bool moving = velocity.sqrMagnitude > movingThreshold * movingThreshold;

        isMoving.Value = moving;

        // Facing itself is set by the owning client via ServerSetFacing —
        // only the client knows which key was pressed first.
    }

    /// <summary>Server only. Called from PlayerMovement's fixed step.</summary>
    public void ServerSetSprinting(bool value)
    {
        if (!IsServer) return;
        isSprinting.Value = value;
    }

    /// <summary>Server only. Called from PlayerMovement's input RPC.</summary>
    public void ServerSetFacing(Vector2 value)
    {
        if (!IsServer || value == Vector2.zero) return;
        facing.Value = value;
    }
}
