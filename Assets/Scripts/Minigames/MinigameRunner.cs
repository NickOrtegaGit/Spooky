using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Hosts the one minigame the local player is currently doing. Local only —
/// nothing here is networked except the start/fail/complete reports that
/// TaskObject sends.
/// </summary>
public class MinigameRunner : MonoBehaviour
{
    public static MinigameRunner Instance { get; private set; }

    private Minigame active;
    private TaskObject activeTask;

    public bool IsBusy => active != null;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public void Run(Minigame prefab, TaskObject task)
    {
        if (IsBusy || prefab == null || task == null) return;

        activeTask = task;
        active = Instantiate(prefab, transform);
        active.Initialize(this);
    }

    public void ReportComplete()
    {
        TaskObject task = activeTask;
        Teardown();
        task?.ReportCompleteServerRpc();
    }

    public void ReportFail()
    {
        TaskObject task = activeTask;
        Teardown();
        task?.ReportFailServerRpc();
    }

    /// <summary>Bail out without reporting success — used when the player is caught.</summary>
    public void ForceExit()
    {
        TaskObject task = activeTask;
        Teardown();
        task?.ReportFailServerRpc();
    }

    private void Teardown()
    {
        if (active != null)
        {
            active.EndInternal();
            Destroy(active.gameObject);
        }

        active = null;
        activeTask = null;
    }
}
