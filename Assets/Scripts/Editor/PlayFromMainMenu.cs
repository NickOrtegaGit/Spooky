using UnityEditor;
using UnityEditor.SceneManagement;

/// <summary>
/// Pressing Play always starts from MainMenu, whatever scene is open. The
/// NetworkManager lives there now, so playing the house directly would have
/// no session at all. Toggle it off under Tools/Spooky to test a scene alone.
/// </summary>
[InitializeOnLoad]
public static class PlayFromMainMenu
{
    private const string MenuPath = "Tools/Spooky/Play From Main Menu";
    private const string PrefKey = "Spooky.PlayFromMainMenu";
    private const string ScenePath = "Assets/Scenes/MainMenu.unity";

    static PlayFromMainMenu()
    {
        // Wait a tick: the asset database is not ready during domain reload.
        EditorApplication.delayCall += Apply;
    }

    private static bool Enabled
    {
        get => EditorPrefs.GetBool(PrefKey, true);
        set => EditorPrefs.SetBool(PrefKey, value);
    }

    [MenuItem(MenuPath)]
    private static void Toggle()
    {
        Enabled = !Enabled;
        Apply();
    }

    [MenuItem(MenuPath, true)]
    private static bool ToggleValidate()
    {
        Menu.SetChecked(MenuPath, Enabled);
        return true;
    }

    private static void Apply()
    {
        EditorSceneManager.playModeStartScene = Enabled
            ? AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath)
            : null;
    }
}
