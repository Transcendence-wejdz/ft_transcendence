using UnityEngine;
using UnityEngine.InputSystem;

// Local keyboard rebind overrides, persisted via PlayerPrefs and reapplied to each player's cloned InputActionAsset.
public static class KeybindOverrides
{
    private const string OverridesKey = "Settings.KeyBindingOverrides";

    public static event System.Action OnSaved;

    public static string Json => PlayerPrefs.GetString(OverridesKey, "");

    private static PlayerInput GetLocalInput()
    {
        GameObject player = LocalPlayer.Get();

        if (player == null)
            return null;
        return player.GetComponent<PlayerInput>();
    }

    private static InputAction FindLocalAction(string actionName)
    {
        PlayerInput input = GetLocalInput();

        if (input == null)
            return null;
        return input.actions?.FindAction(actionName);
    }

    // "Escape" from "<Keyboard>/escape", for keys Unity can't render as text.
    public static string KeyNameFromPath(string path)
    {
        int slash = path.LastIndexOf('/');
        string keyName = path;

        if (slash >= 0)
            keyName = path.Substring(slash + 1);
        if (keyName.Length == 0)
            return "?";
        return char.ToUpper(keyName[0]) + keyName.Substring(1);
    }

    private static bool IsSingleBinding(InputBinding binding, string pathPrefix, string path)
    {
        if (binding.isComposite || binding.isPartOfComposite)
            return false;
        return path != null && path.StartsWith(pathPrefix);
    }

    public static void Apply(InputActionAsset asset)
    {
        string json = PlayerPrefs.GetString(OverridesKey, "");

        if (!string.IsNullOrEmpty(json))
            asset.LoadBindingOverridesFromJson(json);
    }

    public static void Save(InputActionAsset asset)
    {
        PlayerPrefs.SetString(OverridesKey, asset.SaveBindingOverridesAsJson());
        PlayerPrefs.Save();
        OnSaved?.Invoke();
    }

    // Replaces the stored overrides (e.g. from the web account) and reapplies them to the local player.
    public static void SetJson(string json)
    {
        PlayerInput input;

        PlayerPrefs.SetString(OverridesKey, json ?? "");
        PlayerPrefs.Save();
        input = GetLocalInput();
        if (input == null || input.actions == null)
            return;
        input.actions.RemoveAllBindingOverrides();
        Apply(input.actions);
    }

    // Effective (override-aware) path of an action's first gamepad binding, e.g. "<Gamepad>/buttonSouth".
    public static string GetGamepadPath(string actionName)
    {
        InputAction action = FindLocalAction(actionName);

        if (action == null)
            return null;
        foreach (InputBinding binding in action.bindings)
        {
            if (IsSingleBinding(binding, "<Gamepad>", binding.effectivePath))
                return binding.effectivePath;
        }
        return null;
    }

    // Display string for an action's keyboard binding, falling back to the raw path for keys Unity can't render as text.
    public static string GetKeyboardDisplay(string actionName)
    {
        InputAction action = FindLocalAction(actionName);
        InputBinding binding;
        string display;

        if (action == null)
            return null;
        for (int i = 0; i < action.bindings.Count; i++)
        {
            binding = action.bindings[i];
            if (!IsSingleBinding(binding, "<Keyboard>", binding.path))
                continue;
            display = action.GetBindingDisplayString(i, InputBinding.DisplayStringOptions.DontIncludeInteractions);
            if (!string.IsNullOrEmpty(display) && display.Trim().Length > 0)
                return display;
            return KeyNameFromPath(binding.path);
        }
        return null;
    }
}
