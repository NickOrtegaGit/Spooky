using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerMovement : NetworkBehaviour
{
    [SerializeField] private float moveSpeed = 5f;

    private Rigidbody2D rb;
    private Vector2 inputDirection;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
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

        var keyboard = Keyboard.current;
        if (keyboard == null) return;

        float x = 0f;
        float y = 0f;
        if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) x -= 1f;
        if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) x += 1f;
        if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed) y -= 1f;
        if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed) y += 1f;

        Vector2 newInput = new Vector2(x, y).normalized;

        if (newInput != inputDirection)
        {
            inputDirection = newInput;
            SubmitInputServerRpc(inputDirection);
        }
    }

    [Rpc(SendTo.Server)]
    private void SubmitInputServerRpc(Vector2 direction)
    {
        inputDirection = direction;
    }

    private void FixedUpdate()
    {
        if (!IsServer) return;

        rb.linearVelocity = inputDirection * moveSpeed;
    }
}
