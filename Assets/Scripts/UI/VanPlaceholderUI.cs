using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Stand-in for the van until seats exist: shows the join code and a player
/// count, and gives the host a Continue into the house. Replaced by the real
/// van, which replicates seats instead of reading the connection list.
/// </summary>
public class VanPlaceholderUI : MonoBehaviour
{
    [SerializeField] private TMP_Text codeText;
    [SerializeField] private TMP_Text countText;

    [Tooltip("Host only; hidden on clients.")]
    [SerializeField] private Button continueButton;

    private void Start()
    {
        var nm = NetworkManager.Singleton;
        bool isHost = nm != null && nm.IsServer;

        continueButton.gameObject.SetActive(isHost);
        continueButton.onClick.AddListener(() => SessionManager.Instance.LoadHouse());

        string code = RelayConnectionManager.Instance != null ? RelayConnectionManager.Instance.JoinCode : string.Empty;
        codeText.text = $"CODE: {code}";
    }

    private void Update()
    {
        var nm = NetworkManager.Singleton;
        if (nm == null || !nm.IsListening) return;

        // Only the host knows the real count; the real van replicates it.
        countText.text = nm.IsServer
            ? $"{nm.ConnectedClientsIds.Count}/{SessionManager.MaxPlayers} joined"
            : "Waiting for Mr. Magee...";
    }
}
