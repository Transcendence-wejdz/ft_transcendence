using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

// Health (owner-written, replicated) and mana (local), with regen, damage, healing and revive.
public partial class Player
{
    private const float DefaultMaxHealth = 100f;
    private const float DefaultMaxMana = 50f;

    private float maxHealth;
    private float maxMana;

    private readonly NetworkVariable<float> currentHealth = new OfflineSafeNetworkVariable<float>(
        0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    public float MaxHealth => maxHealth + LevelUpHealthBonus;
    public float MaxMana => maxMana + LevelUpManaBonus;
    public float CurrentHealth => currentHealth.Value;
    public float CurrentMana { get; private set; }
    public bool IsDead => currentHealth.Value <= 0f;

    public static bool DeathProtected { get; set; }

    public static event Action<float, float> OnHealthChanged;
    public static event Action<float, float> OnManaChanged;
    public static event Action<Player> OnPlayerDied;
    public static event Action OnNotEnoughMana;

    public event Action<float, float> OnHealthReplicated;

    private void AwakeStats()
    {
        RefreshActiveKit();
        RefreshStatsFromKit();
        CurrentMana = MaxMana;
        currentHealth.OnValueChanged += HandleHealthValueChanged;
        OnClassOrColorChanged += HandleClassOrColorChanged;
        StartCoroutine(InitializeHealthNextFrame());
    }

    private void HandleHealthValueChanged(float previousValue, float newValue)
    {
        if (previousValue > 0f && newValue <= 0f)
            OnPlayerDied?.Invoke(this);
        OnHealthReplicated?.Invoke(newValue, MaxHealth);
        if (this.IsLocallyControlled())
            OnHealthChanged?.Invoke(newValue, MaxHealth);
    }

    private void HandleClassOrColorChanged(int classIndex, int colorIndex)
    {
        HandleClassChanged();
    }

    private IEnumerator InitializeHealthNextFrame()
    {
        yield return null;
        if (this.IsLocallyControlled())
            currentHealth.Value = MaxHealth;
    }

    private void StartStats()
    {
        if (!this.IsLocallyControlled())
            return;
        OnHealthChanged?.Invoke(CurrentHealth, MaxHealth);
        OnManaChanged?.Invoke(CurrentMana, MaxMana);
    }

    private void SpawnStats()
    {
        if (NetworkManager != null && NetworkManager.SceneManager != null)
            NetworkManager.SceneManager.OnLoadEventCompleted += HandleSceneLoadEventCompleted;
    }

    private void DespawnStats()
    {
        if (NetworkManager != null && NetworkManager.SceneManager != null)
            NetworkManager.SceneManager.OnLoadEventCompleted -= HandleSceneLoadEventCompleted;
    }

    private void HandleSceneLoadEventCompleted(string sceneName, LoadSceneMode loadSceneMode, List<ulong> clientsCompleted, List<ulong> clientsTimedOut)
    {
        ResetToFull();
    }

    public void ResetToFull()
    {
        if (!this.IsLocallyControlled())
            return;
        currentHealth.Value = MaxHealth;
        CurrentMana = MaxMana;
        OnManaChanged?.Invoke(CurrentMana, MaxMana);
    }

    private void UpdateStats()
    {
        if (!this.IsLocallyControlled() || activeKit == null || IsDead)
            return;
        if (CurrentHealth < MaxHealth)
            Heal((activeKit.HealthRegenPerSecond + LevelUpHealthRegenBonus) * Time.deltaTime);
        if (CurrentMana < MaxMana)
            RestoreMana((activeKit.ManaRegenPerSecond + LevelUpManaRegenBonus) * Time.deltaTime);
    }

    private void HandleClassChanged()
    {
        RefreshActiveKit();
        RefreshStatsFromKit();
        CurrentMana = Mathf.Min(CurrentMana, MaxMana);
        if (this.IsLocallyControlled())
            currentHealth.Value = Mathf.Min(currentHealth.Value, MaxHealth);
    }

    private void RefreshStatsFromKit()
    {
        maxHealth = DefaultMaxHealth;
        maxMana = DefaultMaxMana;
        if (activeKit == null)
            return;
        maxHealth = activeKit.MaxHealth;
        maxMana = activeKit.MaxMana;
    }

    public void Heal(float amount)
    {
        if (!this.IsLocallyControlled())
            return;
        currentHealth.Value = Mathf.Clamp(currentHealth.Value + amount, 0f, MaxHealth);
    }

    public bool TrySpendMana(float amount)
    {
        if (CurrentMana < amount)
        {
            if (this.IsLocallyControlled())
                OnNotEnoughMana?.Invoke();
            return false;
        }
        CurrentMana -= amount;
        if (this.IsLocallyControlled())
            OnManaChanged?.Invoke(CurrentMana, MaxMana);
        return true;
    }

    public void RestoreMana(float amount)
    {
        CurrentMana = Mathf.Clamp(CurrentMana + amount, 0f, MaxMana);
        if (this.IsLocallyControlled())
            OnManaChanged?.Invoke(CurrentMana, MaxMana);
    }
}
