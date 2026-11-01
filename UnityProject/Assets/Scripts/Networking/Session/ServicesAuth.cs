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
    private const int MaxPlayerNameLength = 15;

    public static event Action<string> OnPlayerNameChanged;

    private static Task signInTask;

    // Unity services player id (the id Friends and sessions use for us), empty until signed in.
    public static string SignedInPlayerId
    {
        get
        {
            if (UnityServices.State != ServicesInitializationState.Initialized || !AuthenticationService.Instance.IsSignedIn)
                return string.Empty;
            return AuthenticationService.Instance.PlayerId;
        }
    }

    // Concurrent callers share one in-flight sign-in (two SignInAnonymouslyAsync calls throw); a failed or finished attempt is redone if still signed out.
    public static Task EnsureSignedInAsync()
    {
        if (UnityServices.State == ServicesInitializationState.Initialized && AuthenticationService.Instance.IsSignedIn)
            return Task.CompletedTask;
        if (signInTask == null || signInTask.IsCompleted)
            signInTask = SignInInternalAsync();
        return signInTask;
    }

    // Scoped to the web account's profile, so each signed-in user keeps their own Unity player.
    private static async Task InitializeServicesAsync()
    {
        InitializationOptions options = new InitializationOptions();

        if (WebProfileBridge.HasProfile && !string.IsNullOrEmpty(WebProfileBridge.UserId))
            options.SetProfile(WebProfileBridge.AuthProfileName);
        await UnityServices.InitializeAsync(options);
    }

    private static async Task SignInInternalAsync()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        await WebProfileBridge.WaitForProfileAsync(WebProfileTimeout);
#endif
        if (UnityServices.State != ServicesInitializationState.Initialized)
            await InitializeServicesAsync();
        if (AuthenticationService.Instance.IsSignedIn)
            return;
        await AuthenticationService.Instance.SignInAnonymouslyAsync();
        await SyncPlayerNameAsync();
        PublishPlayerId();
    }

    [Serializable]
    private class UnityIdLink
    {
        public string accessToken;
    }

    // Proves this account's Unity player to the site, which then finds its avatar from the id the game's friends lists know it by.
    private static void PublishPlayerId()
    {
        if (WebProfileBridge.HasProfile)
            WebBackend.Post("/api/game/unity-id", JsonUtility.ToJson(new UnityIdLink { accessToken = AuthenticationService.Instance.AccessToken }));
    }

    // Web display name with spaces turned to underscores, cut to the Unity name length.
    private static string DesiredPlayerName()
    {
        string desired = Regex.Replace(WebProfileBridge.ConfirmedName.Trim(), @"\s+", "_");

        if (desired.Length > MaxPlayerNameLength)
            desired = desired.Substring(0, MaxPlayerNameLength);
        return desired;
    }

    // Renames the Unity player (Friends) to the web display name; Unity keeps its own "#1234" suffix.
    public static async Task SyncPlayerNameAsync()
    {
        string desired;
        string current;
        string updated;

        if (!WebProfileBridge.HasProfile || !AuthenticationService.Instance.IsSignedIn)
            return;
        desired = DesiredPlayerName();
        if (string.IsNullOrEmpty(desired))
            return;
        try
        {
            current = await AuthenticationService.Instance.GetPlayerNameAsync();
            if (FriendsManager.ShortName(current) == desired)
                return;
            updated = await AuthenticationService.Instance.UpdatePlayerNameAsync(desired);
            OnPlayerNameChanged?.Invoke(updated);
        }
        catch (Exception e)
        {
            Debug.LogWarning($"ServicesAuth: could not set Unity player name to \"{desired}\". {e.Message}");
        }
    }
}
