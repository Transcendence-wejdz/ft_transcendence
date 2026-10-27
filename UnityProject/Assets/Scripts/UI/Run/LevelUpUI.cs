using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Level-up choice screen: solo has no timer; multiplayer adds a countdown and a side ready list.
public class LevelUpUI : MonoBehaviour
{
    [SerializeField] private GameObject panel;
    [SerializeField] private TMP_Text levelText;
    [SerializeField] private Button[] bonusButtons;
    [SerializeField] private GameObject timerGroup;
    [SerializeField] private TMP_Text timerText;
    [SerializeField] private GameObject readyListGroup;
    [SerializeField] private TMP_Text readyListText;
    [SerializeField] private TMP_Text waitingText;
    [SerializeField] private TMP_FontAsset nameFont;
    [SerializeField] private float warningSeconds = 5f;
    [SerializeField] private Color warningColor = new Color(1f, 0.24f, 0.24f, 1f);
    [SerializeField] private float warningPulseScale = 1.3f;
    [SerializeField] private float warningBeatSeconds = 0.35f;

    private readonly StringBuilder readyListBuilder = new StringBuilder();

    private LevelUpManager manager;
    private TMP_Text[] bonusLabels;
    private Color timerBaseColor;
    private int lastTimerSecond = -1;
    private float lastTimerTickTime;
    private bool localChosen;
    private bool shown;

    public static bool IsShowing { get; private set; }

    void Awake()
    {
        IsShowing = false;
        panel.SetActive(false);
        if (nameFont != null)
            MaterialReferenceManager.AddFontAsset(nameFont);
        timerBaseColor = timerText.color;

        bonusLabels = new TMP_Text[bonusButtons.Length];
        for (int i = 0; i < bonusButtons.Length; i++)
        {
            int bonusIndex = i;
            bonusLabels[i] = bonusButtons[i].GetComponentInChildren<TMP_Text>();
            bonusButtons[i].onClick.AddListener(() => ChooseBonus(bonusIndex));
        }
    }

    // Releases the input block too if the scene unloads while the choice is still open.
    void OnDestroy()
    {
        IsShowing = false;
        if (shown)
            PauseManager.SetExternalPause(false);
    }

    void Update()
    {
        if (manager == null)
        {
            manager = FindAnyObjectByType<LevelUpManager>();
            if (manager == null)
                return;
        }

        bool active = manager.SequenceActive;
        if (active != shown)
        {
            shown = active;
            IsShowing = active;
            panel.SetActive(active);
            PauseManager.SetExternalPause(active);
            if (active)
                BeginShow();
        }

        if (!active)
            return;

        bool multi = Player.AllActiveInstances.Count > 1;
        timerGroup.SetActive(multi);
        readyListGroup.SetActive(multi);

        if (multi)
        {
            int second = Mathf.CeilToInt(manager.SecondsRemaining);
            timerText.text = second.ToString();
            PaintTimerWarning(second);
            RefreshReadyList();
        }

        waitingText.gameObject.SetActive(localChosen);
    }

    // Last seconds: red heartbeat timed locally from each tick, since the server only replicates whole seconds.
    private void PaintTimerWarning(int second)
    {
        if (second != lastTimerSecond)
        {
            lastTimerSecond = second;
            lastTimerTickTime = Time.unscaledTime;
        }

        if (second > warningSeconds || second <= 0)
        {
            timerText.color = timerBaseColor;
            timerText.rectTransform.localScale = Vector3.one;
            return;
        }

        float sinceTick = Time.unscaledTime - lastTimerTickTime;
        float fade = 1f - Mathf.Clamp01(sinceTick);
        float beat = 1f - Mathf.Clamp01(sinceTick / Mathf.Max(warningBeatSeconds, 0.01f));
        Color color = warningColor;
        color.a = Mathf.Lerp(0.35f, 1f, fade);
        timerText.color = color;
        timerText.rectTransform.localScale = Vector3.one * Mathf.Lerp(1f, warningPulseScale, beat * beat);
    }

    private void BeginShow()
    {
        localChosen = false;
        levelText.text = $"Level {manager.SequenceLevel}";
        waitingText.gameObject.SetActive(false);
        SetButtonsInteractable(true);

        for (int i = 0; i < bonusLabels.Length; i++)
        {
            if (bonusLabels[i] == null)
                continue;
            LevelUpBonusType type = manager.GetOfferedBonusType(i);
            string bonusName = nameFont != null ? $"<font=\"{nameFont.name}\"><size=150%>{LevelUpBonusCatalog.GetDisplayName(type)}</size></font>" : $"<size=125%>{LevelUpBonusCatalog.GetDisplayName(type)}</size>";
            bonusLabels[i].text = $"<color=#EDE6D6>{bonusName}</color>\n<color=#9E9C94>{LevelUpBonusCatalog.GetDescription(type)}</color>";
        }
    }

    private void ChooseBonus(int bonusIndex)
    {
        if (localChosen)
            return;

        Player local = LocalPlayer.GetPlayer();
        if (local == null)
            return;

        local.ChooseLevelUpBonus(manager.GetOfferedBonusType(bonusIndex));
        localChosen = true;
        SetButtonsInteractable(false);
    }

    private void SetButtonsInteractable(bool interactable)
    {
        foreach (Button button in bonusButtons)
            button.interactable = interactable;
    }

    private void RefreshReadyList()
    {
        readyListBuilder.Clear();
        foreach (Player player in Player.AllActiveInstances)
            readyListBuilder.AppendLine($"{player.PlayerName}: {(player.HasChosenLevelUpBonus ? "Ready" : "...")}");
        readyListText.text = readyListBuilder.ToString();
    }
}
