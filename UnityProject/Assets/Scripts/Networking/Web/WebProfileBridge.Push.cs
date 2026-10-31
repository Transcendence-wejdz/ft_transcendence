using System;
using System.Collections;
using System.Threading.Tasks;
using UnityEngine;

// In-game changes pushed back to the web account: class/color, name, and debounced settings.
public partial class WebProfileBridge
{
    // Saves a class/color change made in-game to the account, skipping values the backend already has.
    private static bool IsSuccess(int status)
    {
        return status >= 200 && status < 300;
    }

    private static bool IsValidSelection(int classIndex, int colorIndex)
    {
        if (classIndex < 0 || classIndex >= ClassNames.Length)
            return false;
        return colorIndex >= 0 && colorIndex < ColorNames.Length;
    }

    private static bool CanPush()
    {
        return HasProfile && !applyingRemote;
    }

    private static void HandleSelectionSaved(int status, int classIndex, int colorIndex)
    {
        if (!IsSuccess(status))
            return;
        confirmedClass = classIndex;
        confirmedColor = colorIndex;
    }

    public static void PushSelection(int classIndex, int colorIndex)
    {
        string body;

        if (!CanPush() || (classIndex == confirmedClass && colorIndex == confirmedColor))
            return;
        if (!IsValidSelection(classIndex, colorIndex))
            return;
        body = JsonUtility.ToJson(new SelectionPatch { @class = ClassNames[classIndex], color = ColorNames[colorIndex] });
        WebBackend.Patch(body, status => HandleSelectionSaved(status, classIndex, colorIndex));
    }

    // A rejected name (taken/invalid) reverts to the account's name.
    private static void HandleNameSaved(int status, string playerName)
    {
        if (IsSuccess(status))
        {
            ConfirmedName = playerName;
            _ = ServicesAuth.SyncPlayerNameAsync();
        }
        else if (status == 400 || status == 409)
        {
            Debug.LogWarning($"WebProfileBridge: name \"{playerName}\" rejected by the server (HTTP {status}), reverting.");
            LocalCharacterLook.SetPlayerName(ConfirmedName);
        }
    }

    // Saves an in-game rename to the account.
    public static void PushName(string playerName)
    {
        string body;

        if (!CanPush() || string.IsNullOrEmpty(playerName) || playerName == ConfirmedName)
            return;
        body = JsonUtility.ToJson(new NamePatch { display_name = playerName });
        WebBackend.Patch(body, status => HandleNameSaved(status, playerName));
    }

    // Debounced so a burst of rebinds/toggles sends a single request.
    private void QueueSettingsPush()
    {
        if (!CanPush())
            return;
        if (settingsPush != null)
            StopCoroutine(settingsPush);
        settingsPush = StartCoroutine(PushSettingsAfterDelay());
    }

    private IEnumerator PushSettingsAfterDelay()
    {
        yield return new WaitForSecondsRealtime(SettingsPushDelay);
        settingsPush = null;
        WebBackend.Patch(JsonUtility.ToJson(new SettingsPatch { settings = CurrentSettings() }));
    }
}
