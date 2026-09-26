using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Anything a player can walk up to and use. Subclass it for real tasks;
/// minigames plug in by overriding Interact.
/// </summary>
public class Interactable : NetworkBehaviour
{
    private static readonly int FlashAmountId = Shader.PropertyToID("_FlashAmount");
    private static readonly int FlashColorId = Shader.PropertyToID("_FlashColor");

    [SerializeField] private string prompt = "Use";

    [Tooltip("Extra reach for this object, added to the player's interact range. " +
             "0 uses the player's range as-is. Useful for something large or " +
             "awkwardly shaped, like a tall door.")]
    [SerializeField] private float extraInteractRange = 0f;
    [SerializeField] private Color highlightColor = Color.white;
    [SerializeField, Range(0f, 1f)] private float highlightStrength = 1f;

    private SpriteRenderer spriteRenderer;
    private MaterialPropertyBlock block;
    private bool highlighted;

    public string Prompt => prompt;

    /// <summary>Added to the player's interact range for this object.</summary>
    public float ExtraInteractRange => extraInteractRange;

    protected virtual void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        block = new MaterialPropertyBlock();
    }

    public void SetHighlighted(bool on)
    {
        if (highlighted == on || spriteRenderer == null) return;
        highlighted = on;

        spriteRenderer.GetPropertyBlock(block);
        block.SetColor(FlashColorId, highlightColor);
        block.SetFloat(FlashAmountId, on ? highlightStrength : 0f);
        spriteRenderer.SetPropertyBlock(block);
    }

    /// <summary>Called on the server when a player interacts. Override for real behavior.</summary>
    public virtual void Interact(ulong clientId)
    {
        Debug.Log($"{name} interacted with by client {clientId}.");
    }
}
