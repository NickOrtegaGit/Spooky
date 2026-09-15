using System.Collections;
using UnityEngine;

/// <summary>
/// Shared presentation for every task minigame: a dark overlay fades in and a
/// panel slides up, then both reverse when the task ends.
///
/// This is the *only* universal part. Anything else — paper, typing, shakes —
/// belongs to the specific minigame. Entirely local.
/// </summary>
public abstract class PanelMinigame : Minigame
{
    [Header("Shared presentation")]
    [SerializeField] protected CanvasGroup overlay;
    [SerializeField] protected RectTransform panel;

    [Header("Timing")]
    [SerializeField] private float slideSeconds = 0.35f;
    [SerializeField] private float overlayTargetAlpha = 0.85f;

    private Vector2 panelShownPosition;
    private Vector2 panelHiddenPosition;
    private bool inputReady;

    /// <summary>True once the panel is up and the player may act.</summary>
    protected bool InputReady => inputReady;

    /// <summary>Where the panel rests when shown — subclasses may need it.</summary>
    protected Vector2 PanelShownPosition => panelShownPosition;

    protected override void OnBegin()
    {
        if (panel != null)
        {
            panelShownPosition = panel.anchoredPosition;
            panelHiddenPosition = panelShownPosition + new Vector2(0f, -Screen.height);
            panel.anchoredPosition = panelHiddenPosition;
        }

        if (overlay != null) overlay.alpha = 0f;

        StartCoroutine(SlideIn());
    }

    private IEnumerator SlideIn()
    {
        yield return Animate(panelHiddenPosition, panelShownPosition, 0f, overlayTargetAlpha);
        inputReady = true;
        OnPanelReady();
    }

    /// <summary>The panel is up and the player can act. Set up the game here.</summary>
    protected abstract void OnPanelReady();

    /// <summary>End in failure. Override EndSequence to play something first.</summary>
    protected void EndWithFailure()
    {
        if (!inputReady) return;
        inputReady = false;
        StartCoroutine(EndRoutine(false));
    }

    /// <summary>End in success.</summary>
    protected void EndWithSuccess()
    {
        if (!inputReady) return;
        inputReady = false;
        StartCoroutine(EndRoutine(true));
    }

    private IEnumerator EndRoutine(bool success)
    {
        // Minigame-specific flourish (a shake, a stamp, a sound) before the
        // shared slide-down.
        yield return EndSequence(success);

        yield return Animate(panelShownPosition, panelHiddenPosition,
            overlay != null ? overlay.alpha : 0f, 0f);

        if (success) Complete();
        else Fail();
    }

    /// <summary>Optional per-minigame animation before the panel slides away.</summary>
    protected virtual IEnumerator EndSequence(bool success)
    {
        yield break;
    }

    private IEnumerator Animate(Vector2 from, Vector2 to, float alphaFrom, float alphaTo)
    {
        float elapsed = 0f;
        while (elapsed < slideSeconds)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / slideSeconds);

            if (panel != null) panel.anchoredPosition = Vector2.Lerp(from, to, t);
            if (overlay != null) overlay.alpha = Mathf.Lerp(alphaFrom, alphaTo, t);

            yield return null;
        }

        if (panel != null) panel.anchoredPosition = to;
        if (overlay != null) overlay.alpha = alphaTo;
    }
}
