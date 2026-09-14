using Unity.Netcode;
using UnityEngine;
using UnityEngine.Rendering.Universal;

/// <summary>
/// First real item. Space toggles the beam; the on/off state replicates so
/// everyone sees it.
/// </summary>
public class Flashlight : CarryableItem
{
    [SerializeField] private Light2D beam;
    [SerializeField] private float beamTurnSpeed = 720f;

    private readonly NetworkVariable<bool> isOn = new NetworkVariable<bool>(false);

    private PlayerAnimator holderAnimator;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        isOn.OnValueChanged += OnToggled;
        ApplyBeam(isOn.Value);
    }

    /// <summary>Held state changed — the beam only shows while carried.</summary>
    protected override void OnHeldStateChanged(bool held)
    {
        ApplyBeam(isOn.Value);
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        isOn.OnValueChanged -= OnToggled;
    }

    public override void Use(ulong clientId)
    {
        if (!IsServer) return;
        if (!IsHeld) return;

        isOn.Value = !isOn.Value;
    }

    /// <summary>A dropped flashlight switches off.</summary>
    public override void ServerDrop(Vector3 position)
    {
        base.ServerDrop(position);
        if (IsServer) isOn.Value = false;
    }

    private void Update()
    {
        if (beam == null || !IsHeld) return;

        // Aim along the holder's replicated facing, so every client sees the
        // beam pointing the same way.
        if (holderAnimator == null || holderAnimator.OwnerClientId != HolderClientId)
        {
            holderAnimator = FindHolderAnimator();
        }

        if (holderAnimator == null) return;

        Vector2 facing = holderAnimator.Facing;
        if (facing.sqrMagnitude < 0.001f) return;

        float targetAngle = Mathf.Atan2(facing.y, facing.x) * Mathf.Rad2Deg;
        float current = beam.transform.eulerAngles.z;
        float next = Mathf.MoveTowardsAngle(current, targetAngle, beamTurnSpeed * Time.deltaTime);
        beam.transform.rotation = Quaternion.Euler(0f, 0f, next);
    }

    private PlayerAnimator FindHolderAnimator()
    {
        if (NetworkManager.Singleton == null) return null;
        if (!NetworkManager.Singleton.ConnectedClients.TryGetValue(HolderClientId, out var client)) return null;
        if (client.PlayerObject == null) return null;
        return client.PlayerObject.GetComponent<PlayerAnimator>();
    }

    private void OnToggled(bool previous, bool current) => ApplyBeam(current);

    private void ApplyBeam(bool on)
    {
        if (beam != null) beam.enabled = on && IsHeld;
    }
}
