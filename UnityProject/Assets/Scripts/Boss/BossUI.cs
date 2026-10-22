using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Boss scene HUD: the living boss's name and health bar at the top of the screen, hidden while no boss exists.
public class BossUI : MonoBehaviour
{
    [SerializeField] private GameObject content;
    [SerializeField] private Image fillImage;
    [SerializeField] private TMP_Text nameText;

    private BossStats boundStats;

    void OnEnable()
    {
        Boss.OnActiveChanged += Bind;
        Bind(Boss.Active);
    }

    void OnDisable()
    {
        Boss.OnActiveChanged -= Bind;
        Bind(null);
    }

    private void Bind(Boss boss)
    {
        if (boundStats != null)
            boundStats.OnHealthChanged -= SetFill;

        boundStats = boss != null ? boss.Stats : null;
        content.SetActive(boundStats != null);
        if (boundStats == null)
            return;

        boundStats.OnHealthChanged += SetFill;
        nameText.text = boss.Kit.DisplayName;
        SetFill(boundStats.CurrentHealth, boundStats.MaxHealth);
    }

    private void SetFill(float current, float max)
    {
        fillImage.fillAmount = max > 0f ? current / max : 0f;
    }
}
