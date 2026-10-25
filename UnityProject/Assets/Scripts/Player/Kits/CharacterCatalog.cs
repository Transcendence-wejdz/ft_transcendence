using UnityEngine;

// Every playable class/color look, loaded from Resources so any scene can resolve kits, portraits and visuals.
[CreateAssetMenu(menuName = "Transcendence/Character Catalog", fileName = "CharacterCatalog")]
public class CharacterCatalog : ScriptableObject
{
    private const string ResourcePath = "CharacterCatalog";

    private static CharacterCatalog instance;

    public static CharacterCatalog Instance
    {
        get
        {
            if (instance == null)
                instance = Resources.Load<CharacterCatalog>(ResourcePath);
            return instance;
        }
    }

    [SerializeField] private ClassKit[] kitsByClass;
    [SerializeField] private ColorVariant[] colorVariants;
    [SerializeField] private Sprite[] backgroundSpritesByColor;

    [System.Serializable]
    private class ColorVariant
    {
        public RuntimeAnimatorController[] controllersByClass;
        public Sprite[] worldSpritesByClass;
        public Sprite[] portraitSpritesByClass;
    }

    public int ClassCount => kitsByClass.Length;
    public int ColorCount => colorVariants.Length;

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

    public bool ApplyVisuals(GameObject player, int classIndex, int colorIndex)
    {
        if (!IsValid(classIndex, colorIndex))
            return false;

        ColorVariant variant = colorVariants[colorIndex];
        RuntimeAnimatorController controller = variant.controllersByClass[classIndex];
        Sprite worldSprite = variant.worldSpritesByClass[classIndex];
        if (controller == null)
            return false;

        player.GetComponent<Animator>().runtimeAnimatorController = controller;
        player.GetComponent<SpriteRenderer>().sprite = worldSprite;
        return true;
    }

    // Reads the class back from the player's current animator controller.
    public int ClassIndexOf(GameObject player)
    {
        RuntimeAnimatorController current = player.GetComponent<Animator>().runtimeAnimatorController;
        AnimatorOverrideController overrideController = current as AnimatorOverrideController;
        RuntimeAnimatorController baseController = overrideController != null ? overrideController.runtimeAnimatorController : current;

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
