using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

// Downloads players' site avatars by Unity id, one request per player; a miss is only asked again after a while, since friends lists rebuild often.
public static class SiteAvatarCache
{
    private const float MissRetrySeconds = 60f;
    private const string AvatarPath = "/api/game/avatar/";

    private struct Entry
    {
        public Texture2D texture;
        public float retryAt;
    }

    private static readonly Dictionary<string, Entry> Entries = new Dictionary<string, Entry>();
    private static readonly Dictionary<string, List<Action<Texture2D>>> Pending = new Dictionary<string, List<Action<Texture2D>>>();

    private static string UrlFor(string unityId)
    {
        return new Uri(Application.absoluteURL).GetLeftPart(UriPartial.Authority) + AvatarPath + UnityWebRequest.EscapeURL(unityId);
    }

    // WebP and GIF avatars fail to decode here, which simply leaves the player's initial showing.
    private static void Finish(string unityId, UnityWebRequest request)
    {
        Texture2D texture = null;
        List<Action<Texture2D>> callbacks;

        if (request.result == UnityWebRequest.Result.Success)
            texture = DownloadHandlerTexture.GetContent(request);
        request.Dispose();
        Entries[unityId] = new Entry { texture = texture, retryAt = Time.unscaledTime + MissRetrySeconds };
        callbacks = Pending[unityId];
        Pending.Remove(unityId);
        foreach (Action<Texture2D> callback in callbacks)
            callback(texture);
    }

    // onDone gets the texture, or null when the player has none or this isn't the website.
    public static void Request(string unityId, Action<Texture2D> onDone)
    {
        Entry entry;
        UnityWebRequest request;

        if (!WebProfileBridge.HasProfile || string.IsNullOrEmpty(unityId))
        {
            onDone(null);
            return;
        }
        if (Entries.TryGetValue(unityId, out entry) && (entry.texture != null || Time.unscaledTime < entry.retryAt))
        {
            onDone(entry.texture);
            return;
        }
        if (Pending.ContainsKey(unityId))
        {
            Pending[unityId].Add(onDone);
            return;
        }
        Pending[unityId] = new List<Action<Texture2D>> { onDone };
        request = UnityWebRequestTexture.GetTexture(UrlFor(unityId), true);
        request.SendWebRequest().completed += operation => Finish(unityId, request);
    }
}
