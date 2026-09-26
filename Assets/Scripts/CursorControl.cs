using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

/// <summary>
/// Cursor is visible in the menu and hidden during play. Relative flashlight
/// aim needs no pointer, and locking means the mouse never runs out of screen
/// to turn with.
///
/// Lives on the NetworkManager object, so it persists through every scene.
/// The session starts before the van, which needs a pointer for Continue —
/// so locking waits for the house as well as a running session.
/// </summary>
public class CursorControl : MonoBehaviour
{
    private bool sessionActive;

    private static bool InHouse => SceneManager.GetActiveScene().name == SceneNames.House;

    private void Start()
    {
        Unlock();
        SceneManager.sceneLoaded += OnSceneLoaded;

        var nm = NetworkManager.Singleton;
        if (nm == null) return;

        nm.OnClientStarted += OnSessionStarted;
        nm.OnServerStarted += OnSessionStarted;
        nm.OnClientStopped += OnSessionStopped;
        nm.OnServerStopped += OnSessionStopped;
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;

        var nm = NetworkManager.Singleton;
        if (nm == null) return;

        nm.OnClientStarted -= OnSessionStarted;
        nm.OnServerStarted -= OnSessionStarted;
        nm.OnClientStopped -= OnSessionStopped;
        nm.OnServerStopped -= OnSessionStopped;
    }

    private void Update()
    {
        if (!sessionActive || !InHouse) return;

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
        if (InHouse) Lock();
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (sessionActive && InHouse) Lock();
        else Unlock();
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
