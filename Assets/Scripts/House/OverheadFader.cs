using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// Fades overhead art — wall tops, roof edges — tile by tile around the local
/// player, so walking behind something never hides the player from themselves.
///
/// One of these covers the whole house: paint art on any of the overhead
/// tilemaps and it works everywhere, with nothing to author per doorway.
///
/// Purely local presentation. Deliberately driven by the LOCAL PLAYER only —
/// the monster must stay concealed behind overhead art or hiding places stop
/// working. Nothing here is networked.
///
/// Note: tiles generated from a sprite sheet ship with TileFlags.LockColor,
/// which makes SetColor silently do nothing. Clear it on the tile assets or
/// none of this is visible. See Docs/House Layout.md.
/// </summary>
public class OverheadFader : MonoBehaviour
{
    [Tooltip("Tilemaps holding overhead art. No colliders, sorted above the player.")]
    [SerializeField] private List<Tilemap> overheadLayers = new List<Tilemap>();

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

    [Header("Debug")]
    [Tooltip("Logs what the cell scan actually finds, once per second.")]
    [SerializeField] private bool logScan = false;

    /// <summary>A cell on a specific layer. Cell coordinates repeat across tilemaps.</summary>
    private readonly struct LayerCell
    {
        public readonly int LayerIndex;
        public readonly Vector3Int Cell;

        public LayerCell(int layerIndex, Vector3Int cell)
        {
            LayerIndex = layerIndex;
            Cell = cell;
        }
    }

    // Every cell currently faded below opaque, across all layers. Only these
    // need restoring, so no tilemap is ever walked in full.
    private readonly HashSet<LayerCell> fadedCells = new HashSet<LayerCell>();

    // Cells inside the ellipse this frame — everything else eases back.
    private readonly HashSet<LayerCell> insideThisFrame = new HashSet<LayerCell>();
    private readonly List<LayerCell> restored = new List<LayerCell>();

    private Transform localPlayer;
    private bool warnedNoLayers;
    private float nextScanLog;

    private void Update()
    {
        if (overheadLayers.Count == 0)
        {
            if (!warnedNoLayers)
            {
                warnedNoLayers = true;
                Debug.LogWarning("OverheadFader has no overhead tilemaps assigned.", this);
            }
            return;
        }

        ResolveLocalPlayer();

        insideThisFrame.Clear();
        if (localPlayer != null) FadeAroundPlayer();

        RestoreCellsOutsideEllipse();
    }

    private void FadeAroundPlayer()
    {
        Vector3 center = localPlayer.position + new Vector3(0f, centerOffsetY, 0f);

        for (int layerIndex = 0; layerIndex < overheadLayers.Count; layerIndex++)
        {
            Tilemap layer = overheadLayers[layerIndex];
            if (layer == null) continue;

            FadeLayerAround(layerIndex, layer, center);
        }

        LogScan(center);
    }

    private void FadeLayerAround(int layerIndex, Tilemap layer, Vector3 center)
    {
        // Cell range covering the ellipse's bounding box on this layer.
        Vector3Int min = layer.WorldToCell(center + new Vector3(-radiusX, -radiusY));
        Vector3Int max = layer.WorldToCell(center + new Vector3(radiusX, radiusY));

        for (int x = min.x; x <= max.x; x++)
        {
            for (int y = min.y; y <= max.y; y++)
            {
                var cell = new Vector3Int(x, y, 0);
                if (!layer.HasTile(cell)) continue;

                Vector3 tileCenter = layer.GetCellCenterWorld(cell);

                // Normalized elliptical distance: 1 at the edge, 0 at the center.
                float dx = (tileCenter.x - center.x) / Mathf.Max(radiusX, 0.0001f);
                float dy = (tileCenter.y - center.y) / Mathf.Max(radiusY, 0.0001f);
                float distance = Mathf.Sqrt(dx * dx + dy * dy);
                if (distance > 1f) continue;

                float t = Mathf.Pow(1f - distance, falloff);
                float target = Mathf.Lerp(1f, minAlpha, t);

                var layerCell = new LayerCell(layerIndex, cell);
                StepAlpha(layerCell, target);
                insideThisFrame.Add(layerCell);
                fadedCells.Add(layerCell);
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

        foreach (LayerCell cell in fadedCells)
        {
            if (insideThisFrame.Contains(cell)) continue;

            if (StepAlpha(cell, 1f) >= 0.999f)
            {
                SetAlpha(cell, 1f);
                restored.Add(cell);
            }
        }

        foreach (LayerCell cell in restored)
        {
            fadedCells.Remove(cell);
        }
    }

    private float StepAlpha(LayerCell cell, float target)
    {
        float current = GetAlpha(cell);

        float next = fadeSeconds > 0f
            ? Mathf.MoveTowards(current, target, Time.deltaTime / fadeSeconds)
            : target;

        SetAlpha(cell, next);
        return next;
    }

    private Tilemap LayerOf(LayerCell cell) =>
        cell.LayerIndex >= 0 && cell.LayerIndex < overheadLayers.Count
            ? overheadLayers[cell.LayerIndex]
            : null;

    private float GetAlpha(LayerCell cell)
    {
        Tilemap layer = LayerOf(cell);
        return layer != null ? layer.GetColor(cell.Cell).a : 1f;
    }

    private void SetAlpha(LayerCell cell, float alpha)
    {
        Tilemap layer = LayerOf(cell);
        if (layer == null) return;

        Color color = layer.GetColor(cell.Cell);
        color.a = alpha;
        layer.SetColor(cell.Cell, color);
    }

    private void ResolveLocalPlayer()
    {
        if (localPlayer != null) return;

        var nm = NetworkManager.Singleton;
        var playerObject = nm != null && nm.IsClient ? nm.LocalClient?.PlayerObject : null;
        if (playerObject != null) localPlayer = playerObject.transform;
    }

    private void LogScan(Vector3 center)
    {
        if (!logScan || Time.time < nextScanLog) return;
        nextScanLog = Time.time + 1f;

        string cellReport = "none faded";
        foreach (LayerCell faded in fadedCells)
        {
            Tilemap layer = LayerOf(faded);
            if (layer == null) continue;

            TileBase tile = layer.GetTile(faded.Cell);
            cellReport =
                $"'{layer.name}' {faded.Cell}: tile '{(tile != null ? tile.name : "null")}', " +
                $"flags {layer.GetTileFlags(faded.Cell)}, color {layer.GetColor(faded.Cell)}";
            break;
        }

        Debug.Log($"OverheadFader — center {center}, {overheadLayers.Count} layer(s), " +
                  $"{fadedCells.Count} cell(s) faded. {cellReport}", this);
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
