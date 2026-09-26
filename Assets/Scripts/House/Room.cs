using UnityEngine;

/// <summary>
/// One room's visible extent, as a polygon. Everything inside it renders;
/// everything outside is occluded. See Docs/House Layout.md.
///
/// Draw the polygon to include whatever should be visible from inside —
/// floor, walls, decorations — not just the walkable floor.
///
/// Rooms are visibility units, not only architectural ones: a long hallway
/// players should see down end to end is one Room, however many doorways it
/// has.
/// </summary>
[RequireComponent(typeof(PolygonCollider2D))]
public class Room : MonoBehaviour
{
    [Tooltip("Editor label. Shows in the room gizmo and in logs.")]
    [SerializeField] private string roomName = "Room";

    private PolygonCollider2D area;

    public string RoomName => roomName;

    public PolygonCollider2D Area
    {
        get
        {
            if (area == null) area = GetComponent<PolygonCollider2D>();
            return area;
        }
    }

    private void Awake()
    {
        area = GetComponent<PolygonCollider2D>();

        // The room is a region, never a physical obstruction.
        area.isTrigger = true;
    }

    /// <summary>True when the point lies inside this room's polygon.</summary>
    public bool Contains(Vector2 worldPoint) => Area.OverlapPoint(worldPoint);

    private void OnDrawGizmos()
    {
        var polygon = GetComponent<PolygonCollider2D>();
        if (polygon == null) return;

        Gizmos.color = new Color(0.4f, 1f, 0.6f, 0.9f);

        // Draw each path so concave and multi-path rooms read correctly.
        for (int path = 0; path < polygon.pathCount; path++)
        {
            Vector2[] points = polygon.GetPath(path);
            if (points.Length < 2) continue;

            for (int i = 0; i < points.Length; i++)
            {
                Vector2 from = (Vector2)transform.TransformPoint(points[i]);
                Vector2 to = (Vector2)transform.TransformPoint(points[(i + 1) % points.Length]);
                Gizmos.DrawLine(from, to);
            }
        }
    }
}
