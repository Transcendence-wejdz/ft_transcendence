using System;
using UnityEngine;

// Scene night mode: while a scene holding this is loaded, every NightLight (player lanterns, enemy torches) is on.
public class NightLighting : MonoBehaviour
{
    public static bool IsNight { get; private set; }
    public static event Action<bool> OnNightChanged;

    private static void SetNight(bool night)
    {
        if (IsNight == night)
            return;
        IsNight = night;
        OnNightChanged?.Invoke(night);
    }

    void OnEnable()
    {
        SetNight(true);
    }

    void OnDisable()
    {
        SetNight(false);
    }
}
