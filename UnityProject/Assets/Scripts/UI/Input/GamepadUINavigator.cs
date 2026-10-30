using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Gamepad menu navigation: focuses the front menu's first control, highlights it, B goes back; runs after the EventSystem so the push that focuses does not also move.
[DefaultExecutionOrder(1000)]
public class GamepadUINavigator : MonoBehaviour
{
    private const float HighlightScale = 1.08f;
    private const float StickPressThreshold = 0.6f;
    private const float StickReleaseThreshold = 0.4f;
    private const float StickKeepDirectionDeg = 60f;
    private const float StickRepeatDelay = 0.5f;
    private const float StickRepeatRate = 0.1f;
    private const float EditingHandleScale = 1.35f;

    private static bool GamepadActive => InputModeTracker.UsingGamepad;

    private static InputAction navigateAction;

    private MoveDirection stickDirection = MoveDirection.None;
    private float nextStickMoveTime;

    private Selectable[] buffer = new Selectable[256];
    private InputSystemUIInputModule patchedModule;
    private RectTransform highlighted;
    private Vector3 highlightedBaseScale;
    private GameObject suppressedHover;
    private GameObject lastScrolled;
    private readonly Dictionary<Transform, Selectable> lastInColumn = new Dictionary<Transform, Selectable>();
    private Slider editingSlider;
    private Vector3 editingHandleBaseScale;
    private readonly List<RaycastResult> hoverHits = new List<RaycastResult>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        var go = new GameObject("GamepadUINavigator");
        DontDestroyOnLoad(go);
        go.AddComponent<GamepadUINavigator>();
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        InputModeTracker.OnModeChanged += OnInputModeChanged;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        InputModeTracker.OnModeChanged -= OnInputModeChanged;
    }

    private void OnInputModeChanged(bool usingGamepad)
    {
        SetInputFieldsActivateOnSelect(!usingGamepad);
        if (!usingGamepad)
            RestoreMouseHover();
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        SetInputFieldsActivateOnSelect(!GamepadActive);
    }

    // Selects a control for gamepad users only, so mouse users never see a stuck highlight.
    public static void Focus(Selectable selectable)
    {
        if (GamepadActive && selectable != null && EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(selectable.gameObject);
    }

    private void Update()
    {
        PatchNavigateAction();

        EventSystem eventSystem = EventSystem.current;
        GameObject selected = eventSystem != null ? eventSystem.currentSelectedGameObject : null;

        if (editingSlider != null && (!GamepadActive || !PauseManager.IsPaused || !editingSlider.isActiveAndEnabled))
            StopSliderEdit();

        if (eventSystem != null && GamepadActive)
        {
            if (PauseManager.IsPaused && !KeybindsPanelUI.IsListening)
            {
                UpdateSliderEditing(IsValidSelection(selected) ? selected : null);
                UpdateStickNavigation(IsValidSelection(selected) ? selected : null);
                selected = eventSystem.currentSelectedGameObject;

                if (!IsValidSelection(selected))
                {
                    Selectable first = FirstInFrontMenu();
                    selected = first != null ? first.gameObject : null;
                    eventSystem.SetSelectedGameObject(selected);
                }
                RememberInColumn(selected);

                if (Gamepad.current != null && Gamepad.current.buttonEast.wasPressedThisFrame)
                {
                    TMP_InputField field = selected != null ? selected.GetComponent<TMP_InputField>() : null;
                    if (field != null && field.isFocused)
                        field.DeactivateInputField();
                    else if (editingSlider != null)
                        StopSliderEdit();
                    else
                        Back();
                }
            }
            else if (!KeybindsPanelUI.IsListening && selected != null && selected.GetComponent<TMP_InputField>() == null)
            {
                selected = null;
                eventSystem.SetSelectedGameObject(null);
            }
        }

        if (!PauseManager.IsPaused && lastInColumn.Count > 0)
            lastInColumn.Clear();

        UpdateHighlight(GamepadActive ? selected : null);
        if (GamepadActive)
        {
            SuppressMouseHover();
            if (selected != lastScrolled)
                ScrollIntoView(selected);
        }
        lastScrolled = selected;
    }

    // Scrolls the list holding a newly focused control so it stays inside the viewport.
    private static void ScrollIntoView(GameObject selected)
    {
        if (selected == null)
            return;
        ScrollRect scroll = selected.GetComponentInParent<ScrollRect>();
        if (scroll == null || scroll.content == null || !selected.transform.IsChildOf(scroll.content))
            return;

        RectTransform viewport = scroll.viewport != null ? scroll.viewport : (RectTransform)scroll.transform;
        Bounds bounds = RectTransformUtility.CalculateRelativeRectTransformBounds(viewport, selected.transform);
        Rect view = viewport.rect;
        float shift = bounds.max.y > view.yMax ? bounds.max.y - view.yMax : bounds.min.y < view.yMin ? bounds.min.y - view.yMin : 0f;
        if (shift == 0f)
            return;

        scroll.velocity = Vector2.zero;
        scroll.content.anchoredPosition -= new Vector2(0f, shift);
    }

    // A still mouse cursor keeps whatever it hovers lit while the pad selects elsewhere, so its hover is cancelled for as long as the pad is in use.
    private void SuppressMouseHover()
    {
        GameObject hovered = UIUnderMouse();
        if (hovered == suppressedHover)
            return;

        suppressedHover = hovered;
        SendToHierarchy(hovered, ExecuteEvents.pointerExitHandler);
    }

    // Back on the mouse, gives the hover back if the cursor still sits on it, since the input module never re-sends an enter.
    private void RestoreMouseHover()
    {
        GameObject hovered = suppressedHover;
        suppressedHover = null;
        if (hovered != null && hovered == UIUnderMouse())
            SendToHierarchy(hovered, ExecuteEvents.pointerEnterHandler);
    }

    private GameObject UIUnderMouse()
    {
        EventSystem eventSystem = EventSystem.current;
        if (eventSystem == null || Mouse.current == null)
            return null;

        var pointer = new PointerEventData(eventSystem) { position = Mouse.current.position.ReadValue() };
        hoverHits.Clear();
        eventSystem.RaycastAll(pointer, hoverHits);
        return hoverHits.Count > 0 ? hoverHits[0].gameObject : null;
    }

    // Pointer enter/exit reach every object from the hit up to the root, as the input module sends them.
    private static void SendToHierarchy<T>(GameObject target, ExecuteEvents.EventFunction<T> handler) where T : IEventSystemHandler
    {
        if (target == null || EventSystem.current == null)
            return;

        var pointer = new PointerEventData(EventSystem.current) { position = Mouse.current != null ? Mouse.current.position.ReadValue() : Vector2.zero };
        for (Transform t = target.transform; t != null; t = t.parent)
            ExecuteEvents.Execute(t.gameObject, pointer, handler);
    }

    // Left stick and dpad navigation with hysteresis: a direction holds within 60 degrees, so a diagonal push no longer alternates between two moves.
    private void UpdateStickNavigation(GameObject selected)
    {
        Vector2 stick = ReadGamepadNavigation();
        MoveDirection direction = StickDirection(stick);
        if (direction == MoveDirection.None)
        {
            stickDirection = MoveDirection.None;
            return;
        }

        bool repeat = direction == stickDirection;
        if (repeat && Time.unscaledTime < nextStickMoveTime)
            return;

        nextStickMoveTime = Time.unscaledTime + (repeat ? StickRepeatRate : StickRepeatDelay);
        stickDirection = direction;
        if (selected == null || TryWrap(selected, direction) || TryMoveSlider(selected, direction) || TryCrossColumn(selected, direction))
            return;

        var data = new AxisEventData(EventSystem.current) { moveDir = direction, moveVector = stick };
        ExecuteEvents.Execute(selected, data, ExecuteEvents.moveHandler);
    }

    // A on a slider starts or ends adjusting it; moving off it ends it too.
    private void UpdateSliderEditing(GameObject selected)
    {
        Slider slider = selected != null ? selected.GetComponent<Slider>() : null;
        if (editingSlider != null && editingSlider != slider)
            StopSliderEdit();
        if (slider == null || Gamepad.current == null || !Gamepad.current.buttonSouth.wasPressedThisFrame)
            return;

        if (editingSlider == slider)
        {
            StopSliderEdit();
            return;
        }
        editingSlider = slider;
        if (slider.handleRect != null)
        {
            editingHandleBaseScale = slider.handleRect.localScale;
            slider.handleRect.localScale = editingHandleBaseScale * EditingHandleScale;
        }
    }

    private void StopSliderEdit()
    {
        if (editingSlider != null && editingSlider.handleRect != null)
            editingSlider.handleRect.localScale = editingHandleBaseScale;
        editingSlider = null;
    }

    // Left/right on a horizontal slider adjust it in edit mode, otherwise leave it like any other control.
    private bool TryMoveSlider(GameObject selected, MoveDirection direction)
    {
        if (direction != MoveDirection.Left && direction != MoveDirection.Right)
            return false;
        Slider slider = selected.GetComponent<Slider>();
        if (slider == null || (slider.direction != Slider.Direction.LeftToRight && slider.direction != Slider.Direction.RightToLeft))
            return false;

        if (slider == editingSlider)
        {
            float sign = (direction == MoveDirection.Right) == (slider.direction == Slider.Direction.LeftToRight) ? 1f : -1f;
            slider.value += sign * (slider.wholeNumbers ? 1f : (slider.maxValue - slider.minValue) * 0.1f);
            return true;
        }

        Selectable target = direction == MoveDirection.Left ? slider.FindSelectableOnLeft() : slider.FindSelectableOnRight();
        Selectable remembered = RememberedIn(target, slider);
        if (remembered != null)
            target = remembered;
        if (target != null && IsValidSelection(target.gameObject))
            EventSystem.current.SetSelectedGameObject(target.gameObject);
        return true;
    }

    private void RememberInColumn(GameObject selected)
    {
        Selectable selectable = selected != null ? selected.GetComponent<Selectable>() : null;
        Transform column = selectable != null ? MenuColumn(selectable.transform) : null;
        if (column != null)
            lastInColumn[column] = selectable;
    }

    // Left/right into another column lands back on the control last focused there, instead of the nearest one.
    private bool TryCrossColumn(GameObject selected, MoveDirection direction)
    {
        if (direction != MoveDirection.Left && direction != MoveDirection.Right)
            return false;
        Selectable current = selected.GetComponent<Selectable>();
        if (current == null)
            return false;

        Selectable target = direction == MoveDirection.Left ? current.FindSelectableOnLeft() : current.FindSelectableOnRight();
        Selectable remembered = RememberedIn(target, current);
        if (remembered == null)
            return false;
        EventSystem.current.SetSelectedGameObject(remembered.gameObject);
        return true;
    }

    private Selectable RememberedIn(Selectable target, Selectable from)
    {
        if (target == null)
            return null;
        Transform column = MenuColumn(target.transform);
        if (column == null || column == MenuColumn(from.transform))
            return null;
        return lastInColumn.TryGetValue(column, out Selectable remembered) && remembered != null && remembered != target && IsValidSelection(remembered.gameObject) ? remembered : null;
    }

    private static Vector2 ReadGamepadNavigation()
    {
        Gamepad pad = Gamepad.current;
        if (pad == null)
            return Vector2.zero;
        Vector2 dpad = pad.dpad.ReadValue();
        return dpad.sqrMagnitude > 0f ? dpad : pad.leftStick.ReadValue();
    }

    // Up or down with nothing further that way inside the same column lands on the column's other end, nearest in x.
    private bool TryWrap(GameObject selected, MoveDirection direction)
    {
        if (direction != MoveDirection.Up && direction != MoveDirection.Down)
            return false;
        Selectable current = selected.GetComponent<Selectable>();
        Transform column = current != null ? MenuColumn(current.transform) : null;
        if (column == null)
            return false;

        bool down = direction == MoveDirection.Down;
        Selectable next = down ? current.FindSelectableOnDown() : current.FindSelectableOnUp();
        if (next != null && IsValidSelection(next.gameObject) && next.transform.IsChildOf(column))
            return false;

        Selectable end = ColumnEnd(current, column, top: down);
        if (end != null)
            EventSystem.current.SetSelectedGameObject(end.gameObject);
        return true;
    }

    // The child of the control's full-screen panel holding it (pause buttons, friends list, a sidebar, a page), so wrapping never leaves that column.
    private static Transform MenuColumn(Transform control)
    {
        Canvas canvas = control.GetComponentInParent<Canvas>();
        if (canvas == null)
            return null;

        Transform root = canvas.rootCanvas.transform;
        Transform t = control;
        while (t.parent != null && t.parent != root && t.parent.parent != root)
            t = t.parent;
        return t.parent != null ? t : null;
    }

    // Top-most (or bottom-most) row of the column, skipping the current row, then the control closest in x.
    private Selectable ColumnEnd(Selectable from, Transform column, bool top)
    {
        int count = FillBuffer();
        float tolerance = 8f * from.transform.lossyScale.y;
        Vector3 origin = from.transform.position;
        Selectable best = null;
        for (int i = 0; i < count; i++)
        {
            Selectable s = buffer[i];
            Vector3 p = s.transform.position;
            if (s == from || Mathf.Abs(p.y - origin.y) <= tolerance || !s.transform.IsChildOf(column)
                || s.navigation.mode == Navigation.Mode.None || !IsValidSelection(s.gameObject))
                continue;
            if (best == null)
            {
                best = s;
                continue;
            }

            Vector3 b = best.transform.position;
            float further = top ? p.y - b.y : b.y - p.y;
            if (further > tolerance || (further >= -tolerance && Mathf.Abs(p.x - origin.x) < Mathf.Abs(b.x - origin.x)))
                best = s;
        }
        return best;
    }

    private MoveDirection StickDirection(Vector2 stick)
    {
        float threshold = stickDirection == MoveDirection.None ? StickPressThreshold : StickReleaseThreshold;
        if (stick.magnitude < threshold)
            return MoveDirection.None;

        if (stickDirection != MoveDirection.None && Vector2.Angle(stick, DirectionVector(stickDirection)) < StickKeepDirectionDeg)
            return stickDirection;

        if (Mathf.Abs(stick.x) > Mathf.Abs(stick.y))
            return stick.x > 0f ? MoveDirection.Right : MoveDirection.Left;
        return stick.y > 0f ? MoveDirection.Up : MoveDirection.Down;
    }

    private static Vector2 DirectionVector(MoveDirection direction)
    {
        switch (direction)
        {
            case MoveDirection.Left: return Vector2.left;
            case MoveDirection.Right: return Vector2.right;
            case MoveDirection.Up: return Vector2.up;
            default: return Vector2.down;
        }
    }

    // The default UI Navigate is PassThrough and would also move on the gamepad, so it gets a keyboard-only Value action; the gamepad stick and dpad are handled above.
    private void PatchNavigateAction()
    {
        InputSystemUIInputModule module = EventSystem.current != null ? EventSystem.current.currentInputModule as InputSystemUIInputModule : null;
        if (module == null || module == patchedModule)
            return;

        if (navigateAction == null)
        {
            InputActionAsset asset = ScriptableObject.CreateInstance<InputActionAsset>();
            navigateAction = asset.AddActionMap("UI").AddAction("Navigate", InputActionType.Value, expectedControlLayout: "Vector2");
            navigateAction.AddBinding("<Joystick>/stick");
            navigateAction.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/w").With("Up", "<Keyboard>/upArrow")
                .With("Down", "<Keyboard>/s").With("Down", "<Keyboard>/downArrow")
                .With("Left", "<Keyboard>/a").With("Left", "<Keyboard>/leftArrow")
                .With("Right", "<Keyboard>/d").With("Right", "<Keyboard>/rightArrow");
            asset.Enable();
        }

        patchedModule = module;
        module.move = InputActionReference.Create(navigateAction);
    }

    // With a gamepad, landing on a text field only selects it; Submit (A) starts editing.
    private static void SetInputFieldsActivateOnSelect(bool activate)
    {
        foreach (TMP_InputField field in FindObjectsByType<TMP_InputField>(FindObjectsInactive.Include))
            field.shouldActivateOnSelect = activate;
    }

    // Invalid when gone, disabled, or behind the front menu (e.g. a HUD button left selected under the pause menu).
    private bool IsValidSelection(GameObject selected)
    {
        if (selected == null || !selected.activeInHierarchy)
            return false;

        Selectable selectable = selected.GetComponent<Selectable>();
        if (selectable == null || !selectable.IsInteractable())
            return false;

        if (SocialContextMenuUI.OpenMenu != null)
            return selected.transform.IsChildOf(SocialContextMenuUI.OpenMenu);
        if (MenuPanel.CurrentOpen != null && !PauseManager.IsMenuOpen)
            return selected.transform.IsChildOf(MenuPanel.CurrentOpen.transform);

        Canvas canvas = selectable.GetComponentInParent<Canvas>();
        return canvas != null && canvas.rootCanvas.sortingOrder >= FrontSortingOrder();
    }

    private int FrontSortingOrder()
    {
        int count = FillBuffer();
        int front = int.MinValue;
        for (int i = 0; i < count; i++)
        {
            Canvas canvas = buffer[i].GetComponentInParent<Canvas>();
            if (canvas != null && IsCandidate(buffer[i], canvas))
                front = Mathf.Max(front, canvas.rootCanvas.sortingOrder);
        }
        return front;
    }

    // The menu's DefaultSelection if it has one, else its top-most then left-most control; the menu is the open NPC panel, else the highest screen-space canvas.
    private Selectable FirstInFrontMenu()
    {
        int count = FillBuffer();
        int front = FrontSortingOrder();
        Transform panel = SocialContextMenuUI.OpenMenu != null ? SocialContextMenuUI.OpenMenu
            : MenuPanel.CurrentOpen != null && !PauseManager.IsMenuOpen ? MenuPanel.CurrentOpen.transform : null;

        foreach (DefaultSelection marked in DefaultSelection.All)
        {
            Selectable s = marked.Selectable;
            Canvas canvas = s != null ? s.GetComponentInParent<Canvas>() : null;
            if (canvas == null || !IsCandidate(s, canvas))
                continue;
            if (panel != null ? s.transform.IsChildOf(panel) : canvas.rootCanvas.sortingOrder == front)
                return s;
        }

        Selectable best = null;
        for (int i = 0; i < count; i++)
        {
            Selectable s = buffer[i];
            Canvas canvas = s.GetComponentInParent<Canvas>();
            if (canvas == null || !IsCandidate(s, canvas))
                continue;
            if (panel != null ? !s.transform.IsChildOf(panel) : canvas.rootCanvas.sortingOrder != front)
                continue;

            if (best == null || IsBefore(s.transform.position, best.transform.position))
                best = s;
        }
        return best;
    }

    private static bool IsCandidate(Selectable s, Canvas canvas)
    {
        return s.IsInteractable() && s.navigation.mode != Navigation.Mode.None
            && canvas.rootCanvas.renderMode != RenderMode.WorldSpace;
    }

    private static bool IsBefore(Vector3 a, Vector3 b)
    {
        if (Mathf.Abs(a.y - b.y) > 1f)
            return a.y > b.y;
        return a.x < b.x;
    }

    private int FillBuffer()
    {
        if (Selectable.allSelectableCount > buffer.Length)
            buffer = new Selectable[Selectable.allSelectableCount * 2];
        return Selectable.AllSelectablesNoAlloc(buffer);
    }

    // The pause menu sits above every other menu, so it takes Back first; then the open NPC panel; level-up and game over have no back.
    private static void Back()
    {
        if (LevelUpUI.IsShowing || GameOverUI.IsShowing)
            return;
        if (KeybindsPanelUI.TryCancelSwap() || KeybindsPanelUI.IsBusy)
            return;
        if (SocialContextMenuUI.CloseOpen())
            return;

        PauseManager pause = FindAnyObjectByType<PauseManager>();
        if (PauseManager.IsMenuOpen && pause != null)
        {
            pause.Back();
            return;
        }

        if (MenuPanel.CurrentOpen != null)
        {
            MenuPanel.CurrentOpen.Close();
            return;
        }

        if (pause != null)
            pause.Back();
    }

    private void UpdateHighlight(GameObject selected)
    {
        RectTransform target = selected != null && selected.GetComponent<IFocusVisual>() == null ? selected.transform as RectTransform : null;
        if (target == highlighted)
            return;

        if (highlighted != null)
            highlighted.localScale = highlightedBaseScale;

        highlighted = target;
        if (highlighted != null)
        {
            highlightedBaseScale = highlighted.localScale;
            highlighted.localScale = highlightedBaseScale * HighlightScale;
        }
    }
}
