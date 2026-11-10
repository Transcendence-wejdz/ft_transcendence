using UnityEngine;

// Lobby customization panel: class and color buttons forwarding to LocalCharacterLook.
public class CharacterCustomizationMenu : MonoBehaviour
{
    [SerializeField] private HoverColorWipe[] classButtons;
    [SerializeField] private GameObject[] colorMarkers;

    private void PinClass(int classIndex)
    {
        for (int i = 0; i < classButtons.Length; i++)
        {
            if (classButtons[i] != null)
                classButtons[i].SetPinned(i == classIndex);
        }
    }

    private void MarkColor(int colorIndex)
    {
        for (int i = 0; i < colorMarkers.Length; i++)
        {
            if (colorMarkers[i] != null)
                colorMarkers[i].SetActive(i == colorIndex);
        }
    }

    // Pins the current class button and shows the marker on the current color (both indexed like CharacterCatalog).
    private void ShowSelection(Sprite portrait)
    {
        GameObject player = LocalPlayer.Get();
        CharacterCatalog catalog = CharacterCatalog.Instance;

        if (player == null || catalog == null)
            return;
        PinClass(catalog.ClassIndexOf(player));
        MarkColor(catalog.ColorIndexOf(player));
    }

    public void SelectClass(int index)
    {
        LocalCharacterLook.SelectClass(index);
    }

    public void SelectColor(int index)
    {
        LocalCharacterLook.SelectColor(index);
    }

    // Renders the real local player (not a separate doll) into CharacterPreviewRT while the panel is open.
    private void OnEnable()
    {
        Player customization;

        LocalCharacterLook.OnPortraitChanged += ShowSelection;
        if (LocalPlayer.Get() == null)
            return;
        customization = LocalPlayer.GetPlayer();
        customization?.SetPreviewCameraActive(true);
        ShowSelection(null);
    }

    private void OnDisable()
    {
        LocalCharacterLook.OnPortraitChanged -= ShowSelection;
        LocalPlayer.GetPlayer()?.SetPreviewCameraActive(false);
    }
}
