using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

// Replicated look changes turned into visuals and events for the UI.
public partial class Player
{
    private void ApplyVisuals()
    {
        if (CharacterCatalog.Instance != null)
            CharacterCatalog.Instance.ApplyVisuals(gameObject, classIndex.Value, colorIndex.Value);
    }

    private void RaiseClassOrColorChanged()
    {
        ApplyVisuals();
        OnClassOrColorChanged?.Invoke(classIndex.Value, colorIndex.Value);
    }

    private void HandleLookChanged(int previous, int current)
    {
        RaiseClassOrColorChanged();
    }

    private void HandleNameChanged(FixedString32Bytes previous, FixedString32Bytes current)
    {
        RefreshAllDisplayNames();
    }

    private void HandleTeamChanged(int previous, int current)
    {
        OnTeamChanged?.Invoke(current);
    }

    private void HandleReadyChanged(bool previous, bool current)
    {
        OnReadyChanged?.Invoke(current);
    }

    private void AwakeCustomization()
    {
        classIndex.OnValueChanged += HandleLookChanged;
        colorIndex.OnValueChanged += HandleLookChanged;
        playerName.OnValueChanged += HandleNameChanged;
        teamIndex.OnValueChanged += HandleTeamChanged;
        isReady.OnValueChanged += HandleReadyChanged;
    }
}
