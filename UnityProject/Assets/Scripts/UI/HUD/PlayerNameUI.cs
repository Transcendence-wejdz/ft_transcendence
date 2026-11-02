using TMPro;
using UnityEngine;

// Keeps the HUD name label in sync with the display name.
public class PlayerNameUI : MonoBehaviour
{
    private TextMeshProUGUI label;

    private void SetName(string value)
    {
        if (label != null && !string.IsNullOrEmpty(value))
            label.text = value;
    }

    void Awake()
    {
        label = GetComponent<TextMeshProUGUI>();
    }

    void OnEnable()
    {
        LocalCharacterLook.OnNameChanged += SetName;
        SetName(LocalCharacterLook.CurrentName());
    }

    void OnDisable()
    {
        LocalCharacterLook.OnNameChanged -= SetName;
    }
}
