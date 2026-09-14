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

    [Header("Held sprites — one per direction")]
    [SerializeField] private Sprite heldUp;
    [SerializeField] private Sprite heldDown;
    [SerializeField] private Sprite heldLeft;
    [SerializeField] private Sprite heldRight;

    [Header("Held offsets — where the hand is for each facing")]
    [SerializeField] private Vector2 offsetUp = new Vector2(-0.2f, 0.1f);
    [SerializeField] private Vector2 offsetDown = new Vector2(0.2f, -0.1f);
    [SerializeField] private Vector2 offsetLeft = new Vector2(-0.25f, 0f);
    [SerializeField] private Vector2 offsetRight = new Vector2(0.25f, 0f);

    [Header("Sorting")]
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

    private FlashlightAim FindHolderAim()
    {
        if (NetworkManager.Singleton == null) return null;
        if (!NetworkManager.Singleton.ConnectedClients.TryGetValue(HolderClientId, out var client)) return null;
        if (client.PlayerObject == null) return null;
        return client.PlayerObject.GetComponent<FlashlightAim>();
    }

    /// <summary>
    /// The body is still four-directional, so pick the nearest of four held
    /// sprites to the free aim angle, and orbit the item around the player.
    /// </summary>
    private void ApplySpriteForAim(float angle)
    {
        var itemSprite = ItemSprite;
        if (itemSprite == null) return;

        float wrapped = Mathf.Repeat(angle, 360f);

        bool aimRight = wrapped < 45f || wrapped >= 315f;
        bool aimUp = wrapped >= 45f && wrapped < 135f;
        bool aimLeft = wrapped >= 135f && wrapped < 225f;

        Sprite wanted =
            aimRight ? heldRight :
            aimUp ? heldUp :
            aimLeft ? heldLeft :
            heldDown;

        if (wanted != null) itemSprite.sprite = wanted;

        Vector2 offset =
            aimRight ? offsetRight :
            aimUp ? offsetUp :
            aimLeft ? offsetLeft :
            offsetDown;

        itemSprite.transform.localPosition = offset;

        // Behind the body when aiming away from the camera or across it.
        itemSprite.sortingOrder = (aimLeft || aimUp)
            ? orderBehindPlayer
            : orderInFrontOfPlayer;

        itemSprite.flipX = false;
        itemSprite.transform.localRotation = Quaternion.identity;
    }

    private void OnToggled(bool previous, bool current) => ApplyBeam(current);

    private void ApplyBeam(bool on)
    {
        if (beam != null) beam.enabled = on && IsHeld;
    }
}
