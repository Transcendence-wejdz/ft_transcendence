using UnityEngine;
using UnityEngine.UI;

// Keeps the HUD portrait in sync with class and color.
public class AvatarUI : MonoBehaviour
{
    private Image avatarImage;

    void Awake()
    {
        avatarImage = GetComponent<Image>();
    }

    void OnEnable()
    {
        LocalCharacterLook.OnPortraitChanged += SetAvatar;
        SetAvatar(LocalCharacterLook.CurrentPortrait());
    }

    void OnDisable()
    {
        LocalCharacterLook.OnPortraitChanged -= SetAvatar;
    }

    private void SetAvatar(Sprite portrait)
    {
        if (portrait != null)
            avatarImage.sprite = portrait;
    }
}
