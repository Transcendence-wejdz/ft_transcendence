using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Boss scene HUD: the living boss's name and health bar at the top of the screen, hidden while no boss exists.
public class BossUI : MonoBehaviour
{
    [SerializeField] private GameObject content;
    [SerializeField] private Image fillImage;
    [SerializeField] private TMP_Text nameText;

    private Boss bound;

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
        if (bound != null)
            bound.OnHealthChanged -= SetFill;

        bound = boss;
        content.SetActive(bound != null);
        if (bound == null)
            return;

        bound.OnHealthChanged += SetFill;
        nameText.text = bound.Kit.DisplayName;
        SetFill(bound.CurrentHealth, bound.MaxHealth);
    }

    private void SetFill(float current, float max)
    {
        BrushFill.Set(fillImage, max > 0f ? current / max : 0f);
    }
}
