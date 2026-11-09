using UnityEngine;
using UnityEngine.UI;

// Settings row that only works in the desktop version: its tag stays visible everywhere, and in the browser the row is greyed out and skipped by navigation.
[RequireComponent(typeof(CanvasGroup))]
public class DesktopOnlyRow : MonoBehaviour
{
    [SerializeField] private float webAlpha = 0.45f;

    private void Awake()
    {
        if (GameSettings.IsDesktop)
            return;
        GetComponent<CanvasGroup>().alpha = webAlpha;
        foreach (Selectable selectable in GetComponentsInChildren<Selectable>(true))
            selectable.interactable = false;
    }
}
