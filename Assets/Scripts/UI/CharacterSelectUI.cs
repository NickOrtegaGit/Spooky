using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Name and skin, then Continue — which is the moment anything connects.
/// Hosting starts the session and loads the van; joining connects, and NGO
/// pulls this client into the host's van on its own.
///
/// Local only. Connecting any earlier would let NGO's scene sync drag a
/// joining client straight into the van, past this screen.
///
/// The skin already showing is the chosen one; cycling is optional.
/// </summary>
public class CharacterSelectUI : MonoBehaviour
{
    [SerializeField] private SkinCatalog catalog;

    [Header("Name")]
    [SerializeField] private TMP_InputField nameInput;

    [Header("Skin")]
    [SerializeField] private Image preview;
    [SerializeField] private TMP_Text skinNameText;
    [SerializeField] private Button previousButton;
    [SerializeField] private Button nextButton;

    [Header("Continue")]
    [SerializeField] private Button continueButton;

    [Tooltip("\"Starting the van...\", or why it did not.")]
    [SerializeField] private TMP_Text statusText;

    private int skinIndex;
    private bool busy;

    private void Start()
    {
        if (PlayerProfile.PendingIntent == PlayerProfile.Intent.None)
        {
            // Played directly from this scene rather than through the menu.
            Debug.LogWarning("CharacterSelectUI: no host/join intent set; treating Continue as Host.");
            PlayerProfile.PendingIntent = PlayerProfile.Intent.Host;
        }

        nameInput.characterLimit = PlayerProfile.MaxNameLength;
        nameInput.text = PlayerProfile.Name;
        nameInput.onValueChanged.AddListener(_ => RefreshInteractable());
        nameInput.onSubmit.AddListener(_ => OnContinue());

        previousButton.onClick.AddListener(() => Cycle(-1));
        nextButton.onClick.AddListener(() => Cycle(+1));
        continueButton.onClick.AddListener(OnContinue);

        skinIndex = catalog != null ? catalog.Wrap(PlayerProfile.SkinIndex) : 0;

        // A returning player's saved name is already there; a new one starts typing.
        if (string.IsNullOrEmpty(nameInput.text)) nameInput.ActivateInputField();

        SetStatus(string.Empty);
        RefreshSkin();
        RefreshInteractable();
    }

    private void Update()
    {
        if (busy) return;

        var keyboard = Keyboard.current;
        if (keyboard == null) return;

        if (keyboard.escapeKey.wasPressedThisFrame)
        {
            SceneManager.LoadScene(SceneNames.MainMenu);
            return;
        }

        // While typing a name, A and D are letters and the arrows move the
        // caret. Click off the field to cycle skins by key.
        if (nameInput.isFocused) return;

        if (keyboard.aKey.wasPressedThisFrame || keyboard.leftArrowKey.wasPressedThisFrame) Cycle(-1);
        if (keyboard.dKey.wasPressedThisFrame || keyboard.rightArrowKey.wasPressedThisFrame) Cycle(+1);
    }

    private void Cycle(int direction)
    {
        if (busy || catalog == null || catalog.Count == 0) return;

        skinIndex = catalog.Wrap(skinIndex + direction);
        RefreshSkin();
    }

    private void RefreshSkin()
    {
        SkinCatalog.Skin skin = catalog != null ? catalog.Get(skinIndex) : null;

        if (preview != null)
        {
            preview.sprite = skin?.preview;
            preview.enabled = skin?.preview != null;
        }

        if (skinNameText != null) skinNameText.text = skin?.displayName ?? string.Empty;
    }

    private void RefreshInteractable()
    {
        bool hasName = !string.IsNullOrEmpty(PlayerProfile.Sanitize(nameInput.text));

        nameInput.interactable = !busy;
        previousButton.interactable = !busy;
        nextButton.interactable = !busy;
        continueButton.interactable = !busy && hasName;
    }

    private async void OnContinue()
    {
        string name = PlayerProfile.Sanitize(nameInput.text);
        if (busy || string.IsNullOrEmpty(name)) return;

        var relay = RelayConnectionManager.Instance;
        if (relay == null)
        {
            SetStatus("No RelayConnectionManager — start from the MainMenu scene.");
            return;
        }

        PlayerProfile.Name = name;
        PlayerProfile.SkinIndex = skinIndex;

        busy = true;
        RefreshInteractable();

        bool hosting = PlayerProfile.PendingIntent == PlayerProfile.Intent.Host;
        SetStatus(hosting ? "Starting the van..." : "Catching up to the van...");

        bool ok = hosting
            ? await relay.StartHostAsync()
            : await relay.JoinWithCodeAsync(PlayerProfile.PendingJoinCode);

        if (this == null) return;

        if (!ok)
        {
            busy = false;
            SetStatus(hosting
                ? "Couldn't start the van. Check your connection and try again."
                : "Couldn't reach that van — the host may have left. Esc to go back.");
            RefreshInteractable();
            return;
        }

        // A joining client waits here: NGO loads the host's van for it once
        // the connection is approved, and a refusal sends it back to the menu.
        if (hosting) SessionManager.Instance.LoadVan();
    }

    private void SetStatus(string status)
    {
        if (statusText != null) statusText.text = status;
    }
}
