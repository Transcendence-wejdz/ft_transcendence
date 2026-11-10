using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

// World-space health bar bound to any IHealthStats.
public class WorldHealthBar : MonoBehaviour
{
    [SerializeField] private Image fillImage;
    [FormerlySerializedAs("stats")]
    [SerializeField] private MonoBehaviour statsSource;
    [SerializeField] private bool hideWhenFull = true;

    private IHealthStats stats;
    private Canvas canvas;
    private float fraction = 1f;

    // Full-health bars stay hidden unless hideWhenFull is off, so idle crowds don't clutter the screen.
    private bool ShouldShow()
    {
        if (!GameSettings.ShowEnemyHealthBars)
            return false;
        return !hideWhenFull || fraction < 1f;
    }

    private void ApplyVisibility()
    {
        if (canvas != null)
            canvas.enabled = ShouldShow();
    }

    private void SetFill(float current, float max)
    {
        fraction = 0f;
        if (max > 0f)
            fraction = current / max;
        BrushFill.Set(fillImage, fraction);
        ApplyVisibility();
    }

    // statsSource is a MonoBehaviour since Unity can't serialize interface references directly.
    void Awake()
    {
        stats = statsSource as IHealthStats;
        canvas = GetComponent<Canvas>();
    }

    void OnEnable()
    {
        if (stats != null)
            stats.OnHealthChanged += SetFill;
        GameSettings.OnChanged += ApplyVisibility;
        ApplyVisibility();
    }

    // Initial sync happens here, after every Awake has set initial health.
    void Start()
    {
        if (stats != null)
            SetFill(stats.CurrentHealth, stats.MaxHealth);
    }

    void OnDisable()
    {
        if (stats != null)
            stats.OnHealthChanged -= SetFill;
        GameSettings.OnChanged -= ApplyVisibility;
    }
}
