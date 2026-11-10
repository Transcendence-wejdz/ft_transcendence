using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Ready-check rows and team buttons, rebuilt for each proposal and kept in sync with joins and leaves.
public partial class ReadyCheckUI
{
    private static bool HasTeams(string[] teamNames)
    {
        return teamNames != null && teamNames.Length > 0;
    }

    private static void ChooseTeam(int teamIndex)
    {
        LocalPlayer.GetPlayer()?.SetTeam(teamIndex);
    }

    private void ClearTeamButtons()
    {
        foreach (RectTransform button in teamButtons)
        {
            if (button != null)
                Destroy(button.gameObject);
        }
        teamButtons.Clear();
    }

    private RectTransform CreateTeamButton(string teamName, int teamIndex)
    {
        RectTransform buttonRect = Instantiate(teamButtonTemplate, teamButtonTemplate.parent);
        TextMeshProUGUI label;
        Button button;

        buttonRect.gameObject.SetActive(true);
        label = buttonRect.GetComponentInChildren<TextMeshProUGUI>();
        if (label != null)
            label.text = teamName;
        button = buttonRect.GetComponent<Button>();
        if (button != null)
            button.onClick.AddListener(() => ChooseTeam(teamIndex));
        return buttonRect;
    }

    private void RebuildTeamButtons(string[] teamNames)
    {
        ClearTeamButtons();
        if (teamButtonContainer != null)
            teamButtonContainer.gameObject.SetActive(HasTeams(teamNames));
        if (teamButtonTemplate == null || teamNames == null)
            return;
        for (int i = 0; i < teamNames.Length; i++)
            teamButtons.Add(CreateTeamButton(teamNames[i], i));
    }

    private void AddRow(Player player)
    {
        GameObject rowObject;
        ReadyCheckEntryUI entry;

        if (rowTemplate == null)
            return;
        rowObject = Instantiate(rowTemplate.gameObject, rowTemplate.parent);
        rowObject.SetActive(true);
        entry = rowObject.GetComponent<ReadyCheckEntryUI>();
        entry.Bind(player, currentTeamNames);
        rows[player] = entry;
    }

    private void ClearRows()
    {
        foreach (ReadyCheckEntryUI row in rows.Values)
        {
            if (row != null)
                Destroy(row.gameObject);
        }
        rows.Clear();
    }

    private void RebuildRows()
    {
        ClearRows();
        foreach (Player player in Player.AllActiveInstances)
            AddRow(player);
    }

    // Adds a row when a player joins during a check.
    private void HandlePlayerRegistered(Player player)
    {
        if (isOpen && !rows.ContainsKey(player))
            AddRow(player);
    }

    private void HandlePlayerUnregistered(Player player)
    {
        ReadyCheckEntryUI entry;

        if (!rows.TryGetValue(player, out entry))
            return;
        if (entry != null)
            Destroy(entry.gameObject);
        rows.Remove(player);
    }
}
