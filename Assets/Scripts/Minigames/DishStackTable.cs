using UnityEngine;

/// <summary>
/// The table surface in the dish-stacking stage. Any plate touching it has
/// missed the stack, which fails the task.
///
/// Put this on a trigger collider spanning the bottom of the stage, wide enough
/// that a plate sliding off the side still lands in it rather than falling
/// past.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class DishStackTable : MonoBehaviour
{
    [Tooltip("Layer the plates are on, so scenery cannot trip the failure.")]
    [SerializeField] private LayerMask plateLayers = ~0;

    /// <summary>Raised when a plate touches the table.</summary>
    public event System.Action PlateHitTable;

    private void Reset()
    {
        GetComponent<Collider2D>().isTrigger = true;
    }

    [Tooltip("Logs every trigger entry and whether it counted as a plate.")]
    [SerializeField] private bool logHits = false;

    private void OnTriggerEnter2D(Collider2D other)
    {
        bool isPlate = (plateLayers.value & (1 << other.gameObject.layer)) != 0;

        if (logHits)
        {
            Debug.Log($"DishStackTable: '{other.name}' on layer " +
                      $"{LayerMask.LayerToName(other.gameObject.layer)} entered — " +
                      $"counts as plate: {isPlate}", this);
        }

        if (!isPlate) return;

        PlateHitTable?.Invoke();
    }
}
