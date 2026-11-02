using System.Collections.Generic;
using UnityEngine;

// Single owner of the game freeze: time only runs again once every system that froze it has let go.
public static class TimeFreeze
{
    private static readonly HashSet<object> holders = new HashSet<object>();

    private static void Apply()
    {
        if (holders.Count > 0)
            Time.timeScale = 0f;
        else
            Time.timeScale = 1f;
    }

    public static void Set(object holder, bool frozen)
    {
        if (frozen)
            holders.Add(holder);
        else
            holders.Remove(holder);
        Apply();
    }

    // Leaving for another scene: nothing that froze time carries over.
    public static void Clear()
    {
        holders.Clear();
        Apply();
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetOnPlay()
    {
        holders.Clear();
    }
}
