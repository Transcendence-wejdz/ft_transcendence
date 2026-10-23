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

    private static bool GamepadActive => InputModeTracker.UsingGamepad;

    private static InputAction navigateAction;

    private MoveDirection stickDirection = MoveDirection.None;
    private float nextStickMoveTime;

    private Selectable[] buffer = new Selectable[256];
    private InputSystemUIInputModule patchedModule;
    private RectTransform highlighted;
    private Vector3 highlightedBaseScale;

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

        if (eventSystem != null && GamepadActive)
        {
            if (PauseManager.IsPaused && !KeybindsPanelUI.IsBusy)
            {
                UpdateStickNavigation(IsValidSelection(selected) ? selected : null);
                selected = eventSystem.currentSelectedGameObject;

                if (!IsValidSelection(selected))
                {
                    Selectable first = FirstInFrontMenu();
                    selected = first != null ? first.gameObject : null;
                    eventSystem.SetSelectedGameObject(selected);
                }

                if (Gamepad.current != null && Gamepad.current.buttonEast.wasPressedThisFrame)
                {
                    TMP_InputField field = selected != null ? selected.GetComponent<TMP_InputField>() : null;
                    if (field != null && field.isFocused)
                        field.DeactivateInputField();
                    else
                        Back();
                }
            }
            else if (selected != null && selected.GetComponent<TMP_InputField>() == null)
            {
                selected = null;
                eventSystem.SetSelectedGameObject(null);
            }
        }

        UpdateHighlight(GamepadActive ? selected : null);
    }

    // Left stick navigation with hysteresis: a direction holds within 60 degrees, so a diagonal push no longer alternates between two moves.
    private void UpdateStickNavigation(GameObject selected)
    {
        Vector2 stick = Gamepad.current != null ? Gamepad.current.leftStick.ReadValue() : Vector2.zero;
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
        if (selected == null)
            return;

        var data = new AxisEventData(EventSystem.current) { moveDir = direction, moveVector = stick };
        ExecuteEvents.Execute(selected, data, ExecuteEvents.moveHandler);
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

    // The default UI Navigate is PassThrough: stick noise below the deadzone overwrites a held dpad, so it gets a Value action; sticks are handled above.
    private void PatchNavigateAction()
    {
        InputSystemUIInputModule module = EventSystem.current != null ? EventSystem.current.currentInputModule as InputSystemUIInputModule : null;
        if (module == null || module == patchedModule)
            return;

        if (navigateAction == null)
        {
            InputActionAsset asset = ScriptableObject.CreateInstance<InputActionAsset>();
            navigateAction = asset.AddActionMap("UI").AddAction("Navigate", InputActionType.Value, expectedControlLayout: "Vector2");
            navigateAction.AddBinding("<Gamepad>/dpad");
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

        if (MenuPanel.CurrentOpen != null)
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

    // Top-most then left-most control of the open NPC panel, else of the highest screen-space canvas.
    private Selectable FirstInFrontMenu()
    {
        int count = FillBuffer();
        int front = FrontSortingOrder();
        Transform panel = MenuPanel.CurrentOpen != null ? MenuPanel.CurrentOpen.transform : null;

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

    // Closes the open NPC panel, else steps back from Settings to Pause, else resumes; level-up and game over have no back.
    private static void Back()
    {
        if (LevelUpUI.IsShowing || GameOverUI.IsShowing || KeybindsPanelUI.IsBusy)
            return;

        if (MenuPanel.CurrentOpen != null)
        {
            MenuPanel.CurrentOpen.Close();
            return;
        }

        PauseManager pause = FindAnyObjectByType<PauseManager>();
        if (pause != null)
            pause.Back();
    }

    private void UpdateHighlight(GameObject selected)
    {
        RectTransform target = selected != null ? selected.transform as RectTransform : null;
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
