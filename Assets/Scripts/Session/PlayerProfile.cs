using UnityEngine;

/// <summary>
/// Who you are and what you meant to do, carried across the local
/// (non-networked) scenes before a session exists: the menu decides host or
/// join, the character screen fills in name and skin, and only then does
/// anything connect.
///
/// Name and skin persist between launches, so a returning player can go
/// straight through the character screen with one press.
/// </summary>
public static class PlayerProfile
{
    public const int MaxNameLength = 12;

    private const string NameKey = "Spooky.PlayerName";
    private const string SkinKey = "Spooky.SkinIndex";

    public enum Intent { None, Host, Join }

    /// <summary>Set by the menu; read by the character screen's Continue.</summary>
    public static Intent PendingIntent { get; set; } = Intent.None;

    /// <summary>The code the menu already checked, used again to really connect.</summary>
    public static string PendingJoinCode { get; set; } = string.Empty;

    /// <summary>
    /// Shown once by the menu after being sent back to it — a rejected join,
    /// a host who left. Cleared when read.
    /// </summary>
    public static string MenuMessage { get; set; } = string.Empty;

    public static string Name
    {
        get => PlayerPrefs.GetString(NameKey, string.Empty);
        set
        {
            PlayerPrefs.SetString(NameKey, Sanitize(value));
            PlayerPrefs.Save();
        }
    }

    public static int SkinIndex
    {
        get => PlayerPrefs.GetInt(SkinKey, 0);
        set
        {
            PlayerPrefs.SetInt(SkinKey, value);
            PlayerPrefs.Save();
        }
    }

    public static string Sanitize(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return string.Empty;

        name = name.Trim();
        return name.Length > MaxNameLength ? name.Substring(0, MaxNameLength) : name;
    }

    public static string TakeMenuMessage()
    {
        string message = MenuMessage;
        MenuMessage = string.Empty;
        return message;
    }
}
