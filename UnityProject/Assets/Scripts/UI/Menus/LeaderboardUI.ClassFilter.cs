using UnityEngine;
using UnityEngine.UI;

// Class filter: keeps the runs with that class among the top scores, since the service only sorts by score.
public partial class LeaderboardUI
{
    private const int NoClassFilter = -1;
    private const int FilteredFetchLimit = 200;

    private static readonly Color FilterOffColor = new Color(0.45f, 0.45f, 0.45f, 1f);

    [Tooltip("Shows every class again.")]
    [SerializeField] private Button allClassesButton;
    [Tooltip("One button per class, in CharacterCatalog order.")]
    [SerializeField] private Button[] classFilterButtons;

    private int classFilter = NoClassFilter;

    private int FetchLimit()
    {
        if (classFilter == NoClassFilter)
            return topCount * maxTeamSize;
        return FilteredFetchLimit;
    }

    private bool PassesClassFilter(CoopRunStats stats)
    {
        if (classFilter == NoClassFilter)
            return true;
        return stats != null && stats.classes != null && System.Array.IndexOf(stats.classes, classFilter) >= 0;
    }

    private static void ShowFilterState(Button button, bool active)
    {
        button.targetGraphic.color = FilterOffColor;
        if (active)
            button.targetGraphic.color = Color.white;
    }

    private void RefreshFilterLook()
    {
        if (allClassesButton != null)
            ShowFilterState(allClassesButton, classFilter == NoClassFilter);
        for (int i = 0; i < classFilterButtons.Length; i++)
            ShowFilterState(classFilterButtons[i], classFilter == i);
    }

    private void SetClassFilter(int classIndex)
    {
        classFilter = classIndex;
        RefreshFilterLook();
        Refresh();
    }

    // Separate so each listener captures its own class.
    private void BindClassFilter(Button button, int classIndex)
    {
        button.onClick.AddListener(() => SetClassFilter(classIndex));
    }

    private void BindClassFilters()
    {
        if (allClassesButton != null)
            BindClassFilter(allClassesButton, NoClassFilter);
        for (int i = 0; i < classFilterButtons.Length; i++)
            BindClassFilter(classFilterButtons[i], i);
        RefreshFilterLook();
    }
}
