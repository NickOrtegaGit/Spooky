using UnityEngine;

/// <summary>
/// Base for task minigames. Entirely local to the player doing it — see
/// Docs/Tasks.md. Subclasses call Complete() or Fail() when done.
/// </summary>
public abstract class Minigame : MonoBehaviour
{
    private MinigameRunner runner;

    /// <summary>Called by the runner once the instance exists.</summary>
    public void Initialize(MinigameRunner owner)
    {
        runner = owner;
        OnBegin();
    }

    /// <summary>Set up UI and input here.</summary>
    protected abstract void OnBegin();

    /// <summary>Tear down here. Called on success, failure, and force-exit.</summary>
    protected virtual void OnEnd() { }

    protected void Complete()
    {
        runner?.ReportComplete();
    }

    protected void Fail()
    {
        runner?.ReportFail();
    }

    /// <summary>Called by the runner. Do not call directly.</summary>
    public void EndInternal() => OnEnd();
}
