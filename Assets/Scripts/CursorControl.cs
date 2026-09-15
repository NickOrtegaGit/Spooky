using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Cursor is visible in the menu and hidden during play. Relative flashlight
/// aim needs no pointer, and locking means the mouse never runs out of screen
/// to turn with.
/// </summary>
public class CursorControl : MonoBehaviour
{
    private bool sessionActive;

    private void Start()
    {
        Unlock();

        var nm = NetworkManager.Singleton;
        if (nm == null) return;

        nm.OnClientStarted += OnSessionStarted;
        nm.OnServerStarted += OnSessionStarted;
        nm.OnClientStopped += OnSessionStopped;
        nm.OnServerStopped += OnSessionStopped;
    }

    private void OnDestroy()
    {
        var nm = NetworkManager.Singleton;
        if (nm == null) return;

        nm.OnClientStarted -= OnSessionStarted;
        nm.OnServerStarted -= OnSessionStarted;
        nm.OnClientStopped -= OnSessionStopped;
        nm.OnServerStopped -= OnSessionStopped;
    }

    private void Update()
    {
        if (!sessionActive) return;

        // Escape frees the cursor mid-session; clicking back in re-locks it.
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            Unlock();
        }
        else if (Cursor.lockState == CursorLockMode.None
                 && Mouse.current != null
                 && Mouse.current.leftButton.wasPressedThisFrame)
        {
            Lock();
        }
    }

    private void OnSessionStarted()
    {
        sessionActive = true;
        Lock();
    }

    private void OnSessionStopped(bool _)
    {
        sessionActive = false;
        Unlock();
    }

    private void Lock()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void Unlock()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void OnDisable() => Unlock();
}
