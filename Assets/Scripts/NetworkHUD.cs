using Unity.Netcode;
using UnityEngine;

public class NetworkHUD : MonoBehaviour
{
    private void OnGUI()
    {
        GUILayout.BeginArea(new Rect(10, 10, 200, 200));

        var nm = NetworkManager.Singleton;
        if (nm == null)
        {
            GUILayout.Label("No NetworkManager in scene.");
            GUILayout.EndArea();
            return;
        }

        if (!nm.IsClient && !nm.IsServer)
        {
            if (GUILayout.Button("Host")) nm.StartHost();
            if (GUILayout.Button("Client")) nm.StartClient();
            if (GUILayout.Button("Server")) nm.StartServer();
        }
        else
        {
            GUILayout.Label($"Mode: {(nm.IsHost ? "Host" : nm.IsServer ? "Server" : "Client")}");
            GUILayout.Label($"Connected: {nm.ConnectedClientsIds.Count}");
            if (GUILayout.Button("Shutdown")) nm.Shutdown();
        }

        GUILayout.EndArea();
    }
}
