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

    // LB/RB step through the tabs in sidebar order, whatever their order in the array.
    private void Awake()
    {
        System.Array.Sort(tabs, (a, b) => a.tabButton.transform.GetSiblingIndex().CompareTo(b.tabButton.transform.GetSiblingIndex()));
        foreach (Tab tab in tabs)
        {
            Tab captured = tab;
            captured.tabButton.onClick.AddListener(() => Select(captured));
            if (captured.tabButton.GetComponent<DefaultSelection>() == null)
                captured.tabButton.gameObject.AddComponent<DefaultSelection>();
        }
    }

    private void OnEnable()
    {
        lastSelected = null;
        Select(tabs[0]);
    }

    // Moving between tabs switches the page; coming back from a page lands on the active tab instead of the nearest one.
    private void Update()
    {
        if (!InputModeTracker.UsingGamepad || KeybindsPanelUI.IsBusy || EventSystem.current == null)
            return;

        if (Gamepad.current != null && (Gamepad.current.leftShoulder.wasPressedThisFrame || Gamepad.current.rightShoulder.wasPressedThisFrame))
        {
            int step = Gamepad.current.rightShoulder.wasPressedThisFrame ? 1 : -1;
            Tab next = tabs[(System.Array.IndexOf(tabs, current) + step + tabs.Length) % tabs.Length];
            Select(next);
            GamepadUINavigator.Focus(next.tabButton);
        }

        GameObject selected = EventSystem.current.currentSelectedGameObject;
        if (selected != lastSelected)
        {
            Tab focused = TabOf(selected);
            if (focused != null && focused != current)
            {
                if (InSidebar(lastSelected))
                    Select(focused);
                else
                    GamepadUINavigator.Focus(current.tabButton);
            }
        }
        lastSelected = EventSystem.current.currentSelectedGameObject;
    }

    // Tabs and their neighbours (the Back button), so wrapping inside the sidebar is not taken for a return from the page.
    private bool InSidebar(GameObject selected) => selected != null && selected.transform.IsChildOf(tabs[0].tabButton.transform.parent);

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

    private void Select(Tab selected)
    {
        current = selected;
        foreach (Tab tab in tabs)
        {
            bool isSelected = tab == selected;
            tab.page.SetActive(isSelected);
            tab.tabButton.interactable = true;
            tab.tabButton.GetComponent<DefaultSelection>().enabled = isSelected;
            HoverColorWipe hover = tab.tabButton.GetComponent<HoverColorWipe>();
            if (hover != null)
                hover.SetPinned(isSelected);
        }
    }
}
