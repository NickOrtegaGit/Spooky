using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Step 2 test harness: proves the overlay fade and panel slide work before
/// any real minigame exists. Enter succeeds, Escape fails.
/// </summary>
public class PanelTestMinigame : PanelMinigame
{
    protected override void OnPanelReady()
    {
        Debug.Log("Panel is up. Enter to complete, Escape to fail.");
    }

    private void Update()
    {
        if (!InputReady || Keyboard.current == null) return;

        if (Keyboard.current.enterKey.wasPressedThisFrame) EndWithSuccess();
        else if (Keyboard.current.escapeKey.wasPressedThisFrame) EndWithFailure();
    }
}
