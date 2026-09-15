using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Temporary stand-in for testing the framework: completes after a delay, or
/// fails on Escape. Replaced by the real typewriter.
/// </summary>
public class PlaceholderMinigame : Minigame
{
    [SerializeField] private float secondsToComplete = 3f;

    private float finishTime;

    protected override void OnBegin()
    {
        finishTime = Time.time + secondsToComplete;
        Debug.Log("Minigame started — hold on.");
    }

    private void Update()
    {
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            Debug.Log("Minigame failed.");
            Fail();
            return;
        }

        if (Time.time >= finishTime)
        {
            Debug.Log("Minigame complete.");
            Complete();
        }
    }

    private void OnGUI()
    {
        float remaining = Mathf.Max(0f, finishTime - Time.time);
        GUI.Box(new Rect(Screen.width / 2f - 150, 40, 300, 60),
            $"Working... {remaining:0.0}s\n(Esc to fail)");
    }
}
