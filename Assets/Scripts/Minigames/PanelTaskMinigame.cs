using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Placeholder task panel. Sit on it uninterrupted for a few seconds to
/// complete; X backs out. The real minigames replace the timer — the panel
/// presentation is inherited and stays.
/// </summary>
public class PanelTaskMinigame : PanelMinigame
{
    [Header("Placeholder completion")]
    [SerializeField] private float secondsToComplete = 5f;

    [Header("UI")]
    [SerializeField] private TMP_Text countdownLabel;
    [SerializeField] private TMP_Text hintLabel;

    private float remaining;
    private bool counting;

    protected override void OnPanelReady()
    {
        remaining = secondsToComplete;
        counting = true;

        if (hintLabel != null) hintLabel.text = "Press X to leave";
        UpdateCountdown();
    }

    private void Update()
    {
        if (!counting) return;

        if (Keyboard.current != null && Keyboard.current.xKey.wasPressedThisFrame)
        {
            counting = false;
            EndWithFailure();
            return;
        }

        remaining -= Time.deltaTime;
        UpdateCountdown();

        if (remaining <= 0f)
        {
            counting = false;
            EndWithSuccess();
        }
    }

    private void UpdateCountdown()
    {
        if (countdownLabel == null) return;
        countdownLabel.text = $"{Mathf.Max(0f, remaining):0.0}";
    }

    /// <summary>Freeze the readout on the outcome while the panel slides away.</summary>
    protected override IEnumerator EndSequence(bool success)
    {
        if (countdownLabel != null) countdownLabel.text = success ? "Done" : "Cancelled";
        if (hintLabel != null) hintLabel.text = string.Empty;

        yield return new WaitForSeconds(0.25f);
    }

    /// <summary>Stop counting if the runner tears us down (the monster caught you).</summary>
    protected override void OnEnd()
    {
        counting = false;
    }
}
