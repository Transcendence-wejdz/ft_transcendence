using UnityEngine;

// Every playable class/color look, loaded from Resources so any scene can resolve kits, portraits and visuals.
[CreateAssetMenu(menuName = "Transcendence/Character Catalog", fileName = "CharacterCatalog")]
public class CharacterCatalog : ScriptableObject
{
    [System.Serializable]
    private class ColorVariant
    {
        public RuntimeAnimatorController[] controllersByClass;
        public Sprite[] worldSpritesByClass;
        public Sprite[] portraitSpritesByClass;
    }

    private const string ResourcePath = "CharacterCatalog";

    private static CharacterCatalog instance;

    [SerializeField] private ClassKit[] kitsByClass;
    [SerializeField] private ColorVariant[] colorVariants;
    [SerializeField] private Sprite[] backgroundSpritesByColor;

    // Indexed by ColorIndex: Black, Blue, Purple, Red, Yellow; light tints readable as name tag text.
    [SerializeField] private Color[] accentColorsByColor =
    {
        new Color(0.75f, 0.75f, 0.75f),
        new Color(0.45f, 0.65f, 1f),
        new Color(0.8f, 0.55f, 1f),
        new Color(1f, 0.45f, 0.42f),
        new Color(1f, 0.86f, 0.35f),
    };

    // Same order, saturated so the tint still reads on white ability FX.
    [SerializeField] private Color[] fxColorsByColor =
    {
        new Color(0.3f, 0.3f, 0.35f),
        new Color(0.24f, 0.47f, 1f),
        new Color(0.65f, 0.3f, 1f),
        new Color(1f, 0.23f, 0.19f),
        new Color(1f, 0.8f, 0.1f),
    };

    // Same order, color of the light under ability FX; Black glows white since a dark light shows nothing.
    [SerializeField] private Color[] fxLightColorsByColor =
    {
        Color.white,
        new Color(0.24f, 0.47f, 1f),
        new Color(0.65f, 0.3f, 1f),
        new Color(1f, 0.23f, 0.19f),
        new Color(1f, 0.8f, 0.1f),
    };

    public static CharacterCatalog Instance
    {
        get
        {
            if (instance == null)
                instance = Resources.Load<CharacterCatalog>(ResourcePath);
            return instance;
        }
    }

    private static Player OwnerPlayer(GameObject owner)
    {
        if (owner == null)
            return null;
        return owner.GetComponentInParent<Player>();
    }

    // The class controller under a color override, so every color of one class maps back to it.
    private static RuntimeAnimatorController BaseController(RuntimeAnimatorController current)
    {
        AnimatorOverrideController overrideController = current as AnimatorOverrideController;

        if (overrideController == null)
            return current;
        return overrideController.runtimeAnimatorController;
    }

    public bool IsValid(int classIndex, int colorIndex)
    {
        return classIndex >= 0 && classIndex < kitsByClass.Length
            && colorIndex >= 0 && colorIndex < colorVariants.Length;
    }

    public ClassKit GetKit(int classIndex)
    {
        if (classIndex < 0 || classIndex >= kitsByClass.Length)
            return null;
        return kitsByClass[classIndex];
    }

    public Sprite GetPortrait(int classIndex, int colorIndex)
    {
        if (!IsValid(classIndex, colorIndex))
            return null;
        return colorVariants[colorIndex].portraitSpritesByClass[classIndex];
    }

    public Sprite GetBackground(int colorIndex)
    {
        if (colorIndex < 0 || colorIndex >= backgroundSpritesByColor.Length)
            return null;
        return backgroundSpritesByColor[colorIndex];
    }

    public Color GetAccentColor(int colorIndex)
    {
        if (colorIndex < 0 || colorIndex >= accentColorsByColor.Length)
            return Color.white;
        return accentColorsByColor[colorIndex];
    }

    public Color GetFxColor(int colorIndex)
    {
        if (colorIndex < 0 || colorIndex >= fxColorsByColor.Length)
            return Color.white;
        return fxColorsByColor[colorIndex];
    }

    public Color GetFxLightColor(int colorIndex)
    {
        if (colorIndex < 0 || colorIndex >= fxLightColorsByColor.Length)
            return Color.white;
        return fxLightColorsByColor[colorIndex];
    }

    // FX tint of the player owning this object, white when it has none (dummies, editor previews).
    public static Color FxColorOf(GameObject owner)
    {
        Player player = OwnerPlayer(owner);

        if (player == null || Instance == null)
            return Color.white;
        return Instance.GetFxColor(player.ColorIndex);
    }

    // FX light color of the player owning this object, white when it has none.
    public static Color FxLightColorOf(GameObject owner)
    {
        Player player = OwnerPlayer(owner);

        if (player == null || Instance == null)
            return Color.white;
        return Instance.GetFxLightColor(player.ColorIndex);
    }

    public bool ApplyVisuals(GameObject player, int classIndex, int colorIndex)
    {
        ColorVariant variant;
        RuntimeAnimatorController controller;

        if (!IsValid(classIndex, colorIndex))
            return false;
        variant = colorVariants[colorIndex];
        controller = variant.controllersByClass[classIndex];
        if (controller == null)
            return false;
        player.GetComponent<Animator>().runtimeAnimatorController = controller;
        player.GetComponent<SpriteRenderer>().sprite = variant.worldSpritesByClass[classIndex];
        return true;
    }

    // Reads the class back from the player's current animator controller.
    public int ClassIndexOf(GameObject player)
    {
        RuntimeAnimatorController baseController = BaseController(player.GetComponent<Animator>().runtimeAnimatorController);
        RuntimeAnimatorController[] baseControllersByClass = colorVariants[0].controllersByClass;

        for (int i = 0; i < baseControllersByClass.Length; i++)
        {
            if (baseControllersByClass[i] == baseController)
                return i;
        }
        return 0;
    }

    // Reads the color back from the player's current animator controller.
    public int ColorIndexOf(GameObject player)
    {
        RuntimeAnimatorController current = player.GetComponent<Animator>().runtimeAnimatorController;

        for (int c = 0; c < colorVariants.Length; c++)
        {
            foreach (RuntimeAnimatorController candidate in colorVariants[c].controllersByClass)
            {
                if (candidate == current)
                    return c;
            }
        }
        return 0;
    }
}
