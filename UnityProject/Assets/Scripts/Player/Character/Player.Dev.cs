// Dev cheats/tweaks, from DevMenu UI
using UnityEngine;

public partial class Player
{
    // Dev flags
    public bool InfiniteHealth { get; private set; }
    public bool UnlimitedMana { get; private set; }
    public bool UnlimitedDashes { get; private set; }
    public bool FastAsFuck { get; private set; }

    // Setters with regens if applicable
    public void DevInfiniteHealth(bool on)
    {
        InfiniteHealth = on;
        Debug.Log($"[Dev] InfiniteHealth={on} on {name} (local={this.IsLocallyControlled()})");
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
    public void DevSpeedBoost(bool on) => FastAsFuck = on;
}
