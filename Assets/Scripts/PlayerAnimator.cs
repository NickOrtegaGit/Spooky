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

    [SerializeField] private float movingThreshold = 0.05f;

    // Server writes, everyone reads. Facing is kept separate from velocity so
    // the character keeps facing the last direction walked when it stops.
    private readonly NetworkVariable<Vector2> facing =
        new NetworkVariable<Vector2>(Vector2.down);
    private readonly NetworkVariable<bool> isMoving =
        new NetworkVariable<bool>(false);

    private Animator animator;
    private Rigidbody2D rb;

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
    }

    private void FixedUpdate()
    {
        if (!IsServer) return;

        Vector2 velocity = rb.linearVelocity;
        bool moving = velocity.sqrMagnitude > movingThreshold * movingThreshold;

        isMoving.Value = moving;

        if (!moving) return;

        // Snap to the dominant axis: 4-directional art has no diagonals.
        facing.Value = Mathf.Abs(velocity.x) > Mathf.Abs(velocity.y)
            ? new Vector2(Mathf.Sign(velocity.x), 0f)
            : new Vector2(0f, Mathf.Sign(velocity.y));
    }
}
