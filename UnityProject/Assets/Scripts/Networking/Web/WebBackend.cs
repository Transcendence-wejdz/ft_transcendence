using System;
using System.Collections.Generic;
using AOT;
using UnityEngine;
#if UNITY_WEBGL && !UNITY_EDITOR
using System.Runtime.InteropServices;
#endif

// Sends in-game profile/settings changes to the web backend; no-op outside a WebGL build.
public static class WebBackend
{
    private delegate void PatchCallback(int requestId, int status);

    private static readonly Dictionary<int, Action<int>> Pending = new Dictionary<int, Action<int>>();

#if UNITY_WEBGL && !UNITY_EDITOR
    private static int nextRequestId;

    [DllImport("__Internal")]
    private static extern void WebBackend_Patch(string body, int requestId, PatchCallback callback);

    [DllImport("__Internal")]
    private static extern void WebBackend_Post(string path, string body, int requestId, PatchCallback callback);
#endif

    // PATCHes /api/game/profile with a JSON body; onDone receives the HTTP status (0 = network error).
    public static void Patch(string json, Action<int> onDone = null)
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        int requestId = ++nextRequestId;
        if (onDone != null)
            Pending[requestId] = onDone;
        WebBackend_Patch(json, requestId, OnPatchDone);
#endif
    }

    // POSTs a JSON body to a same-origin API path (e.g. /api/game/reward); onDone receives the HTTP status (0 = network error).
    public static void Post(string path, string json, Action<int> onDone = null)
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        int requestId = ++nextRequestId;
        if (onDone != null)
            Pending[requestId] = onDone;
        WebBackend_Post(path, json, requestId, OnPatchDone);
#endif
    }

    [MonoPInvokeCallback(typeof(PatchCallback))]
    private static void OnPatchDone(int requestId, int status)
    {
        Action<int> onDone;

        if (status < 200 || status >= 300)
            Debug.LogWarning($"WebBackend: request failed with HTTP {status}.");
        if (!Pending.TryGetValue(requestId, out onDone))
            return;
        Pending.Remove(requestId);
        onDone(status);
    }
}
