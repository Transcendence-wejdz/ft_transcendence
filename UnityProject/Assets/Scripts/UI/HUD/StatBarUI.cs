using TMPro;
using UnityEngine;
using UnityEngine.UI;

// HUD health or mana bar of the local player, with its current / max value.
public class StatBarUI : MonoBehaviour
{
    private enum Stat { Health, Mana }

    [SerializeField] private Stat stat;
    [SerializeField] private TMP_Text valueText;

    private Image fillImage;

    private void SetFill(float current, float max)
    {
        float fraction = 0f;

        if (max > 0f)
            fraction = current / max;
        BrushFill.Set(fillImage, fraction);
        if (valueText != null)
            valueText.text = $"{Mathf.CeilToInt(current)}<space=0.12em>/<space=0.12em>{Mathf.CeilToInt(max)}";
    }

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
}
