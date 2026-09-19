using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// Clears LockColor on selected Tile assets so Tilemap.SetColor can tint them
/// per cell — what OverheadFader needs to fade overhead art.
///
/// Tiles created through Assets > Create > 2D > Tiles > Tile lock their color
/// by default, and Tilemap caches the value SetColor writes, so the fade looks
/// like it is working while the renderer ignores it.
/// </summary>
public static class ClearTileColorLock
{
    [MenuItem("Tools/Spooky/Clear Color Lock on Selected Tiles")]
    private static void ClearOnSelection()
    {
        Object[] selected = Selection.GetFiltered(typeof(TileBase), SelectionMode.Assets);

        if (selected.Length == 0)
        {
            Debug.LogWarning("Select one or more Tile assets in the Project window first.");
            return;
        }

        int changed = 0;

        foreach (Object asset in selected)
        {
            if (asset is not Tile tile) continue;

            if ((tile.flags & TileFlags.LockColor) == 0) continue;

            Undo.RecordObject(tile, "Clear Tile Color Lock");
            tile.flags &= ~TileFlags.LockColor;
            EditorUtility.SetDirty(tile);
            changed++;
        }

        AssetDatabase.SaveAssets();

        Debug.Log($"Cleared color lock on {changed} of {selected.Length} selected tile(s). " +
                  "Tiles already unlocked were skipped.");
    }

    [MenuItem("Tools/Spooky/Report Flags on Selected Tiles")]
    private static void ReportOnSelection()
    {
        Object[] selected = Selection.GetFiltered(typeof(TileBase), SelectionMode.Assets);

        foreach (Object asset in selected)
        {
            if (asset is Tile tile)
            {
                Debug.Log($"{tile.name}: flags = {tile.flags}", tile);
            }
            else
            {
                Debug.Log($"{asset.name}: {asset.GetType().Name} — flags not exposed.", asset);
            }
        }
    }
}
