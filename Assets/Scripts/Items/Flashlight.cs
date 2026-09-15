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

    [Header("Held orbit")]
    [Tooltip("Distance from the player's centre that the flashlight orbits at.")]
    [SerializeField] private float orbitRadius = 0.35f;
    [SerializeField] private int orderInFrontOfPlayer = 105;
    [SerializeField] private int orderBehindPlayer = 95;

    private readonly NetworkVariable<bool> isOn = new NetworkVariable<bool>(false);

    private FlashlightAim holderAim;
    private PlayerLightControl suppressedLight;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        isOn.OnValueChanged += OnToggled;
        ApplyBeam(isOn.Value);
    }

    /// <summary>Held state changed — the beam only shows while carried.</summary>
    protected override void OnHeldStateChanged(bool held)
    {
        // Picking it up switches it on; carrying it unlit is a trap state.
        if (held && IsServer)
        {
            isOn.Value = true;
            SeedAimFromHolderFacing();
        }

        ApplyBeam(isOn.Value);
        ApplyLightSuppression(held);
    }

    /// <summary>
    /// The drawback: carrying the flashlight kills the holder's own light
    /// bubble, on or off. Reach in one direction instead of awareness in all.
    /// </summary>
    private void ApplyLightSuppression(bool held)
    {
        if (!held)
        {
            if (suppressedLight != null)
            {
                suppressedLight.RemoveSuppressor();
                suppressedLight = null;
            }
            return;
        }

        if (suppressedLight != null) return;

        var holder = FindHolderLight();
        if (holder == null) return;

        suppressedLight = holder;
        suppressedLight.AddSuppressor();
    }

    private PlayerLightControl FindHolderLight()
    {
        if (NetworkManager.Singleton == null) return null;
        if (!NetworkManager.Singleton.ConnectedClients.TryGetValue(HolderClientId, out var client)) return null;
        if (client.PlayerObject == null) return null;
        return client.PlayerObject.GetComponent<PlayerLightControl>();
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
        if (!IsHeld) return;

        if (holderAim == null || holderAim.OwnerClientId != HolderClientId)
        {
            holderAim = FindHolderAim();
        }

        if (holderAim == null) return;

        float targetAngle = holderAim.AimAngle;

        if (beam != null)
        {
            float current = beam.transform.eulerAngles.z;
            float next = Mathf.MoveTowardsAngle(current, targetAngle, beamTurnSpeed * Time.deltaTime);
            beam.transform.rotation = Quaternion.Euler(0f, 0f, next);
        }

        ApplySpriteForAim(targetAngle);
    }

    /// <summary>
    /// Start the beam pointing where the player is already facing, rather
    /// than snapping to wherever the mouse happens to sit.
    /// </summary>
    private void SeedAimFromHolderFacing()
    {
        if (NetworkManager.Singleton == null) return;
        if (!NetworkManager.Singleton.ConnectedClients.TryGetValue(HolderClientId, out var client)) return;
        if (client.PlayerObject == null) return;

        var animator = client.PlayerObject.GetComponent<PlayerAnimator>();
        var aim = client.PlayerObject.GetComponent<FlashlightAim>();
        if (animator == null || aim == null) return;

        aim.ServerSetAimFromDirection(animator.Facing);
    }

    private FlashlightAim FindHolderAim()
    {
        if (NetworkManager.Singleton == null) return null;
        if (!NetworkManager.Singleton.ConnectedClients.TryGetValue(HolderClientId, out var client)) return null;
        if (client.PlayerObject == null) return null;
        return client.PlayerObject.GetComponent<FlashlightAim>();
    }

    /// <summary>
    /// One right-facing sprite, orbited around the player and rotated to the
    /// aim angle. No snapping, so turning is continuous.
    /// </summary>
    private void ApplySpriteForAim(float angle)
    {
        var itemSprite = ItemSprite;
        if (itemSprite == null) return;

        float radians = angle * Mathf.Deg2Rad;
        Vector3 orbit = new Vector3(Mathf.Cos(radians), Mathf.Sin(radians), 0f) * orbitRadius;

        itemSprite.transform.localPosition = orbit;
        itemSprite.transform.localRotation = Quaternion.Euler(0f, 0f, angle);

        // Behind the body while aiming away from the camera.
        float wrapped = Mathf.Repeat(angle, 360f);
        bool aimingUp = wrapped > 20f && wrapped < 160f;
        itemSprite.sortingOrder = aimingUp ? orderBehindPlayer : orderInFrontOfPlayer;

        itemSprite.flipX = false;
    }

    private void OnToggled(bool previous, bool current) => ApplyBeam(current);

    private void ApplyBeam(bool on)
    {
        if (beam != null) beam.enabled = on && IsHeld;
    }
}
