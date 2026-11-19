// Dev cheats/tweaks, from DevMenu UI
using Unity.Netcode;
using UnityEngine;

public partial class Player
{
    // Dev flags
    public bool InfiniteHealth { get; private set; }
    public bool UnlimitedMana { get; private set; }
    public bool UnlimitedDashes { get; private set; }
    public bool NoCooldown { get; private set; }
    public bool FastAsFuck { get; private set; }
    // Replicated on network
    private readonly NetworkVariable<bool> kgbAgent = new OfflineSafeNetworkVariable<bool>(
        false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    public bool KgbAgent => kgbAgent.Value;
    // Persistence
    public static bool DevPrefInt(string key) => PlayerPrefs.GetInt("dev." + key) == 1;
    public static void SetDevPrefInt(string key, bool on) => PlayerPrefs.SetInt("dev." + key, on ? 1 : 0);
    public static float DevPrefFloat(string key) => PlayerPrefs.GetFloat("dev." + key);
    public static void SetDevPrefFloat(string key, float value) => PlayerPrefs.SetFloat("dev." + key, value);
    private void LoadDev()
    {
        DevInfiniteHealth(DevPrefInt("health"));
        DevUnlimitedMana(DevPrefInt("mana"));
        DevUnlimitedDashes(DevPrefInt("dashes"));
        DevNoCooldown(DevPrefInt("cooldown"));
        DevSpeedBoost(DevPrefInt("speed"));
        DevKgbAgent(DevPrefInt("kgb"));
    }

    // Setters with regens if applicable
    public void DevInfiniteHealth(bool on)
    {
        InfiniteHealth = on;
        if (on)
            Heal(MaxHealth);
    }
    public void DevUnlimitedMana(bool on)
    {
        UnlimitedMana = on;
        if (on)
            RestoreMana(MaxMana);
    }
    public void DevUnlimitedDashes(bool on)
    {
        UnlimitedDashes = on;
        if (on)
            dodgeCharges = MaxDodgeCharges;
    }
    public void DevNoCooldown(bool on) => NoCooldown = on;
    public void DevSpeedBoost(bool on) => FastAsFuck = on;
    public void DevKgbAgent(bool on) => kgbAgent.Value = on;

    private float DevCooldown(float cooldown) => NoCooldown ? 0.05f : cooldown;
}
