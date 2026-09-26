using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Sprint stamina. Server-authoritative, like movement — a client that
/// reported its own stamina could sprint forever, and sprinting away from the
/// monster is exactly the thing worth cheating at.
///
/// The value replicates so the owner's HUD can show it. Other players never
/// see it: being winded is not something to broadcast to the room.
/// </summary>
public class PlayerStamina : NetworkBehaviour
{
    [Header("Capacity")]
    [Tooltip("Seconds of continuous sprinting from full.")]
    [SerializeField] private float maxStamina = 4f;

    [Tooltip("Stamina spent per second of sprinting.")]
    [SerializeField] private float drainPerSecond = 1f;

    [Tooltip("Stamina recovered per second. Below the drain rate, so sprinting " +
             "is rationed rather than tapped.")]
    [SerializeField] private float recoverPerSecond = 0.65f;

    [Header("Recovery")]
    [Tooltip("Seconds after releasing sprint before recovery begins.")]
    [SerializeField] private float recoveryDelay = 0.6f;

    [Tooltip("Longer pause before recovery when stamina is run to empty — the " +
             "cost of not watching the bar.")]
    [SerializeField] private float exhaustedRecoveryDelay = 2.5f;

    [Tooltip("Fraction of the bar that must refill before sprinting is allowed " +
             "again after bottoming out. 1 means a full bar.")]
    [SerializeField, Range(0f, 1f)] private float exhaustedResumeFraction = 1f;

    // Server writes, owner reads for its HUD.
    private readonly NetworkVariable<float> stamina = new NetworkVariable<float>(
        0f, NetworkVariableReadPermission.Owner, NetworkVariableWritePermission.Server);

    // Replicated so the owner's HUD can show the locked-out state differently.
    private readonly NetworkVariable<bool> exhausted = new NetworkVariable<bool>(
        false, NetworkVariableReadPermission.Owner, NetworkVariableWritePermission.Server);

    private float recoverAtTime;

    /// <summary>0..1 for the HUD.</summary>
    public float Normalized => maxStamina > 0f ? stamina.Value / maxStamina : 0f;

    /// <summary>True while locked out after running the bar to empty.</summary>
    public bool IsExhausted => exhausted.Value;

    /// <summary>True when the bar is untouched, so the HUD can hide itself.</summary>
    public bool IsFull => stamina.Value >= maxStamina - 0.001f;

    public override void OnNetworkSpawn()
    {
        if (IsServer) stamina.Value = maxStamina;
    }

    /// <summary>
    /// Server only. True if sprinting is allowed right now — called by
    /// PlayerMovement each fixed step.
    /// </summary>
    public bool ServerCanSprint() => !exhausted.Value && stamina.Value > 0f;

    /// <summary>
    /// Server only. Spends or recovers stamina for this step. Pass whether the
    /// player is actually sprinting, not merely holding the key.
    /// </summary>
    public void ServerTick(bool sprinting, float deltaTime)
    {
        if (!IsServer) return;

        if (sprinting && ServerCanSprint())
        {
            stamina.Value = Mathf.Max(0f, stamina.Value - drainPerSecond * deltaTime);

            // Running it all the way down locks sprinting out until it has
            // refilled, and waits longer before it starts.
            if (stamina.Value <= 0f)
            {
                exhausted.Value = true;
                recoverAtTime = Time.time + exhaustedRecoveryDelay;
            }
            else
            {
                recoverAtTime = Time.time + recoveryDelay;
            }

            return;
        }

        if (Time.time < recoverAtTime) return;

        stamina.Value = Mathf.Min(maxStamina, stamina.Value + recoverPerSecond * deltaTime);

        if (exhausted.Value && Normalized >= exhaustedResumeFraction)
        {
            exhausted.Value = false;
        }
    }
}
