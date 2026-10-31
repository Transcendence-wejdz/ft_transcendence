using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Bottom-center shared XP bar + level indicator under the top-left avatar, both shown once waves start (same gate as CoopHUD).
public class XPBarUI : MonoBehaviour
{
    private const float SearchInterval = 0.5f;

    [SerializeField] private GameObject content;
    [SerializeField] private Image fillImage;
    [SerializeField] private TMP_Text levelText;
    [SerializeField] private bool showBar = true;

    private XPManager xpManager;
    private WaveSpawner spawner;
    private float nextSearchTime;

    // Run managers are absent outside a run (Lobby), so the lookup is throttled instead of searching every frame.
    private bool FindRunManagers()
    {
        if (xpManager != null && spawner != null)
            return true;
        if (Time.unscaledTime < nextSearchTime)
            return false;
        nextSearchTime = Time.unscaledTime + SearchInterval;
        xpManager = FindAnyObjectByType<XPManager>();
        spawner = FindAnyObjectByType<WaveSpawner>();
        return xpManager != null && spawner != null;
    }

    private void SetShown(bool started)
    {
        if (levelText.gameObject.activeSelf == started)
            return;
        content.SetActive(started && showBar);
        levelText.gameObject.SetActive(started);
    }

    private void ShowProgress()
    {
        int level;
        int xpIntoLevel;
        int xpForLevel;
        float fraction = 0f;

        xpManager.GetProgress(out level, out xpIntoLevel, out xpForLevel);
        levelText.text = $"Lv. {level}";
        if (xpForLevel > 0)
            fraction = (float)xpIntoLevel / xpForLevel;
        BrushFill.Set(fillImage, fraction);
    }

    void Awake()
    {
        content.SetActive(false);
        levelText.gameObject.SetActive(false);
    }

    void Update()
    {
        bool started;

        if (!FindRunManagers())
            return;
        started = spawner.CurrentWave > 0;
        SetShown(started);
        if (started)
            ShowProgress();
    }
}
