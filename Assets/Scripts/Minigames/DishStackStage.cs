using UnityEngine;

/// <summary>
/// The off-screen physics stage the dish-stacking minigame plays on: table,
/// spawn point, and the camera filming it into a RenderTexture.
///
/// A scene singleton, for the same reason PatrolRoute is one — the minigame
/// panel is a prefab, and a prefab cannot hold references to scene objects.
/// The panel looks the stage up at runtime instead.
///
/// Put this on the stage root, far from the house so its physics and its
/// camera never touch the level.
/// </summary>
public class DishStackStage : MonoBehaviour
{
    public static DishStackStage Instance { get; private set; }

    [Tooltip("Where a new plate starts. Its X is the left end of the slide.")]
    [SerializeField] private Transform spawnPoint;

    [Tooltip("Trigger covering the table surface. A plate touching it fails.")]
    [SerializeField] private DishStackTable table;

    public Transform SpawnPoint => spawnPoint;
    public DishStackTable Table => table;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }
}
