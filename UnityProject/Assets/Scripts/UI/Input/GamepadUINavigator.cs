using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Gamepad menu navigation: focuses the front menu's first control, highlights it, B goes back; runs after the EventSystem so the push that focuses does not also move. Moves and selection rules live in GamepadUINavigator.Moves.cs and .Selection.cs.
[DefaultExecutionOrder(1000)]
public partial class GamepadUINavigator : MonoBehaviour
{
    private const float HighlightScale = 1.08f;

    private static InputAction navigateAction;

    private InputSystemUIInputModule patchedModule;
    private RectTransform highlighted;
    private Vector3 highlightedBaseScale;
    private GameObject suppressedHover;
    private GameObject lastScrolled;
    private readonly List<RaycastResult> hoverHits = new List<RaycastResult>();

    private static bool GamepadActive => InputModeTracker.UsingGamepad;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        GameObject go = new GameObject("GamepadUINavigator");

        DontDestroyOnLoad(go);
        go.AddComponent<GamepadUINavigator>();
    }

    private static GameObject CurrentSelection(EventSystem eventSystem)
    {
        if (eventSystem == null)
            return null;
        return eventSystem.currentSelectedGameObject;
    }

    // Controls drawing their own focus (IFocusVisual) are left at their size.
    private static RectTransform HighlightTarget(GameObject selected)
    {
        if (selected == null || selected.GetComponent<IFocusVisual>() != null)
            return null;
        return selected.transform as RectTransform;
    }

    private void UpdateHighlight(GameObject selected)
    {
        RectTransform target = HighlightTarget(selected);

        if (target == highlighted)
            return;
        if (highlighted != null)
            highlighted.localScale = highlightedBaseScale;
        highlighted = target;
        if (highlighted == null)
            return;
        highlightedBaseScale = highlighted.localScale;
        highlighted.localScale = highlightedBaseScale * HighlightScale;
    }

    // The pause menu sits above every other menu, so it takes Back first; then the open NPC panel; level-up and game over have no back.
    private static void Back()
    {
        PauseManager pause;

        if (LevelUpUI.IsShowing || GameOverUI.IsShowing)
            return;
        if (KeybindsPanelUI.TryCancelSwap() || KeybindsPanelUI.IsBusy)
            return;
        if (SocialContextMenuUI.CloseOpen())
            return;
        pause = FindAnyObjectByType<PauseManager>();
        if (PauseManager.IsMenuOpen && pause != null)
            pause.Back();
        else if (MenuPanel.CurrentOpen != null)
            MenuPanel.CurrentOpen.Close();
        else if (pause != null)
            pause.Back();
    }

    // B first leaves a text field being edited, then a slider being adjusted, and only then goes back.
    private void HandleBackButton(GameObject selected)
    {
        TMP_InputField field = null;

        if (selected != null)
            field = selected.GetComponent<TMP_InputField>();
        if (field != null && field.isFocused)
            field.DeactivateInputField();
        else if (editingSlider != null)
            StopSliderEdit();
        else
            Back();
    }

    // In a menu: moves, then falls back to the front menu's first control when the selection is gone or hidden.
    private GameObject NavigateMenu(EventSystem eventSystem, GameObject selected)
    {
        GameObject valid = null;

        if (IsValidSelection(selected))
            valid = selected;
        UpdateSliderEditing(valid);
        UpdateStickNavigation(valid);
        selected = eventSystem.currentSelectedGameObject;
        if (!IsValidSelection(selected))
        {
            selected = FirstInFrontMenuObject();
            eventSystem.SetSelectedGameObject(selected);
        }
        RememberInColumn(selected);
        if (Gamepad.current != null && Gamepad.current.buttonEast.wasPressedThisFrame)
            HandleBackButton(selected);
        return selected;
    }

    // Out of menus, nothing stays selected (a typed-in text field excepted), so A never clicks a hidden button.
    private GameObject UpdateGamepadSelection(EventSystem eventSystem, GameObject selected)
    {
        if (PauseManager.IsPaused && !KeybindsPanelUI.IsListening)
            return NavigateMenu(eventSystem, selected);
        if (KeybindsPanelUI.IsListening || selected == null || selected.GetComponent<TMP_InputField>() != null)
            return selected;
        eventSystem.SetSelectedGameObject(null);
        return null;
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

    private void Update()
    {
        EventSystem eventSystem;
        GameObject selected;

        PatchNavigateAction();
        eventSystem = EventSystem.current;
        selected = CurrentSelection(eventSystem);
        if (editingSlider != null && ShouldStopSliderEdit())
            StopSliderEdit();
        if (eventSystem != null && GamepadActive)
            selected = UpdateGamepadSelection(eventSystem, selected);
        if (!PauseManager.IsPaused && lastInColumn.Count > 0)
            lastInColumn.Clear();
        if (GamepadActive)
            UpdateHighlight(selected);
        else
            UpdateHighlight(null);
        if (GamepadActive)
        {
            SuppressMouseHover();
            if (selected != lastScrolled)
                ScrollIntoView(selected);
        }
        lastScrolled = selected;
    }
}
