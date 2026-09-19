using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// Fades overhead art — wall tops, roof edges — tile by tile around the local
/// player, so walking behind something never hides the player from themselves.
///
/// One of these covers the whole house: paint art on the overhead tilemap and
/// it works everywhere, with nothing to author per doorway.
///
/// Purely local presentation. Deliberately driven by the LOCAL PLAYER only —
/// the monster must stay concealed behind overhead art or hiding places stop
/// working. Nothing here is networked.
/// </summary>
public class OverheadFader : MonoBehaviour
{
    [Tooltip("The tilemap holding overhead art. No colliders, sorted above the player.")]
    [SerializeField] private Tilemap overhead;

    [Header("Fade area")]
    [Tooltip("Horizontal reach in world units. Wider than tall reads better top-down.")]
    [SerializeField] private float radiusX = 2.5f;

    [Tooltip("Vertical reach in world units.")]
    [SerializeField] private float radiusY = 2f;

    [Tooltip("Shifts the fade area up from the player's feet. Art that hides the " +
             "player is always above them in top-down, so the reach belongs there.")]
    [SerializeField] private float centerOffsetY = 1f;

    [Tooltip("Alpha at the very center of the fade.")]
    [SerializeField, Range(0f, 1f)] private float minAlpha = 0.25f;

    [Tooltip("Shapes the falloff. >1 keeps tiles solid until the player is close.")]
    [SerializeField, Range(0.25f, 4f)] private float falloff = 1.5f;

    [Header("Response")]
    [Tooltip("Seconds for a tile to reach its target alpha. 0 snaps.")]
    [SerializeField] private float fadeSeconds = 0.15f;

    // Every cell currently faded below opaque. Only these need restoring, so
    // the whole tilemap is never walked.
    private readonly HashSet<Vector3Int> fadedCells = new HashSet<Vector3Int>();

    // Cells inside the ellipse this frame — everything else eases back.
    private readonly HashSet<Vector3Int> insideThisFrame = new HashSet<Vector3Int>();
    private readonly List<Vector3Int> restored = new List<Vector3Int>();

    private Transform localPlayer;

    private bool warnedNoTilemap;

    private void Update()
    {
        if (overhead == null)
        {
            if (!warnedNoTilemap)
            {
                warnedNoTilemap = true;
                Debug.LogWarning("OverheadFader has no Overhead tilemap assigned.", this);
            }
            return;
        }

        ResolveLocalPlayer();

        insideThisFrame.Clear();
        if (localPlayer != null) FadeAroundPlayer();

        RestoreCellsOutsideEllipse();
    }

    [Header("Debug")]
    [Tooltip("Logs what the cell scan actually finds, once per second.")]
    [SerializeField] private bool logScan = false;

    [Tooltip("Tint faded cells red instead of fading them. If red shows, SetColor " +
             "reaches the renderer and only alpha is being dropped.")]
    [SerializeField] private bool debugTintRed = false;

    private float nextScanLog;

    private void FadeAroundPlayer()
    {
        Vector3 center = localPlayer.position + new Vector3(0f, centerOffsetY, 0f);

        if (logScan && Time.time >= nextScanLog)
        {
            nextScanLog = Time.time + 1f;

            Vector3Int playerCell = overhead.WorldToCell(center);
            BoundsInt bounds = overhead.cellBounds;

            Debug.Log(
                $"OverheadFader scan — fade center {center}, cell {playerCell}. " +
                $"Tilemap '{overhead.name}' at {overhead.transform.position}, " +
                $"bounds {bounds.min}..{bounds.max}. " +
                $"Faded {fadedCells.Count} cell(s) last frame.", this);
        }

        // Cell range covering the ellipse's bounding box.
        Vector3Int min = overhead.WorldToCell(center + new Vector3(-radiusX, -radiusY));
        Vector3Int max = overhead.WorldToCell(center + new Vector3(radiusX, radiusY));

        for (int x = min.x; x <= max.x; x++)
        {
            for (int y = min.y; y <= max.y; y++)
            {
                var cell = new Vector3Int(x, y, 0);
                if (!overhead.HasTile(cell)) continue;

                Vector3 tileCenter = overhead.GetCellCenterWorld(cell);

                // Normalized elliptical distance: 1 at the edge, 0 at the player.
                float dx = (tileCenter.x - center.x) / Mathf.Max(radiusX, 0.0001f);
                float dy = (tileCenter.y - center.y) / Mathf.Max(radiusY, 0.0001f);
                float distance = Mathf.Sqrt(dx * dx + dy * dy);
                if (distance > 1f) continue;

                float t = Mathf.Pow(1f - distance, falloff);
                float target = Mathf.Lerp(1f, minAlpha, t);

                StepAlpha(cell, target);
                insideThisFrame.Add(cell);
                fadedCells.Add(cell);
            }
        }
    }

    /// <summary>
    /// Eases every faded cell the player has walked away from back to opaque,
    /// dropping it from the tracked set once it gets there.
    /// </summary>
    private void RestoreCellsOutsideEllipse()
    {
        restored.Clear();

        foreach (Vector3Int cell in fadedCells)
        {
            if (insideThisFrame.Contains(cell)) continue;

            if (StepAlpha(cell, 1f) >= 0.999f)
            {
                SetAlpha(cell, 1f);
                restored.Add(cell);
            }
        }

        foreach (Vector3Int cell in restored)
        {
            fadedCells.Remove(cell);
        }
    }

    private float StepAlpha(Vector3Int cell, float target)
    {
        float current = GetAlpha(cell);

        float next = fadeSeconds > 0f
            ? Mathf.MoveTowards(current, target, Time.deltaTime / fadeSeconds)
            : target;

        SetAlpha(cell, next);
        return next;
    }

    private float GetAlpha(Vector3Int cell) => overhead.GetColor(cell).a;

    private void SetAlpha(Vector3Int cell, float alpha)
    {
        Color color = overhead.GetColor(cell);
        color.a = alpha;

        // Diagnostic: force RGB to red on faded cells so a working SetColor is
        // visible even if alpha is being ignored.
        if (debugTintRed)
        {
            bool faded = alpha < 0.999f;
            color.r = 1f;
            color.g = faded ? 0f : 1f;
            color.b = faded ? 0f : 1f;
            color.a = 1f;
        }

        overhead.SetColor(cell, color);
    }

    private void ResolveLocalPlayer()
    {
        if (localPlayer != null) return;

        var nm = NetworkManager.Singleton;
        var playerObject = nm != null && nm.IsClient ? nm.LocalClient?.PlayerObject : null;
        if (playerObject != null) localPlayer = playerObject.transform;
    }

    private void OnDrawGizmosSelected()
    {
        Transform origin = localPlayer != null ? localPlayer : transform;
        Vector3 center = origin.position + new Vector3(0f, centerOffsetY, 0f);

        Gizmos.color = new Color(0.3f, 0.8f, 1f, 0.6f);
        Gizmos.matrix = Matrix4x4.TRS(center, Quaternion.identity,
            new Vector3(radiusX, radiusY, 1f));
        Gizmos.DrawWireSphere(Vector3.zero, 1f);
    }
}
