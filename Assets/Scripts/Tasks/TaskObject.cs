using Unity.Netcode;
using UnityEngine;

/// <summary>
/// An interactable that counts toward the round's task list. Completion is
/// server-authoritative and replicated, so every client sees the same state.
/// </summary>
public class TaskObject : Interactable
{
    [SerializeField] private string taskDescription = "Do the thing";

    private readonly NetworkVariable<bool> isComplete = new NetworkVariable<bool>(false);

    public string TaskDescription => taskDescription;
    public bool IsComplete => isComplete.Value;

    public override void Interact(ulong clientId)
    {
        if (!IsServer || isComplete.Value) return;

        // v1: instant completion. A real minigame replaces this — start it on
        // the interacting client, and only report back start/fail/complete.
        isComplete.Value = true;
        TaskTracker.Instance?.NotifyTaskCompleted();
    }
}
