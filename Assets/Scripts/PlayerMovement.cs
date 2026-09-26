using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerMovement : NetworkBehaviour
{
    [SerializeField] private float moveSpeed = 5f;

    [Header("Sprint")]
    [SerializeField] private float sprintSpeed = 8f;

    [Tooltip("How fast sprint velocity turns toward new input, in units per " +
             "second squared. Lower means more committed to the current " +
             "heading — leaning into a turn instead of snapping.")]
    [SerializeField] private float sprintTurnAcceleration = 18f;

    [Tooltip("How fast sprint velocity builds along the current heading.")]
    [SerializeField] private float sprintAcceleration = 30f;

    private Rigidbody2D rb;
    private PlayerState state;
    private PlayerAnimator playerAnimator;
    private PlayerStamina stamina;
    private Vector2 inputDirection;
    private bool sprintHeld;

    // How fast this object is actually moving through the world, measured from
    // the transform. Needed on remote copies, whose rigidbody is Kinematic and
    // reports no velocity of its own.
    private Vector3 lastObservedPosition;
    private float observedSpeed;

    // Facing is decided on the owning client, where key press order is known.
    // First axis held wins and keeps winning until that key is released, so
    // the character does not flip direction mid-diagonal.
    private Vector2 facingIntent = Vector2.down;
    private bool horizontalHeldFirst;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        state = GetComponent<PlayerState>();
        playerAnimator = GetComponent<PlayerAnimator>();
        stamina = GetComponent<PlayerStamina>();
    }

    public override void OnNetworkSpawn()
    {
        // Only the server simulates physics; remote copies are driven by NetworkTransform.
        rb.bodyType = IsServer ? RigidbodyType2D.Dynamic : RigidbodyType2D.Kinematic;

        // The server decides where players start so every client agrees.
        if (IsServer && SpawnManager.Instance != null)
        {
            transform.position = SpawnManager.Instance.GetSpawnPosition();
        }
    }

    private void Update()
    {
        TrackObservedSpeed();

        if (!IsOwner) return;
        if (state != null && state.IsCaught) return;

        // Committed while doing a task — that is the vulnerability.
        if (MinigameRunner.Instance != null && MinigameRunner.Instance.IsBusy)
        {
            if (inputDirection != Vector2.zero || sprintHeld)
            {
                inputDirection = Vector2.zero;
                sprintHeld = false;
                SubmitInputServerRpc(inputDirection, facingIntent, sprintHeld);
            }
            return;
        }

        var keyboard = Keyboard.current;
        if (keyboard == null) return;

        // Drive the local animation from the owner's own view every frame, not
        // just when input changes: stamina can run out mid-sprint, and the
        // animation has to drop then too. The owner can read its own stamina,
        // so this matches what the server will decide.
        UpdateOwnerSprintAnimation();

        float x = 0f;
        float y = 0f;
        if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) x -= 1f;
        if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) x += 1f;
        if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed) y -= 1f;
        if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed) y += 1f;

        Vector2 newInput = new Vector2(x, y).normalized;
        bool newSprint = keyboard.leftShiftKey.isPressed || keyboard.rightShiftKey.isPressed;

        UpdateFacingIntent(x, y);

        if (newInput != inputDirection || newSprint != sprintHeld)
        {
            inputDirection = newInput;
            sprintHeld = newSprint;

            SubmitInputServerRpc(inputDirection, facingIntent, sprintHeld);
        }
        else if (playerAnimator != null && playerAnimator.Facing != facingIntent && facingIntent != Vector2.zero)
        {
            SubmitInputServerRpc(inputDirection, facingIntent, sprintHeld);
        }
    }

    /// <summary>
    /// Owner only. Mirrors the server's sprint decision locally so the
    /// animation switches on the keypress instead of a round trip later —
    /// including refusing to switch when there is no stamina to sprint on.
    /// </summary>
    private void UpdateOwnerSprintAnimation()
    {
        if (playerAnimator == null) return;

        bool hasStamina = stamina == null || !stamina.IsExhausted && stamina.Normalized > 0f;
        bool sprinting = sprintHeld && inputDirection != Vector2.zero && hasStamina;

        playerAnimator.OwnerSetSprinting(sprinting);
    }

    /// <summary>
    /// Measures real movement through the world, for copies whose rigidbody
    /// carries no velocity of its own.
    /// </summary>
    private void TrackObservedSpeed()
    {
        if (Time.deltaTime <= 0f) return;

        observedSpeed = (transform.position - lastObservedPosition).magnitude / Time.deltaTime;
        lastObservedPosition = transform.position;
    }

    /// <summary>
    /// True when actually moving at or near full sprint speed — not merely
    /// holding shift. Read on both the owner and the server: the owner to
    /// decide whether to ask, the server to check the claim against the
    /// rigidbody it owns.
    /// </summary>
    public bool IsAtBurstSpeed(float fraction)
    {
        if (!sprintHeld) return false;

        float threshold = sprintSpeed * fraction;

        // On the server the rigidbody is the truth. On a remote client it is
        // Kinematic and driven by NetworkTransform, so its velocity reads zero
        // — measure how far the transform actually moved instead.
        float speedSqr = IsServer
            ? rb.linearVelocity.sqrMagnitude
            : observedSpeed * observedSpeed;

        return speedSqr >= threshold * threshold;
    }

    private void UpdateFacingIntent(float x, float y)
    {
        bool horizontal = Mathf.Abs(x) > 0.01f;
        bool vertical = Mathf.Abs(y) > 0.01f;

        if (!horizontal && !vertical) return;   // keep the last facing when idle

        // Whichever axis started first stays in charge until it is released.
        if (horizontal && !vertical) horizontalHeldFirst = true;
        else if (vertical && !horizontal) horizontalHeldFirst = false;

        bool useHorizontal = horizontalHeldFirst ? horizontal : !vertical;

        facingIntent = useHorizontal
            ? new Vector2(Mathf.Sign(x), 0f)
            : new Vector2(0f, Mathf.Sign(y));
    }

    [Rpc(SendTo.Server)]
    private void SubmitInputServerRpc(Vector2 direction, Vector2 facing, bool sprinting)
    {
        inputDirection = direction;
        sprintHeld = sprinting;
        if (playerAnimator != null) playerAnimator.ServerSetFacing(facing);
    }

    private void FixedUpdate()
    {
        if (!IsServer) return;

        // Enforced server-side too: a client that ignores its own caught flag
        // still does not get to move.
        if (state != null && state.IsCaught)
        {
            inputDirection = Vector2.zero;
            rb.linearVelocity = Vector2.zero;
            return;
        }

        bool wantsToSprint = sprintHeld && inputDirection != Vector2.zero;
        bool sprinting = wantsToSprint && (stamina == null || stamina.ServerCanSprint());

        if (stamina != null) stamina.ServerTick(sprinting, Time.fixedDeltaTime);
        if (playerAnimator != null) playerAnimator.ServerSetSprinting(sprinting);

        if (!sprinting)
        {
            // Walking is direct: instant response, turns on a dime.
            rb.linearVelocity = inputDirection * moveSpeed;
            return;
        }

        // Sprinting steers instead of snapping. Velocity accelerates toward
        // the target, and turning is slower than accelerating, so changing
        // heading at speed costs distance — commit to a direction and you are
        // committed to it.
        Vector2 target = inputDirection * sprintSpeed;
        Vector2 current = rb.linearVelocity;

        Vector2 heading = current.sqrMagnitude > 0.01f ? current.normalized : inputDirection;

        // Split the needed change into "along where I am already going" and
        // "across it", then let each approach its target at its own rate. The
        // across part is the turn, and it is the slow one.
        Vector2 delta = target - current;
        Vector2 along = Vector2.Dot(delta, heading) * heading;
        Vector2 across = delta - along;

        Vector2 alongStep = Vector2.ClampMagnitude(
            along, sprintAcceleration * Time.fixedDeltaTime);
        Vector2 acrossStep = Vector2.ClampMagnitude(
            across, sprintTurnAcceleration * Time.fixedDeltaTime);

        rb.linearVelocity = Vector2.ClampMagnitude(
            current + alongStep + acrossStep, sprintSpeed);
    }
}
