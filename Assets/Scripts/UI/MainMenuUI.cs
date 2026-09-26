using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Host or Join. Nothing connects here: Host just moves on to the character
/// screen, and Join checks its code with Relay first so a typo is caught
/// before you bother picking a name.
/// </summary>
public class MainMenuUI : MonoBehaviour
{
    [SerializeField] private Button hostButton;
    [SerializeField] private Button joinButton;

    [Header("Join panel")]
    [Tooltip("Hidden until Join is pressed.")]
    [SerializeField] private GameObject joinPanel;
    [SerializeField] private TMP_InputField codeInput;
    [SerializeField] private Button confirmJoinButton;
    [SerializeField] private Button cancelJoinButton;

    [Tooltip("Errors and status — a bad code, or why you were sent back here.")]
    [SerializeField] private TMP_Text messageText;

    private bool busy;

    private void Start()
    {
        hostButton.onClick.AddListener(OnHost);
        joinButton.onClick.AddListener(OpenJoinPanel);
        confirmJoinButton.onClick.AddListener(OnConfirmJoin);
        cancelJoinButton.onClick.AddListener(CloseJoinPanel);

        codeInput.onValueChanged.AddListener(OnCodeChanged);
        codeInput.onSubmit.AddListener(_ => OnConfirmJoin());

        joinPanel.SetActive(false);
        ShowMessage(PlayerProfile.TakeMenuMessage());
        RefreshInteractable();
    }

    private void Update()
    {
        if (busy || !joinPanel.activeSelf) return;

        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            CloseJoinPanel();
        }
    }

    private void OnHost()
    {
        if (busy) return;

        PlayerProfile.PendingIntent = PlayerProfile.Intent.Host;
        PlayerProfile.PendingJoinCode = string.Empty;
        SceneManager.LoadScene(SceneNames.Character);
    }

    private void OpenJoinPanel()
    {
        if (busy) return;

        joinPanel.SetActive(true);
        ShowMessage(string.Empty);
        codeInput.text = string.Empty;
        codeInput.ActivateInputField();
        RefreshInteractable();
    }

    private void CloseJoinPanel()
    {
        joinPanel.SetActive(false);
        ShowMessage(string.Empty);
        RefreshInteractable();
    }

    private void OnCodeChanged(string value)
    {
        // Relay codes are uppercase; typing them any other way should still work.
        string upper = value.ToUpperInvariant();
        if (upper != value) codeInput.SetTextWithoutNotify(upper);
        RefreshInteractable();
    }

    private async void OnConfirmJoin()
    {
        if (busy || string.IsNullOrWhiteSpace(codeInput.text)) return;

        var relay = RelayConnectionManager.Instance;
        if (relay == null)
        {
            ShowMessage("No RelayConnectionManager in the scene.");
            return;
        }

        string code = RelayConnectionManager.NormalizeCode(codeInput.text);

        busy = true;
        RefreshInteractable();
        ShowMessage("Checking code...");

        bool ok = await relay.CheckJoinCodeAsync(code);

        // The scene may have changed while waiting.
        if (this == null) return;

        busy = false;

        if (!ok)
        {
            ShowMessage("No job found for that code.");
            RefreshInteractable();
            codeInput.ActivateInputField();
            return;
        }

        PlayerProfile.PendingIntent = PlayerProfile.Intent.Join;
        PlayerProfile.PendingJoinCode = code;
        SceneManager.LoadScene(SceneNames.Character);
    }

    private void RefreshInteractable()
    {
        bool panelOpen = joinPanel.activeSelf;

        hostButton.interactable = !busy && !panelOpen;
        joinButton.interactable = !busy && !panelOpen;
        codeInput.interactable = !busy;
        confirmJoinButton.interactable = !busy && !string.IsNullOrWhiteSpace(codeInput.text);
        cancelJoinButton.interactable = !busy;
    }

    private void ShowMessage(string message)
    {
        if (messageText == null) return;

        messageText.text = message;
        messageText.gameObject.SetActive(!string.IsNullOrEmpty(message));
    }
}
