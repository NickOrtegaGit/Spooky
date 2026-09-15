using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Free 360-degree flashlight aim from the mouse. Lives on the player; the
/// held flashlight reads the replicated angle.
///
/// The owning client computes the angle from its own camera and sends it to
/// the server, throttled — a mouse moves every frame and does not need to be
/// replicated that often.
/// </summary>
public class FlashlightAim : NetworkBehaviour
{
    [SerializeField] private float sendIntervalSeconds = 0.05f;   // 20 Hz
    [SerializeField] private float sendAngleThreshold = 2f;       // degrees

    [Header("Relative aim")]
    [Tooltip("Degrees of beam rotation per pixel of horizontal mouse movement.")]
    [SerializeField] private float degreesPerPixel = 0.4f;
    [Tooltip("Ignore jitter below this many pixels of movement.")]
    [SerializeField] private float movementDeadzone = 0.5f;

    private readonly NetworkVariable<float> aimAngle = new NetworkVariable<float>(-90f);

    private float lastSentAngle;
    private float nextSendTime;
    private float localAngle = -90f;

    /// <summary>Replicated aim, in degrees. -90 is straight down.</summary>
    public float AimAngle => aimAngle.Value;

    /// <summary>
    /// Server only. Point the aim at a direction — used on pickup so the beam
    /// starts where the player is facing rather than wherever the mouse is.
    /// </summary>
    public void ServerSetAimFromDirection(Vector2 direction)
    {
        if (!IsServer || direction == Vector2.zero) return;
        aimAngle.Value = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
    }

    public override void OnNetworkSpawn()
    {
        localAngle = aimAngle.Value;
        lastSentAngle = localAngle;

        // A seed from the server (e.g. on pickup) must not be undone by the
        // client's own running angle.
        aimAngle.OnValueChanged += (_, current) =>
        {
            if (!IsOwner) return;
            if (Mathf.Abs(Mathf.DeltaAngle(localAngle, current)) > 1f) localAngle = current;
        };
    }

    private void Update()
    {
        if (!IsOwner || Mouse.current == null) return;

        // Relative aiming: the mouse is a dial, not a pointer. A still mouse
        // leaves the beam exactly where it was.
        Vector2 delta = Mouse.current.delta.ReadValue();
        if (delta.sqrMagnitude < movementDeadzone * movementDeadzone) return;

        // Horizontal movement turns the beam; vertical adds to it so circular
        // wrist motion sweeps naturally.
        float turn = (delta.x + delta.y) * degreesPerPixel;

        localAngle = Mathf.Repeat(localAngle + turn, 360f);

        bool moved = Mathf.Abs(Mathf.DeltaAngle(lastSentAngle, localAngle)) >= sendAngleThreshold;
        if (!moved || Time.time < nextSendTime) return;

        lastSentAngle = localAngle;
        nextSendTime = Time.time + sendIntervalSeconds;
        SubmitAimServerRpc(localAngle);
    }

    [Rpc(SendTo.Server)]
    private void SubmitAimServerRpc(float angle)
    {
        aimAngle.Value = angle;
    }
}
