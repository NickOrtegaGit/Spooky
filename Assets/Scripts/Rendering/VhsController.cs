using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Drives the VHS effect's strength from what is happening to the local
/// player. Purely visual and entirely local — every client computes its own
/// dread level, nothing replicates.
///
/// Put this on one object in the scene and assign the same material the
/// VhsRenderFeature uses.
/// </summary>
public class VhsController : MonoBehaviour
{
    private static readonly int StrengthId = Shader.PropertyToID("_Strength");

    [Header("Material")]
    [Tooltip("The material using Spooky/VhsDistortion — the same one on the Renderer Feature.")]
    [SerializeField] private Material vhsMaterial;

    [Header("Baseline")]
    [Tooltip("Always-on strength when nothing is happening.")]
    [SerializeField, Range(0f, 1f)] private float baseStrength = 0.25f;

    [Header("Monster proximity")]
    [Tooltip("Beyond this distance the monster adds nothing.")]
    [SerializeField] private float farDistance = 12f;
    [Tooltip("At or inside this distance the monster's contribution is at full.")]
    [SerializeField] private float nearDistance = 3f;
    [Tooltip("How much strength a monster at nearDistance adds.")]
    [SerializeField, Range(0f, 1f)] private float proximityStrength = 0.6f;
    [Tooltip("Shapes the falloff. >1 keeps it calm until the monster is close.")]
    [SerializeField, Range(0.25f, 4f)] private float proximityFalloff = 2f;
    [Tooltip("Off: distance alone drives it, through walls. On: only when actually visible.")]
    [SerializeField] private bool requireLineOfSight = false;
    [SerializeField] private LayerMask sightBlockers;

    [Header("States")]
    [Tooltip("Added while the local player is caught.")]
    [SerializeField, Range(0f, 1f)] private float caughtStrength = 0.9f;
    [Tooltip("Added while the local player is in a task panel.")]
    [SerializeField, Range(0f, 1f)] private float taskStrength = 0.35f;

    [Header("Response")]
    [Tooltip("Seconds to ease toward a new strength. 0 snaps.")]
    [SerializeField] private float smoothing = 0.4f;

    [Header("Debug")]
    [Tooltip("Ignore gameplay and hold the slider below — for dialing the look in play mode.")]
    [SerializeField] private bool overrideStrength = false;
    [SerializeField, Range(0f, 1f)] private float overrideValue = 0.5f;

    private Transform localPlayer;
    private PlayerState localPlayerState;
    private Transform monster;
    private float current;

    /// <summary>What the effect is actually at right now. Read-only, for debugging.</summary>
    public float CurrentStrength => current;

    private void OnEnable()
    {
        current = baseStrength;
        Apply(current);
    }

    private void OnDisable()
    {
        // Leave the material clean — it is an asset, so edits persist.
        Apply(0f);
    }

    private void Update()
    {
        float target = overrideStrength ? overrideValue : ComputeTarget();

        current = smoothing > 0f
            ? Mathf.Lerp(current, target, 1f - Mathf.Exp(-Time.deltaTime / smoothing))
            : target;

        Apply(current);
    }

    private float ComputeTarget()
    {
        ResolveReferences();

        float strength = baseStrength;

        strength += ProximityContribution();

        if (localPlayerState != null && localPlayerState.IsCaught)
        {
            strength += caughtStrength;
        }

        if (MinigameRunner.Instance != null && MinigameRunner.Instance.IsBusy)
        {
            strength += taskStrength;
        }

        return Mathf.Clamp01(strength);
    }

    private float ProximityContribution()
    {
        if (monster == null || localPlayer == null || proximityStrength <= 0f) return 0f;

        float distance = Vector2.Distance(localPlayer.position, monster.position);
        if (distance >= farDistance) return 0f;

        if (requireLineOfSight && !CanSeeMonster(distance)) return 0f;

        // 0 at farDistance, 1 at nearDistance and closer.
        float t = Mathf.InverseLerp(farDistance, nearDistance, distance);
        return Mathf.Pow(Mathf.Clamp01(t), proximityFalloff) * proximityStrength;
    }

    private bool CanSeeMonster(float distance)
    {
        Vector2 origin = localPlayer.position;
        Vector2 direction = ((Vector2)monster.position - origin).normalized;
        return Physics2D.Raycast(origin, direction, distance, sightBlockers).collider == null;
    }

    /// <summary>
    /// Both the local player and the monster spawn after this component, and
    /// the player can respawn, so these are re-resolved rather than cached once.
    /// </summary>
    private void ResolveReferences()
    {
        if (localPlayer == null)
        {
            var nm = NetworkManager.Singleton;
            var playerObject = nm != null && nm.IsClient ? nm.LocalClient?.PlayerObject : null;

            if (playerObject != null)
            {
                localPlayer = playerObject.transform;
                localPlayerState = playerObject.GetComponent<PlayerState>();
            }
        }

        if (monster == null)
        {
            var ai = FindFirstObjectByType<MonsterAI>();
            if (ai != null) monster = ai.transform;
        }
    }

    private void Apply(float strength)
    {
        if (vhsMaterial == null) return;
        vhsMaterial.SetFloat(StrengthId, strength);
    }

    private void OnValidate()
    {
        // Keep the band ordered so the falloff math cannot invert.
        nearDistance = Mathf.Max(0f, nearDistance);
        farDistance = Mathf.Max(nearDistance + 0.1f, farDistance);
    }

    private void OnDrawGizmosSelected()
    {
        Transform origin = localPlayer != null ? localPlayer : transform;

        Gizmos.color = new Color(1f, 0.4f, 0.4f);
        Gizmos.DrawWireSphere(origin.position, nearDistance);
        Gizmos.color = new Color(0.4f, 0.4f, 1f);
        Gizmos.DrawWireSphere(origin.position, farDistance);
    }
}
