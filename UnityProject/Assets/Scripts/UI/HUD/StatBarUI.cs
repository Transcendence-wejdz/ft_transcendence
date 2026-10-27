using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class StatBarUI : MonoBehaviour
{
    private enum Stat { Health, Mana }

    [SerializeField] private Stat stat;
    [SerializeField] private TMP_Text valueText;

    private Image fillImage;

    void Awake()
    {
        fillImage = GetComponent<Image>();
    }

    void OnEnable()
    {
        if (stat == Stat.Health)
            Player.OnHealthChanged += SetFill;
        else
            Player.OnManaChanged += SetFill;
    }

    void OnDisable()
    {
        if (stat == Stat.Health)
            Player.OnHealthChanged -= SetFill;
        else
            Player.OnManaChanged -= SetFill;
    }

    private void SetFill(float current, float max)
    {
        BrushFill.Set(fillImage, max > 0f ? current / max : 0f);

        if (valueText != null)
            valueText.text = $"{Mathf.CeilToInt(current)}<space=0.12em>/<space=0.12em>{Mathf.CeilToInt(max)}";
    }
}
