using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// Fades a chunk of overhead art — wall tops, roof edges — while the local
/// player is underneath it, so walking behind something does not hide the
/// player from themselves.
///
/// Purely local presentation. Deliberately driven by the LOCAL PLAYER only:
/// the monster must stay concealed behind overhead art, or hiding places stop
/// working. Nothing here is networked.
///
/// Put this on a GameObject with a trigger Collider2D covering the walkable
/// area the art overlaps.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class OverheadRegion : MonoBehaviour
{
    [Tooltip("Renderers to fade. Usually one overhead Tilemap, but any " +
             "SpriteRenderers placed by hand work too.")]
    [SerializeField] private List<Renderer> fadeTargets = new List<Renderer>();

    [Tooltip("Alpha while the local player is underneath.")]
    [SerializeField, Range(0f, 1f)] private float fadedAlpha = 0.3f;

    [Tooltip("Seconds to fade in or out.")]
    [SerializeField] private float fadeSeconds = 0.2f;

    private readonly List<Color> originalColors = new List<Color>();

    private Collider2D region;
    private Transform localPlayer;
    private float currentAlpha = 1f;
    private bool playerInside;

    /// <summary>Inspector-visible state, so a region that is not fading can be diagnosed.</summary>
    public bool PlayerInside => playerInside;
    public bool HasLocalPlayer => localPlayer != null;
    public float CurrentAlpha => currentAlpha;

    private void Awake()
    {
        region = GetComponent<Collider2D>();
        region.isTrigger = true;

        if (fadeTargets.Count == 0)
        {
            Debug.LogWarning($"OverheadRegion '{name}' has no fade targets; it will do nothing. " +
                             "Drag the Overhead tilemap into Fade Targets.", this);
        }

        // Tilemaps expose tint through TilemapRenderer's material, but the
        // Tilemap component's color is what actually drives it.
        foreach (Renderer target in fadeTargets)
        {
            originalColors.Add(GetColor(target));
        }
    }

    private void Update()
    {
        ResolveLocalPlayer();

        playerInside = localPlayer != null
            && region.OverlapPoint(localPlayer.position);

        float target = playerInside ? fadedAlpha : 1f;

        currentAlpha = fadeSeconds > 0f
            ? Mathf.MoveTowards(currentAlpha, target, Time.deltaTime / fadeSeconds)
            : target;

        ApplyAlpha(currentAlpha);
    }

    /// <summary>
    /// The local player spawns after this component and can respawn, so the
    /// reference is re-resolved rather than cached once.
    /// </summary>
    private void ResolveLocalPlayer()
    {
        if (localPlayer != null) return;

        var nm = NetworkManager.Singleton;
        var playerObject = nm != null && nm.IsClient ? nm.LocalClient?.PlayerObject : null;
        if (playerObject != null) localPlayer = playerObject.transform;
    }

    private void ApplyAlpha(float alpha)
    {
        for (int i = 0; i < fadeTargets.Count; i++)
        {
            Renderer target = fadeTargets[i];
            if (target == null) continue;

            Color color = i < originalColors.Count ? originalColors[i] : Color.white;
            color.a = alpha;
            SetColor(target, color);
        }
    }

    private static Color GetColor(Renderer target)
    {
        if (target == null) return Color.white;

        var tilemap = target.GetComponent<Tilemap>();
        if (tilemap != null) return tilemap.color;

        var sprite = target as SpriteRenderer;
        return sprite != null ? sprite.color : Color.white;
    }

    private static void SetColor(Renderer target, Color color)
    {
        var tilemap = target.GetComponent<Tilemap>();
        if (tilemap != null)
        {
            tilemap.color = color;
            return;
        }

        if (target is SpriteRenderer sprite) sprite.color = color;
    }

    private void OnDrawGizmosSelected()
    {
        var collider2d = GetComponent<Collider2D>();
        if (collider2d == null) return;

        Gizmos.color = new Color(0.3f, 0.8f, 1f, 0.25f);
        Bounds bounds = collider2d.bounds;
        Gizmos.DrawCube(bounds.center, bounds.size);
    }
}
