using UnityEngine;

/// <summary>
/// Decides the next point to steer toward. Swapping in A* later means writing
/// a new implementation of this, not touching the monster state machine.
/// </summary>
public interface IPathfinder
{
    Vector2 GetNextStep(Vector2 from, Vector2 to);
}

/// <summary>
/// v1: walk straight at the target. Correct in an open room, and wrong the
/// moment the house has concave geometry to path around.
/// </summary>
public class DirectPathfinder : IPathfinder
{
    public Vector2 GetNextStep(Vector2 from, Vector2 to) => to;
}
