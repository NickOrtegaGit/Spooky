using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Every selectable player look. The character screen cycles through these
/// and the chosen index travels with the connection request, so every client
/// resolves the same index to the same skin.
///
/// Order matters once players exist: an index is what gets sent, so
/// reordering entries changes what saved profiles point at.
/// </summary>
[CreateAssetMenu(menuName = "Spooky/Skin Catalog", fileName = "SkinCatalog")]
public class SkinCatalog : ScriptableObject
{
    [Serializable]
    public class Skin
    {
        public string displayName = "Employee";

        [Tooltip("Shown on the character screen.")]
        public Sprite preview;

        [Tooltip("Swapped onto the player's Animator in the house. Leave empty " +
                 "to keep the prefab's own controller.")]
        public RuntimeAnimatorController animator;
    }

    [SerializeField] private List<Skin> skins = new List<Skin>();

    public int Count => skins.Count;

    /// <summary>Wraps, so any index — including a stale saved one — is valid.</summary>
    public int Wrap(int index)
    {
        if (skins.Count == 0) return 0;
        return ((index % skins.Count) + skins.Count) % skins.Count;
    }

    public Skin Get(int index) => skins.Count == 0 ? null : skins[Wrap(index)];
}
