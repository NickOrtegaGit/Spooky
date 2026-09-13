using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Temporary OnGUI front end for Relay. Replace with a real menu before ship.
/// </summary>
public class RelayHUD : MonoBehaviour
{
    private string codeInput = string.Empty;

    private void OnGUI()
    {
        var relay = RelayConnectionManager.Instance;
        var nm = NetworkManager.Singleton;
        if (relay == null || nm == null) return;

        GUILayout.BeginArea(new Rect(10, 10, 260, 220), GUI.skin.box);

        GUILayout.Label(relay.Status);

        if (!nm.IsClient && !nm.IsServer)
        {
            GUI.enabled = !relay.IsBusy;

            if (GUILayout.Button("Host Game")) relay.StartHost();

            GUILayout.Space(8);
            GUILayout.Label("Join code:");
            codeInput = GUILayout.TextField(codeInput, 6).ToUpperInvariant();

            if (GUILayout.Button("Join Game")) relay.JoinWithCode(codeInput);

            GUI.enabled = true;
        }
        else
        {
            if (!string.IsNullOrEmpty(relay.JoinCode))
            {
                GUILayout.Label($"Code: {relay.JoinCode}");
                if (GUILayout.Button("Copy code"))
                {
                    GUIUtility.systemCopyBuffer = relay.JoinCode;
                }
            }

            GUILayout.Label($"Mode: {(nm.IsHost ? "Host" : nm.IsServer ? "Server" : "Client")}");
            GUILayout.Label($"Players: {nm.ConnectedClientsIds.Count}");

            if (GUILayout.Button("Disconnect")) relay.Disconnect();
        }

        GUILayout.EndArea();
    }
}
