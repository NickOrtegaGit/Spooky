using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// A marker tile meaning "spawn a door leaf here." It carries no door art —
/// the frame is painted as ordinary tiles in Walls. This exists only so
/// DoorSpawner can find door positions by scanning a tilemap.
///
/// Paint it on the CENTER cell of the frame's 3x3 footprint, on a dedicated
/// Markers tilemap whose renderer is disabled so players never see it.
///
/// Create one via Assets > Create > Spooky > Door Tile.
/// </summary>
[CreateAssetMenu(menuName = "Spooky/Door Tile", fileName = "DoorTile")]
public class DoorTile : TileBase
{
    [Tooltip("Editor-visible marker art. Keep it distinct — players never see it.")]
    [SerializeField] private Sprite markerSprite;

    [Tooltip("Tint for the marker in the editor.")]
    [SerializeField] private Color markerColor = new Color(1f, 0.3f, 0.3f, 0.6f);

    [Header("Leaf placement")]
    [Tooltip("Offset from the marker cell's center to where the leaf sits, in world units. " +
             "A 2-tall leaf in a 3-tall frame usually needs a nudge here.")]
    [SerializeField] private Vector2 leafOffset = Vector2.zero;

    public Vector2 LeafOffset => leafOffset;

    public override void GetTileData(Vector3Int position, ITilemap tilemap, ref TileData tileData)
    {
        tileData.sprite = markerSprite;
        tileData.color = markerColor;

        // Never collides: the frame tiles in Walls do the blocking, and the
        // spawned leaf brings its own collider.
        tileData.colliderType = Tile.ColliderType.None;
        tileData.flags = TileFlags.LockTransform;

        // Deliberately NOT setting tileData.gameObject: Unity would
        // instantiate it un-networked on every client independently. The host
        // spawns doors instead — see DoorSpawner.
    }
}
