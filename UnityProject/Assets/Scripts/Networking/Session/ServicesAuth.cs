using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.Core;
using UnityEngine;

// Shared Unity Services init and anonymous sign-in, scoped to the signed-in web account when there is one.
public static class ServicesAuth
{
    private const float WebProfileTimeout = 5f;
    private const int MaxPlayerNameLength = 50;

    public static event Action<string> OnPlayerNameChanged;

    public static async Task EnsureSignedInAsync()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        await WebProfileBridge.WaitForProfileAsync(WebProfileTimeout);
#endif
        if (UnityServices.State != ServicesInitializationState.Initialized)
        {
            InitializationOptions options = new InitializationOptions();
            if (WebProfileBridge.HasProfile && !string.IsNullOrEmpty(WebProfileBridge.UserId))
                options.SetProfile(WebProfileBridge.AuthProfileName);
            await UnityServices.InitializeAsync(options);
        }

        if (!AuthenticationService.Instance.IsSignedIn)
        {
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
            await SyncPlayerNameAsync();
        }
    }

    // Renames the Unity player (Friends) to the web display name; Unity keeps its own "#1234" suffix.
    public static async Task SyncPlayerNameAsync()
    {
        if (!WebProfileBridge.HasProfile || !AuthenticationService.Instance.IsSignedIn)
            return;

        string desired = Regex.Replace(WebProfileBridge.ConfirmedName.Trim(), @"\s+", "_");
        if (desired.Length > MaxPlayerNameLength)
            desired = desired.Substring(0, MaxPlayerNameLength);
        if (string.IsNullOrEmpty(desired))
            return;

        try
        {
            string current = await AuthenticationService.Instance.GetPlayerNameAsync();
            if (FriendsManager.ShortName(current) == desired)
                return;

            string updated = await AuthenticationService.Instance.UpdatePlayerNameAsync(desired);
            OnPlayerNameChanged?.Invoke(updated);
        }
        catch (Exception e)
        {
            Debug.LogWarning($"ServicesAuth: could not set Unity player name to \"{desired}\". {e.Message}");
        }
    }
}
