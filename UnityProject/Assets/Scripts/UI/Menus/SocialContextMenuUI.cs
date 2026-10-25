using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
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

    [SerializeField] private RectTransform panel;
    [SerializeField] private Button itemTemplate;
    [SerializeField] private Button blocker;
    [SerializeField] private float gap = 6f;
    [SerializeField] private Color itemColor = new Color(0.45f, 0.45f, 0.48f, 1f);
    [SerializeField] private Color destructiveColor = new Color(0.72f, 0.16f, 0.16f, 1f);

    private readonly List<Button> items = new List<Button>();

    private void Awake()
    {
        itemTemplate.gameObject.SetActive(false);
        blocker.onClick.AddListener(Close);
    }

    // Hidden menus never come back stale when the pause reopens.
    private void OnDisable()
    {
        gameObject.SetActive(false);
    }

    public void Close()
    {
        gameObject.SetActive(false);
    }

    // Opens to the left of the anchor, top-aligned with it; nothing to show means no menu.
    public void Open(RectTransform anchor, List<Item> entries)
    {
        foreach (Button item in items)
            Destroy(item.gameObject);
        items.Clear();
        if (entries.Count == 0)
            return;

        foreach (Item entry in entries)
            items.Add(CreateItem(entry));

        gameObject.SetActive(true);
        var corners = new Vector3[4];
        anchor.GetWorldCorners(corners);
        panel.position = corners[1];
        panel.anchoredPosition += Vector2.left * gap;
    }

    private Button CreateItem(Item entry)
    {
        Button button = Instantiate(itemTemplate, itemTemplate.transform.parent);
        button.gameObject.SetActive(true);
        TMP_Text label = button.GetComponentInChildren<TMP_Text>();
        label.text = entry.Label;
        button.GetComponent<Image>().color = entry.Destructive ? destructiveColor : itemColor;

        bool armed = false;
        button.onClick.AddListener(() =>
        {
            if (entry.Destructive && !armed)
            {
                armed = true;
                label.text = "Confirm?";
                return;
            }
            Close();
            entry.Action?.Invoke();
        });
        return button;
    }
}
