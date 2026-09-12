using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Replicated per-player status. What *happens* to a caught player is not
/// decided yet — respawn, spectator, and downed-and-revivable all build on
/// this same flag.
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class PlayerState : NetworkBehaviour
{
    [SerializeField] private Color caughtTint = new Color(0.45f, 0.45f, 0.5f);

    private readonly NetworkVariable<bool> isCaught = new NetworkVariable<bool>(false);

    private SpriteRenderer spriteRenderer;
    private Color normalColor;

    public bool IsCaught => isCaught.Value;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        normalColor = spriteRenderer.color;
    }

    public override void OnNetworkSpawn()
    {
        isCaught.OnValueChanged += OnCaughtChanged;
        ApplyTint(isCaught.Value);
    }

    public override void OnNetworkDespawn()
    {
        isCaught.OnValueChanged -= OnCaughtChanged;
    }

    /// <summary>Server only. Called by the monster on contact.</summary>
    public void SetCaught(bool caught)
    {
        if (!IsServer) return;
        isCaught.Value = caught;
    }

    private void OnCaughtChanged(bool previous, bool current) => ApplyTint(current);

    private void ApplyTint(bool caught)
    {
        spriteRenderer.color = caught ? caughtTint : normalColor;
    }
}
