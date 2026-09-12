using UnityEngine;

/// <summary>
/// Scene-side patrol route. The monster prefab cannot hold references to scene
/// objects, so it looks this up at spawn time instead.
/// </summary>
public class PatrolRoute : MonoBehaviour
{
    public static PatrolRoute Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    /// <summary>Waypoints in child order, so reordering in the Hierarchy reorders the route.</summary>
    public Transform[] GetWaypoints()
    {
        var points = new Transform[transform.childCount];
        for (int i = 0; i < transform.childCount; i++)
        {
            points[i] = transform.GetChild(i);
        }
        return points;
    }
}
