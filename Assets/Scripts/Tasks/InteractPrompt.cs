using UnityEngine;

/// <summary>
/// "Press E" bubble above the player's head. Put this on a child of the
/// player that has the SpriteRenderer and Animator.
///
/// Local only — you see your own prompt, nobody else's — so nothing here is
/// networked.
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class InteractPrompt : MonoBehaviour
{
    [SerializeField] private float hideAfterInteractSeconds = 2f;
    [SerializeField] private float fadeInSeconds = 0.15f;
    [SerializeField] private float fadeOutSeconds = 0.25f;

    private SpriteRenderer spriteRenderer;
    private Animator animator;

    private bool inRange;
    private float suppressedUntil;
    private float alpha;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        animator = GetComponent<Animator>();

        alpha = 0f;
        ApplyAlpha();
    }

    /// <summary>Called every frame by PlayerInteractor with the current state.</summary>
    public void SetInRange(bool value) => inRange = value;

    /// <summary>Hide briefly after interacting — placeholder until real tasks exist.</summary>
    public void SuppressAfterInteract() => suppressedUntil = Time.time + hideAfterInteractSeconds;

    private void Update()
    {
        bool shouldShow = inRange && Time.time >= suppressedUntil;

        float duration = shouldShow ? fadeInSeconds : fadeOutSeconds;
        float target = shouldShow ? 1f : 0f;

        alpha = duration > 0f
            ? Mathf.MoveTowards(alpha, target, Time.deltaTime / duration)
            : target;

        ApplyAlpha();
    }

    private void ApplyAlpha()
    {
        if (spriteRenderer == null) return;

        Color c = spriteRenderer.color;
        c.a = alpha;
        spriteRenderer.color = c;

        // Stop animating once fully hidden; resume as soon as it starts showing.
        bool visible = alpha > 0.001f;
        spriteRenderer.enabled = visible;
        if (animator != null) animator.enabled = visible;
    }
}
