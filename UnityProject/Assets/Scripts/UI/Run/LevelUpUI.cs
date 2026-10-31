using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Level-up choice screen: solo has no timer; multiplayer adds a countdown and a side ready list.
public partial class LevelUpUI : MonoBehaviour
{
    private const float WarningMinAlpha = 0.35f;
    private const float MinBeatSeconds = 0.01f;

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

    private void SetButtonsInteractable(bool interactable)
    {
        foreach (Button button in bonusButtons)
            button.interactable = interactable;
    }

    // The name in the label font when one is set, else just bigger.
    private string BonusTitle(LevelUpBonusType type)
    {
        string displayName = LevelUpBonusCatalog.GetDisplayName(type);

        if (nameFont != null)
            return $"<font=\"{nameFont.name}\"><size=150%>{displayName}</size></font>";
        return $"<size=125%>{displayName}</size>";
    }

    private void LabelBonus(int slot)
    {
        LevelUpBonusType type = manager.GetOfferedBonusType(slot);

        bonusLabels[slot].text = $"<color=#EDE6D6>{BonusTitle(type)}</color>\n<color=#9E9C94>{LevelUpBonusCatalog.GetDescription(type)}</color>";
    }

    private void BeginShow()
    {
        localChosen = false;
        levelText.text = $"Level {manager.SequenceLevel}";
        waitingText.gameObject.SetActive(false);
        SetButtonsInteractable(true);
        for (int i = 0; i < bonusLabels.Length; i++)
        {
            if (bonusLabels[i] != null)
                LabelBonus(i);
        }
    }

    private void ChooseBonus(int bonusIndex)
    {
        Player local;

        if (localChosen)
            return;
        local = LocalPlayer.GetPlayer();
        if (local == null)
            return;
        local.ChooseLevelUpBonus(manager.GetOfferedBonusType(bonusIndex));
        localChosen = true;
        SetButtonsInteractable(false);
    }

    private void SetShown(bool active)
    {
        shown = active;
        IsShowing = active;
        panel.SetActive(active);
        PauseManager.SetExternalPause(active);
        if (active)
            BeginShow();
    }

    private void BindBonusButtons()
    {
        bonusLabels = new TMP_Text[bonusButtons.Length];
        for (int i = 0; i < bonusButtons.Length; i++)
        {
            bonusLabels[i] = bonusButtons[i].GetComponentInChildren<TMP_Text>();
            BindBonusButton(bonusButtons[i], i);
        }
    }

    // Separate so each listener captures its own slot index.
    private void BindBonusButton(Button button, int bonusIndex)
    {
        button.onClick.AddListener(() => ChooseBonus(bonusIndex));
    }

    private bool FindManager()
    {
        if (manager == null)
            manager = FindAnyObjectByType<LevelUpManager>();
        return manager != null;
    }

    void Awake()
    {
        IsShowing = false;
        panel.SetActive(false);
        if (nameFont != null)
            MaterialReferenceManager.AddFontAsset(nameFont);
        timerBaseColor = timerText.color;
        BindBonusButtons();
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
        bool active;

        if (!FindManager())
            return;
        active = manager.SequenceActive;
        if (active != shown)
            SetShown(active);
        if (!active)
            return;
        UpdateMultiplayerExtras();
        waitingText.gameObject.SetActive(localChosen);
    }
}
