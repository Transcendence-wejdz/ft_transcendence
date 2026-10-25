using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Bottom-center shared XP bar + level indicator under the top-left avatar, both shown once waves start (same gate as CoopHUD).
public class XPBarUI : MonoBehaviour
{
    [SerializeField] private GameObject content;
    [SerializeField] private Image fillImage;
    [SerializeField] private TMP_Text levelText;
    [SerializeField] private bool showBar = true;

    private XPManager xpManager;
    private WaveSpawner spawner;
    private float nextSearchTime;

    void Awake()
    {
        content.SetActive(false);
        levelText.gameObject.SetActive(false);
    }

    // Run managers are absent outside a run (Lobby), so the lookup is throttled instead of searching every frame.
    void Update()
    {
        if (xpManager == null || spawner == null)
        {
            if (Time.unscaledTime < nextSearchTime)
                return;
            nextSearchTime = Time.unscaledTime + 0.5f;
            xpManager = FindAnyObjectByType<XPManager>();
            spawner = FindAnyObjectByType<WaveSpawner>();
            if (xpManager == null || spawner == null)
                return;
        }

        bool started = spawner.CurrentWave > 0;
        if (levelText.gameObject.activeSelf != started)
        {
            content.SetActive(started && showBar);
            levelText.gameObject.SetActive(started);
        }

        if (!started)
            return;

        xpManager.GetProgress(out int level, out int xpIntoLevel, out int xpForLevel);
        levelText.text = $"Lv. {level}";
        BrushFill.Set(fillImage, xpForLevel > 0 ? (float)xpIntoLevel / xpForLevel : 0f);
    }
}
