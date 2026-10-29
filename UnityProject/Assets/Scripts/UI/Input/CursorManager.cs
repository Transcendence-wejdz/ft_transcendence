using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// Replaces the OS cursor
public class CursorManager : MonoBehaviour
{
    private static readonly Vector2 DefaultHotspot = new Vector2(21, 16);
    private static readonly Vector2 HoverHotspot = new Vector2(19, 16);
    private const string DefaultCursorResourcePath = "Tiny Swords/UI Elements/Cursors/Cursor_01";
    private const string HoverCursorResourcePath = "Tiny Swords/UI Elements/Cursors/Cursor_02";
    private const int CursorSortingOrder = 10000;

    [SerializeField] private float displayScale = 0.5f;
    [SerializeField] private float clickScale = 0.5f;
    [SerializeField] private float clickAnimationDuration = 0.08f;

    private Sprite defaultSprite;
    private Sprite hoverSprite;
    private RectTransform canvasRect;
    private RectTransform cursorRect;
    private Image cursorImage;
    private Coroutine activeAnimation;
    private bool isHoveringClickable;
    private readonly List<RaycastResult> raycastResults = new List<RaycastResult>();
    private static CursorManager instance;
    private Vector2 virtualPosition;
    private bool virtualActive;

    // Where the game treats the cursor as pointing: the virtual cursor while it runs, the OS one otherwise.
    public static Vector2 PointerPosition
    {
        get
        {
            if (instance != null && instance.virtualActive)
                return instance.virtualPosition;
            return Mouse.current != null ? Mouse.current.position.ReadValue() : Vector2.zero;
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        var go = new GameObject("CursorManager");
        DontDestroyOnLoad(go);
        go.AddComponent<CursorManager>();
    }

    private void Awake()
    {
        defaultSprite = Resources.Load<Sprite>(DefaultCursorResourcePath);
        hoverSprite = Resources.Load<Sprite>(HoverCursorResourcePath);
        if (defaultSprite == null)
        {
            Debug.LogWarning($"CursorManager: could not load cursor sprite at Resources/{DefaultCursorResourcePath}");
            Destroy(gameObject);
            return;
        }

        instance = this;
        Cursor.visible = false;

        var canvasGO = new GameObject("CursorCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
        canvasGO.transform.SetParent(transform, false);
        var canvas = canvasGO.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = CursorSortingOrder;
        var scaler = canvasGO.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(800f, 600f);
        canvasRect = canvasGO.GetComponent<RectTransform>();

        var imageGO = new GameObject("Cursor", typeof(RectTransform), typeof(Image));
        imageGO.transform.SetParent(canvasGO.transform, false);
        cursorImage = imageGO.GetComponent<Image>();
        cursorImage.raycastTarget = false;
        cursorRect = imageGO.GetComponent<RectTransform>();
        cursorRect.anchorMin = cursorRect.anchorMax = new Vector2(0.5f, 0.5f);

        ApplySprite(defaultSprite, DefaultHotspot);
        GameSettings.OnChanged += ApplyCurrentSprite;
    }

    // Re-applies the shown sprite so a Cursor Size change takes effect at once.
    private void ApplyCurrentSprite()
    {
        if (isHoveringClickable && hoverSprite != null)
            ApplySprite(hoverSprite, HoverHotspot);
        else
            ApplySprite(defaultSprite, DefaultHotspot);
    }

    private void ApplySprite(Sprite sprite, Vector2 hotspot)
    {
        cursorImage.sprite = sprite;
        cursorImage.SetNativeSize();
        cursorRect.sizeDelta *= displayScale * GameSettings.CursorSize;

        Rect spriteRect = sprite.rect;
        float spriteTopFromTextureTop = sprite.texture.height - (spriteRect.y + spriteRect.height);
        cursorRect.pivot = new Vector2(
            (hotspot.x - spriteRect.x) / spriteRect.width,
            1f - (hotspot.y - spriteTopFromTextureTop) / spriteRect.height);
    }

    private void OnDestroy()
    {
        GameSettings.OnChanged -= ApplyCurrentSprite;
        if (virtualActive)
            Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void Update()
    {
        if (cursorRect == null || Mouse.current == null)
            return;

        Vector2 osPosition = Mouse.current.position.ReadValue();
        bool osInsideWindow = Application.isFocused
            && osPosition.x >= 0f && osPosition.x <= Screen.width
            && osPosition.y >= 0f && osPosition.y <= Screen.height;
        UpdateVirtualCursor(osInsideWindow);

        Vector2 mouseScreenPosition = PointerPosition;
        bool insideWindow = virtualActive || osInsideWindow;
        Cursor.visible = !insideWindow;
        cursorImage.enabled = insideWindow && !InputModeTracker.UsingGamepad;
        if (!cursorImage.enabled)
            return;

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, mouseScreenPosition, null, out Vector2 localPoint))
            cursorRect.localPosition = localPoint;

        if (hoverSprite != null)
        {
            bool nowHovering = IsPointerOverClickable(mouseScreenPosition);
            if (nowHovering != isHoveringClickable)
            {
                isHoveringClickable = nowHovering;
                ApplySprite(nowHovering ? hoverSprite : defaultSprite, nowHovering ? HoverHotspot : DefaultHotspot);
            }
        }

        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            if (activeAnimation != null)
                StopCoroutine(activeAnimation);
            activeAnimation = StartCoroutine(AnimateClick());
        }
    }

    // Outside menus with a non default mouse sensitivity, locks the OS cursor and moves a virtual one by mouse delta x sensitivity; menus get the OS cursor back where the virtual one was.
    private void UpdateVirtualCursor(bool osInsideWindow)
    {
        bool wanted = !Mathf.Approximately(GameSettings.MouseSensitivity, 1f)
            && !PauseManager.IsPaused && !InputModeTracker.UsingGamepad && Application.isFocused
            && (virtualActive || osInsideWindow);

        if (wanted && !virtualActive)
        {
            virtualPosition = Mouse.current.position.ReadValue();
            Cursor.lockState = CursorLockMode.Locked;
            virtualActive = true;
            return;
        }
        if (!wanted && virtualActive)
        {
            virtualActive = false;
            Cursor.lockState = CursorLockMode.None;
            Mouse.current.WarpCursorPosition(virtualPosition);
            return;
        }
        if (!virtualActive)
            return;

        virtualPosition += Mouse.current.delta.ReadValue() * GameSettings.MouseSensitivity;
        virtualPosition.x = Mathf.Clamp(virtualPosition.x, 0f, Screen.width);
        virtualPosition.y = Mathf.Clamp(virtualPosition.y, 0f, Screen.height);
    }

    private bool IsPointerOverClickable(Vector2 screenPosition)
    {
        if (EventSystem.current == null)
            return false;

        var pointerEventData = new PointerEventData(EventSystem.current) { position = screenPosition };

        raycastResults.Clear();
        EventSystem.current.RaycastAll(pointerEventData, raycastResults);
        foreach (RaycastResult result in raycastResults)
        {
            Selectable selectable = result.gameObject.GetComponentInParent<Selectable>();
            if (selectable != null && selectable.interactable)
                return true;
        }
        return false;
    }

    private IEnumerator AnimateClick()
    {
        float t = 0f;
        while (t < clickAnimationDuration)
        {
            t += Time.unscaledDeltaTime;
            float pulse = Mathf.Sin(Mathf.Clamp01(t / clickAnimationDuration) * Mathf.PI);
            cursorRect.localScale = Vector3.one * Mathf.Lerp(1f, clickScale, pulse);
            yield return null;
        }

        cursorRect.localScale = Vector3.one;
        activeAnimation = null;
    }
}
