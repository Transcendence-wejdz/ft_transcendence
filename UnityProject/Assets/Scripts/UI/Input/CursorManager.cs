using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// Replaces the OS cursor: a crosshair (style and color from the settings) in game, the arrow in menus and over buttons; crosshair drawing lives in CursorManager.Crosshair.cs.
public partial class CursorManager : MonoBehaviour
{
    private const string DefaultCursorResourcePath = "Tiny Swords/UI Elements/Cursors/Cursor_01";
    private const string HoverCursorResourcePath = "Tiny Swords/UI Elements/Cursors/Cursor_02";
    private const int CursorSortingOrder = 10000;

    private static readonly Vector2 DefaultHotspot = new Vector2(21, 16);
    private static readonly Vector2 HoverHotspot = new Vector2(19, 16);
    private static readonly Vector2 Center = new Vector2(0.5f, 0.5f);

    private static CursorManager instance;

    [SerializeField] private float displayScale = 0.5f;
    [SerializeField] private float clickScale = 0.5f;
    [Tooltip("Scale the crosshair spreads to on a click, like a gun sight kicking open.")]
    [SerializeField] private float crosshairClickScale = 1.2f;
    [SerializeField] private float clickAnimationDuration = 0.08f;

    private Sprite defaultSprite;
    private Sprite hoverSprite;
    private RectTransform canvasRect;
    private RectTransform pointerRect;
    private RectTransform arrowRect;
    private Image arrowImage;
    private RectTransform crosshairRect;
    private Image crosshairFill;
    private Image crosshairOutline;
    private Coroutine activeAnimation;
    private bool isHoveringClickable;
    private bool showingArrow = true;
    private readonly List<RaycastResult> raycastResults = new List<RaycastResult>();
    private Vector2 virtualPosition;
    private bool virtualActive;

    // Where the game treats the cursor as pointing: the virtual cursor while it runs, the OS one otherwise.
    public static Vector2 PointerPosition
    {
        get
        {
            if (instance != null && instance.virtualActive)
                return instance.virtualPosition;
            if (Mouse.current == null)
                return Vector2.zero;
            return Mouse.current.position.ReadValue();
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        GameObject go = new GameObject("CursorManager");

        DontDestroyOnLoad(go);
        go.AddComponent<CursorManager>();
    }

    // The arrow's tip sits on the pointer: its pivot is moved onto the hotspot, measured in the full texture since the sprites are not cropped.
    private void ApplyArrowSprite(Sprite sprite, Vector2 hotspot)
    {
        Rect spriteRect = sprite.rect;
        float spriteTopFromTextureTop = sprite.texture.height - (spriteRect.y + spriteRect.height);

        arrowImage.sprite = sprite;
        arrowImage.SetNativeSize();
        arrowRect.sizeDelta *= displayScale * GameSettings.CursorSize;
        arrowRect.pivot = new Vector2(
            (hotspot.x - spriteRect.x) / spriteRect.width,
            1f - (hotspot.y - spriteTopFromTextureTop) / spriteRect.height);
    }

    private void ApplyCurrentArrow()
    {
        if (isHoveringClickable)
            ApplyArrowSprite(hoverSprite, HoverHotspot);
        else
            ApplyArrowSprite(defaultSprite, DefaultHotspot);
    }

    private void ApplyCrosshair()
    {
        int style = GameSettings.CursorStyle;
        float size = CrosshairTextureSize * displayScale * GameSettings.CursorSize;

        crosshairFill.sprite = CrosshairSprite(style, false);
        crosshairOutline.sprite = CrosshairSprite(style, true);
        crosshairFill.rectTransform.sizeDelta = new Vector2(size, size);
        crosshairOutline.rectTransform.sizeDelta = new Vector2(size, size);
        crosshairOutline.color = CrosshairOutlineColor;
        crosshairFill.color = CrosshairColor();
    }

    // Re-applies size, crosshair shape and arrow sprite so a settings change takes effect at once.
    private void ApplySettings()
    {
        ApplyCurrentArrow();
        ApplyCrosshair();
    }

    private void MovePointer(Vector2 screenPosition)
    {
        Vector2 localPoint;

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPosition, null, out localPoint))
            pointerRect.localPosition = localPoint;
    }

    private void Awake()
    {
        Transform canvas;

        if (!LoadSprites())
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
        Cursor.visible = false;
        canvas = BuildCanvas();
        BuildPointer(canvas);
        ApplySettings();
        GameSettings.OnChanged += ApplySettings;
    }

    private void OnDestroy()
    {
        GameSettings.OnChanged -= ApplySettings;
        if (virtualActive)
            Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    // Hidden over the gamepad, and outside the window where the OS cursor takes over.
    private void Update()
    {
        bool osInsideWindow;
        bool insideWindow;
        bool visible;

        if (pointerRect == null || Mouse.current == null)
            return;
        osInsideWindow = IsInsideWindow(Mouse.current.position.ReadValue());
        UpdateVirtualCursor(osInsideWindow);
        insideWindow = virtualActive || osInsideWindow;
        Cursor.visible = !insideWindow;
        visible = insideWindow && !InputModeTracker.UsingGamepad;
        pointerRect.gameObject.SetActive(visible);
        if (!visible)
            return;
        MovePointer(PointerPosition);
        UpdateMode(PointerPosition);
        if (Mouse.current.leftButton.wasPressedThisFrame)
            PlayClick();
    }
}
