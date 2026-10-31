using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// Drives a settings tab bar: one page visible at a time, the active tab pinned so its highlight sweeps in; with a gamepad, focusing a tab or LB/RB switches the page.
public class SettingsTabGroup : MonoBehaviour
{
    [System.Serializable]
    public class Tab
    {
        public Button tabButton;
        public GameObject page;
    }

    [SerializeField] private Tab[] tabs;

    private Tab current;
    private GameObject lastSelected;

    private static int SidebarOrder(Tab a, Tab b)
    {
        return a.tabButton.transform.GetSiblingIndex().CompareTo(b.tabButton.transform.GetSiblingIndex());
    }

    // Shoulder buttons step through the tabs: 1 for RB, -1 for LB, 0 for neither.
    private static int ShoulderStep()
    {
        Gamepad pad = Gamepad.current;

        if (pad == null)
            return 0;
        if (pad.rightShoulder.wasPressedThisFrame)
            return 1;
        if (pad.leftShoulder.wasPressedThisFrame)
            return -1;
        return 0;
    }

    // Tabs and their neighbours (the Back button), so wrapping inside the sidebar is not taken for a return from the page.
    private bool InSidebar(GameObject selected)
    {
        return selected != null && selected.transform.IsChildOf(tabs[0].tabButton.transform.parent);
    }

    private Tab TabOf(GameObject selected)
    {
        if (selected == null)
            return null;
        foreach (Tab tab in tabs)
        {
            if (tab.tabButton.gameObject == selected)
                return tab;
        }
        return null;
    }

    private void ShowTab(Tab tab, bool isSelected)
    {
        HoverColorWipe hover = tab.tabButton.GetComponent<HoverColorWipe>();

        tab.page.SetActive(isSelected);
        tab.tabButton.interactable = true;
        tab.tabButton.GetComponent<DefaultSelection>().enabled = isSelected;
        if (hover != null)
            hover.SetPinned(isSelected);
    }

    private void Select(Tab selected)
    {
        current = selected;
        foreach (Tab tab in tabs)
            ShowTab(tab, tab == selected);
    }

    private void BindTab(Tab tab)
    {
        tab.tabButton.onClick.AddListener(() => Select(tab));
        if (tab.tabButton.GetComponent<DefaultSelection>() == null)
            tab.tabButton.gameObject.AddComponent<DefaultSelection>();
    }

    private void StepTab(int step)
    {
        Tab next = tabs[(System.Array.IndexOf(tabs, current) + step + tabs.Length) % tabs.Length];

        Select(next);
        GamepadUINavigator.Focus(next.tabButton);
    }

    // Moving between tabs switches the page; coming back from a page lands on the active tab instead of the nearest one.
    private void FollowFocus(GameObject selected)
    {
        Tab focused = TabOf(selected);

        if (focused == null || focused == current)
            return;
        if (InSidebar(lastSelected))
            Select(focused);
        else
            GamepadUINavigator.Focus(current.tabButton);
    }

    // LB/RB step through the tabs in sidebar order, whatever their order in the array.
    private void Awake()
    {
        System.Array.Sort(tabs, SidebarOrder);
        foreach (Tab tab in tabs)
            BindTab(tab);
    }

    private void OnEnable()
    {
        lastSelected = null;
        Select(tabs[0]);
    }

    private void Update()
    {
        int step;
        GameObject selected;

        if (!InputModeTracker.UsingGamepad || KeybindsPanelUI.IsBusy || EventSystem.current == null)
            return;
        step = ShoulderStep();
        if (step != 0)
            StepTab(step);
        selected = EventSystem.current.currentSelectedGameObject;
        if (selected != lastSelected)
            FollowFocus(selected);
        lastSelected = EventSystem.current.currentSelectedGameObject;
    }
}
