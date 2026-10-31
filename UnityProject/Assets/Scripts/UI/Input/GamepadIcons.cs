using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

// Gamepad button sprites from the gdb-gamepad-2 sheets in Resources, styled after the current pad (PlayStation, Switch, else Xbox).
public static class GamepadIcons
{
    private const string SheetPath = "UI/gdb-gamepad-2(all)/gdb-";

    private static readonly Dictionary<string, Dictionary<string, Sprite>> sheets = new Dictionary<string, Dictionary<string, Sprite>>();

    // Sheet prefix matching Gamepad.current, Xbox when no pad or an unknown one is connected.
    public static string CurrentStyle()
    {
        Gamepad pad = Gamepad.current;

        if (pad == null)
            return "xbox";
        if (InputSystem.IsFirstLayoutBasedOnSecond(pad.layout, "DualShockGamepad"))
            return "playstation";
        if (pad.layout.Contains("Switch"))
            return "switch";
        return "xbox";
    }

    // Icon for a binding path like "<Gamepad>/buttonSouth" or "<Gamepad>/dpad/up", null when the sheet has none.
    public static Sprite ForPath(string bindingPath)
    {
        string control = ControlName(bindingPath);
        string style;
        Sprite sprite;

        if (control == null)
            return null;
        style = CurrentStyle();
        if (Sheet(style).TryGetValue(style + "_" + control, out sprite))
            return sprite;
        return null;
    }

    // "buttonSouth" from "<Gamepad>/buttonSouth", "dpad" from "<Gamepad>/dpad/up"; null when there is no control part.
    private static string ControlName(string bindingPath)
    {
        int slash;
        string control;
        int sub;

        if (string.IsNullOrEmpty(bindingPath))
            return null;
        slash = bindingPath.IndexOf('/');
        if (slash < 0)
            return null;
        control = bindingPath.Substring(slash + 1);
        sub = control.IndexOf('/');
        if (sub >= 0)
            control = control.Substring(0, sub);
        return control;
    }

    private static Dictionary<string, Sprite> Sheet(string style)
    {
        Dictionary<string, Sprite> sheet;

        if (sheets.TryGetValue(style, out sheet))
            return sheet;
        sheet = new Dictionary<string, Sprite>();
        foreach (Sprite sprite in Resources.LoadAll<Sprite>(SheetPath + style + "-2"))
            sheet[sprite.name] = sprite;
        sheets[style] = sheet;
        return sheet;
    }
}
