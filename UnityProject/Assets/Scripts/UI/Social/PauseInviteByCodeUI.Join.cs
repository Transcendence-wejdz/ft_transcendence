using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Join by code: checked before closing the pause menu, then joined.
public partial class PauseInviteByCodeUI
{
    // An empty code or our own one never reaches the network; returns the error to show, or null.
    private string CodeError(string code)
    {
        if (string.IsNullOrEmpty(code))
            return "Enter a session code.";
        if (code == shownCode)
            return "That's your own session.";
        return null;
    }

    private async System.Threading.Tasks.Task<bool> CheckCodeAsync(string code)
    {
        (bool ok, string message) check;

        busy = true;
        joinButton.interactable = false;
        SetStatus("Looking for that game...", isError: false);
        check = await SessionJoin.CheckAsync(code);
        busy = false;
        if (this == null)
            return false;
        joinButton.interactable = true;
        if (!check.ok)
            SetStatus(check.message, isError: true);
        return check.ok;
    }

    private async void JoinChecked(string code)
    {
        (bool ok, string message) result;

        joinInput.text = string.Empty;
        SetStatus(string.Empty, isError: false);
        FindAnyObjectByType<PauseManager>()?.Resume();
        result = await SessionJoin.JoinAsync(code, alreadyChecked: true);
        if (this != null && !result.ok)
            SetStatus(result.message, isError: true);
    }

    // Checks the code before closing the pause menu, so a bad code costs nothing; then joining reloads or repopulates the scene under it.
    private async void Join()
    {
        string code = joinInput.text.Trim().ToUpperInvariant();
        string error = CodeError(code);

        if (error != null)
        {
            SetStatus(error, isError: true);
            return;
        }
        if (busy)
            return;
        if (await CheckCodeAsync(code))
            JoinChecked(code);
    }

    private void JoinFromInput(string text)
    {
        Join();
    }
}
