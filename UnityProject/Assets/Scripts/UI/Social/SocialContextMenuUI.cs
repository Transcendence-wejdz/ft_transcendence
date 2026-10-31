using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Small click menu opened next to a player entry; destructive items need a second click, clicking outside or hiding the pause closes it.
public class SocialContextMenuUI : MonoBehaviour
{
    public struct Item
    {
        public string Label;
        public Action Action;
        public bool Destructive;

        public Item(string label, Action action, bool destructive = false)
        {
            Label = label;
            Action = action;
            Destructive = destructive;
        }
    }

    // One menu button's click: a destructive item arms on the first click and acts on the second.
    private class ItemClick
    {
        public SocialContextMenuUI menu;
        public Item entry;
        public TMP_Text label;
        public bool armed;

        public void OnClick()
        {
            if (entry.Destructive && !armed)
            {
                armed = true;
                label.text = "Confirm?";
                return;
            }
            menu.Close();
            entry.Action?.Invoke();
        }
    }

    private static SocialContextMenuUI open;

    [SerializeField] private RectTransform panel;
    [SerializeField] private Button itemTemplate;
    [SerializeField] private Button blocker;
    [SerializeField] private float gap = 6f;
    [SerializeField] private Color itemColor = new Color(0.45f, 0.45f, 0.48f, 1f);
    [SerializeField] private Color destructiveColor = new Color(0.72f, 0.16f, 0.16f, 1f);

    private readonly List<Button> items = new List<Button>();
    private Selectable returnFocus;

    // The open menu, which keeps gamepad focus to itself like a modal.
    public static Transform OpenMenu
    {
        get
        {
            if (open == null)
                return null;
            return open.transform;
        }
    }

    private static Selectable CurrentSelection()
    {
        GameObject current;

        if (EventSystem.current == null)
            return null;
        current = EventSystem.current.currentSelectedGameObject;
        if (current == null)
            return null;
        return current.GetComponent<Selectable>();
    }

    private Color ItemColor(Item entry)
    {
        if (entry.Destructive)
            return destructiveColor;
        return itemColor;
    }

    private Button CreateItem(Item entry)
    {
        Button button = Instantiate(itemTemplate, itemTemplate.transform.parent);
        TMP_Text label;
        ItemClick click;

        button.gameObject.SetActive(true);
        label = button.GetComponentInChildren<TMP_Text>();
        label.text = entry.Label;
        button.GetComponent<Image>().color = ItemColor(entry);
        click = new ItemClick { menu = this, entry = entry, label = label };
        button.onClick.AddListener(click.OnClick);
        return button;
    }

    private void ClearItems()
    {
        foreach (Button item in items)
            Destroy(item.gameObject);
        items.Clear();
    }

    // To the left of the anchor, top-aligned with it.
    private void PlaceNextTo(RectTransform anchor)
    {
        Vector3[] corners = new Vector3[4];

        anchor.GetWorldCorners(corners);
        panel.position = corners[1];
        panel.anchoredPosition += Vector2.left * gap;
    }

    // Closes whichever context menu is open (gamepad Back); false when none was.
    public static bool CloseOpen()
    {
        if (open == null)
            return false;
        open.Close();
        return true;
    }

    public void Close()
    {
        gameObject.SetActive(false);
    }

    // Nothing to show means no menu.
    public void Open(RectTransform anchor, List<Item> entries)
    {
        ClearItems();
        if (entries.Count == 0)
            return;
        foreach (Item entry in entries)
            items.Add(CreateItem(entry));
        returnFocus = CurrentSelection();
        open = this;
        gameObject.SetActive(true);
        PlaceNextTo(anchor);
        GamepadUINavigator.Focus(items[0]);
    }

    private void Awake()
    {
        itemTemplate.gameObject.SetActive(false);
        blocker.onClick.AddListener(Close);
    }

    // Hidden menus never come back stale when the pause reopens; the gamepad focus goes back to the entry that opened it.
    private void OnDisable()
    {
        gameObject.SetActive(false);
        if (open != this)
            return;
        open = null;
        if (returnFocus != null && returnFocus.IsActive())
            GamepadUINavigator.Focus(returnFocus);
        returnFocus = null;
    }
}
