using UnityEngine;
using UnityEngine.UI;

// Ability slot overlay that unwinds over the cooldown of the local player's last use.
public class CooldownIcon : MonoBehaviour
{
    [SerializeField] private AbilityType ability;

    private Image overlay;
    private float duration;
    private float remaining;

    private void HandleAbilityUsed(AbilityType usedAbility, float cooldownDuration)
    {
        if (usedAbility != ability)
            return;
        duration = cooldownDuration;
        remaining = cooldownDuration;
        overlay.fillAmount = 1f;
    }

    void Awake()
    {
        overlay = GetComponent<Image>();
        overlay.fillAmount = 0f;
    }

    void OnEnable()
    {
        Player.OnAbilityUsed += HandleAbilityUsed;
    }

    void OnDisable()
    {
        Player.OnAbilityUsed -= HandleAbilityUsed;
    }

    void Update()
    {
        if (remaining <= 0f)
            return;
        remaining -= Time.deltaTime;
        if (duration > 0f)
            overlay.fillAmount = Mathf.Clamp01(remaining / duration);
        else
            overlay.fillAmount = 0f;
    }

}
