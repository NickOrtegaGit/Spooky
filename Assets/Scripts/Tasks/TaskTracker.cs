using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Round-level task progress. Server counts; the total replicates to everyone.
/// </summary>
public class TaskTracker : NetworkBehaviour
{
    public static TaskTracker Instance { get; private set; }

    private readonly NetworkVariable<int> completedCount = new NetworkVariable<int>(0);

    private readonly List<TaskObject> tasks = new List<TaskObject>();

    public int CompletedCount => completedCount.Value;
    public int TotalCount { get; private set; }
    public bool AllComplete => TotalCount > 0 && completedCount.Value >= TotalCount;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public override void OnNetworkSpawn()
    {
        RefreshTaskList();
    }

    /// <summary>Finds every task in the scene. Call again if tasks spawn later.</summary>
    public void RefreshTaskList()
    {
        tasks.Clear();
        tasks.AddRange(FindObjectsByType<TaskObject>(FindObjectsSortMode.None));
        TotalCount = tasks.Count;
    }

    public void NotifyTaskCompleted()
    {
        if (!IsServer) return;

        completedCount.Value++;

        if (AllComplete)
        {
            Debug.Log("All tasks complete.");
        }
    }
}
