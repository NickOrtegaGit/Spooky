using Unity.Netcode;
using UnityEngine;

/// <summary>
/// A stamina bar above the player's head. Fades in when stamina starts
/// draining and fades away once it is full again.
///
/// Owner-only, like the interact prompt: you see your own bar and nobody
/// else's. Being winded is not something to broadcast to the room — and
/// PlayerStamina only replicates its value to the owner anyway, so a remote
/// copy would have nothing to show.
///
/// Put this on a child of the player holding the bar's SpriteRenderers.
/// </summary>
public class StaminaBar : MonoBehaviour
{
    [Header("Sprites")]
    [Tooltip("The filling part. Scaled on X from a LEFT-edge pivot, so it " +
             "empties toward its left rather than toward its middle.")]
    [SerializeField] private SpriteRenderer fill;

    [Tooltip("Optional backing or frame behind the fill.")]
    [SerializeField] private SpriteRenderer background;

    [Header("Colors")]
    [SerializeField] private Color normalColor = new Color(0.85f, 0.85f, 0.8f);

    [Tooltip("Shown while sprinting is locked out after bottoming out.")]
    [SerializeField] private Color exhaustedColor = new Color(0.8f, 0.3f, 0.25f);

    [Header("Fading")]
    [SerializeField] private float fadeInSeconds = 0.15f;
    [SerializeField] private float fadeOutSeconds = 0.6f;

    [Tooltip("Seconds the full bar lingers before fading away.")]
    [SerializeField] private float holdAfterFullSeconds = 0.5f;

    private NetworkBehaviour owner;
    private PlayerStamina stamina;
    private float fillWidth;
    private float alpha;
    private float hideAtTime;

    private void Awake()
    {
        stamina = GetComponentInParent<PlayerStamina>();
        owner = stamina;

        // Remember the authored width so the fill can scale against it.
        if (fill != null) fillWidth = fill.transform.localScale.x;

        alpha = 0f;
        ApplyAlpha();
    }

    private void Update()
    {
        // Only the owning client has a stamina value to show — the variable is
        // not replicated to anyone else.
        if (stamina == null || owner == null || !owner.IsOwner)
        {
            alpha = 0f;
            ApplyAlpha();
            return;
        }

        UpdateFill();
        UpdateVisibility();
        ApplyAlpha();
    }

    private void UpdateFill()
    {
        if (fill == null) return;

        Vector3 scale = fill.transform.localScale;
        scale.x = fillWidth * stamina.Normalized;
        fill.transform.localScale = scale;

        fill.color = ColorWithAlpha(
            stamina.IsExhausted ? exhaustedColor : normalColor, alpha);
    }

    /// <summary>
    /// Visible whenever the bar is not full, plus a moment afterward so it does
    /// not vanish the instant it tops up.
    /// </summary>
    private void UpdateVisibility()
    {
        bool shouldShow = !stamina.IsFull || stamina.IsExhausted;

        if (shouldShow) hideAtTime = Time.time + holdAfterFullSeconds;

        bool visible = Time.time < hideAtTime;

        float duration = visible ? fadeInSeconds : fadeOutSeconds;
        float target = visible ? 1f : 0f;

        alpha = duration > 0f
            ? Mathf.MoveTowards(alpha, target, Time.deltaTime / duration)
            : target;
    }

    private void ApplyAlpha()
    {
        if (background != null) background.color = ColorWithAlpha(background.color, alpha);
        if (fill != null) fill.color = ColorWithAlpha(fill.color, alpha);

        // Stop drawing entirely once hidden, rather than drawing transparent.
        bool visible = alpha > 0.001f;
        if (background != null) background.enabled = visible;
        if (fill != null) fill.enabled = visible;
    }

    private static Color ColorWithAlpha(Color color, float a)
    {
        color.a = a;
        return color;
    }
}
