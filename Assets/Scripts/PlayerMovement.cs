using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerMovement : NetworkBehaviour
{
    [SerializeField] private float moveSpeed = 5f;

    private Rigidbody2D rb;
    private PlayerState state;
    private PlayerAnimator playerAnimator;
    private Vector2 inputDirection;

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
        if (!IsOwner) return;
        if (state != null && state.IsCaught) return;

        // Committed while doing a task — that is the vulnerability.
        if (MinigameRunner.Instance != null && MinigameRunner.Instance.IsBusy)
        {
            if (inputDirection != Vector2.zero)
            {
                inputDirection = Vector2.zero;
                SubmitInputServerRpc(inputDirection, facingIntent);
            }
            return;
        }

        var keyboard = Keyboard.current;
        if (keyboard == null) return;

        float x = 0f;
        float y = 0f;
        if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) x -= 1f;
        if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) x += 1f;
        if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed) y -= 1f;
        if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed) y += 1f;

        Vector2 newInput = new Vector2(x, y).normalized;

        UpdateFacingIntent(x, y);

        if (newInput != inputDirection)
        {
            inputDirection = newInput;
            SubmitInputServerRpc(inputDirection, facingIntent);
        }
        else if (playerAnimator != null && playerAnimator.Facing != facingIntent && facingIntent != Vector2.zero)
        {
            SubmitInputServerRpc(inputDirection, facingIntent);
        }
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
    private void SubmitInputServerRpc(Vector2 direction, Vector2 facing)
    {
        inputDirection = direction;
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

        rb.linearVelocity = inputDirection * moveSpeed;
    }
}
