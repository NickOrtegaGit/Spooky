using UnityEngine;
using UnityEngine.Rendering.Universal;

/// <summary>
/// The player's own light bubble. Items can suppress it as a drawback — the
/// flashlight trades your close-range awareness for a directional beam.
/// </summary>
public class PlayerLightControl : MonoBehaviour
{
    [SerializeField] private Light2D playerLight;

    private int suppressors;

    private void Awake()
    {
        if (playerLight == null) playerLight = GetComponentInChildren<Light2D>(true);
        Apply();
    }

    /// <summary>Counted, so several sources can suppress without fighting.</summary>
    public void AddSuppressor()
    {
        suppressors++;
        Apply();
    }

    public void RemoveSuppressor()
    {
        suppressors = Mathf.Max(0, suppressors - 1);
        Apply();
    }

    private void Apply()
    {
        if (playerLight != null) playerLight.enabled = suppressors == 0;
    }
}
