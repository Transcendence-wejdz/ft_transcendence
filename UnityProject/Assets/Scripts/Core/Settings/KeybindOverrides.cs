using UnityEngine;
using UnityEngine.InputSystem;

// Local keyboard rebind overrides, persisted via PlayerPrefs and reapplied to each player's cloned InputActionAsset.
public static class KeybindOverrides
{
    private const string OverridesKey = "Settings.KeyBindingOverrides";

    public static event System.Action OnSaved;

    public static string Json => PlayerPrefs.GetString(OverridesKey, "");

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
        PlayerPrefs.SetString(OverridesKey, json ?? "");
        PlayerPrefs.Save();

        GameObject player = LocalPlayer.Get();
        PlayerInput input = player != null ? player.GetComponent<PlayerInput>() : null;
        if (input == null || input.actions == null)
            return;

        input.actions.RemoveAllBindingOverrides();
        Apply(input.actions);
    }

    // Effective (override-aware) path of an action's first gamepad binding, e.g. "<Gamepad>/buttonSouth".
    public static string GetGamepadPath(string actionName)
    {
        GameObject player = LocalPlayer.Get();
        PlayerInput input = player != null ? player.GetComponent<PlayerInput>() : null;
        InputAction action = input != null ? input.actions?.FindAction(actionName) : null;
        if (action == null)
            return null;

        foreach (InputBinding binding in action.bindings)
        {
            string path = binding.effectivePath;
            if (!binding.isComposite && !binding.isPartOfComposite && path != null && path.StartsWith("<Gamepad>"))
                return path;
        }
        return null;
    }

    // Display string for an action's keyboard binding, falling back to the raw path for keys Unity can't render as text.
    public static string GetKeyboardDisplay(string actionName)
    {
        GameObject player = LocalPlayer.Get();
        PlayerInput input = player != null ? player.GetComponent<PlayerInput>() : null;
        InputAction action = input != null ? input.actions?.FindAction(actionName) : null;
        if (action == null)
            return null;

        for (int i = 0; i < action.bindings.Count; i++)
        {
            InputBinding binding = action.bindings[i];
            if (binding.isComposite || binding.isPartOfComposite || binding.path == null || !binding.path.StartsWith("<Keyboard>"))
                continue;

            string display = action.GetBindingDisplayString(i, InputBinding.DisplayStringOptions.DontIncludeInteractions);
            if (!string.IsNullOrEmpty(display) && display.Trim().Length > 0)
                return display;

            string path = binding.path;
            int slash = path.LastIndexOf('/');
            string keyName = slash >= 0 ? path.Substring(slash + 1) : path;
            return keyName.Length > 0 ? char.ToUpper(keyName[0]) + keyName.Substring(1) : "?";
        }
        return null;
    }
}
