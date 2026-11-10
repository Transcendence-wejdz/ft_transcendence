using UnityEngine;
using UnityEngine.UI;

// Marks an element as not available yet: placed under a Selectable, the prefab keeps it locked.
public class ComingSoonBanner : MonoBehaviour
{
    public static bool IsOn(Selectable selectable)
    {
        return selectable.GetComponentInChildren<ComingSoonBanner>(true) != null;
    }

    void Awake()
    {
        Selectable selectable = GetComponentInParent<Selectable>(true);

        if (selectable != null)
            selectable.interactable = false;
    }
}
