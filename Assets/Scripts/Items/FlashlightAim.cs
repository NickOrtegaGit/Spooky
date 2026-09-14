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

    private readonly NetworkVariable<float> aimAngle = new NetworkVariable<float>(-90f);

    private float lastSentAngle;
    private float nextSendTime;
    private Camera cachedCamera;

    /// <summary>Replicated aim, in degrees. -90 is straight down.</summary>
    public float AimAngle => aimAngle.Value;

    private void Update()
    {
        if (!IsOwner) return;

        Camera cam = ResolveCamera();
        if (cam == null || Mouse.current == null) return;

        Vector2 screenPos = Mouse.current.position.ReadValue();
        Vector3 worldPos = cam.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, -cam.transform.position.z));

        Vector2 toMouse = (Vector2)worldPos - (Vector2)transform.position;
        if (toMouse.sqrMagnitude < 0.0001f) return;

        float angle = Mathf.Atan2(toMouse.y, toMouse.x) * Mathf.Rad2Deg;

        bool moved = Mathf.Abs(Mathf.DeltaAngle(lastSentAngle, angle)) >= sendAngleThreshold;
        if (!moved || Time.time < nextSendTime) return;

        lastSentAngle = angle;
        nextSendTime = Time.time + sendIntervalSeconds;
        SubmitAimServerRpc(angle);
    }

    private Camera ResolveCamera()
    {
        if (cachedCamera == null) cachedCamera = Camera.main;
        return cachedCamera;
    }

    [Rpc(SendTo.Server)]
    private void SubmitAimServerRpc(float angle)
    {
        aimAngle.Value = angle;
    }
}
