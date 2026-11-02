using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// UI input module patches: keyboard-only Navigate action, text fields that only select on gamepad focus.
public partial class GamepadUINavigator
{
    // With a gamepad, landing on a text field only selects it; Submit (A) starts editing.
    private static void SetInputFieldsActivateOnSelect(bool activate)
    {
        foreach (TMP_InputField field in FindObjectsByType<TMP_InputField>(FindObjectsInactive.Include))
            field.shouldActivateOnSelect = activate;
    }

    private static InputAction CreateKeyboardNavigateAction()
    {
        InputActionAsset asset = ScriptableObject.CreateInstance<InputActionAsset>();
        InputAction action = asset.AddActionMap("UI").AddAction("Navigate", InputActionType.Value, expectedControlLayout: "Vector2");

        action.AddBinding("<Joystick>/stick");
        action.AddCompositeBinding("2DVector")
            .With("Up", "<Keyboard>/w").With("Up", "<Keyboard>/upArrow")
            .With("Down", "<Keyboard>/s").With("Down", "<Keyboard>/downArrow")
            .With("Left", "<Keyboard>/a").With("Left", "<Keyboard>/leftArrow")
            .With("Right", "<Keyboard>/d").With("Right", "<Keyboard>/rightArrow");
        asset.Enable();
        return action;
    }

    private static InputSystemUIInputModule CurrentUIModule()
    {
        if (EventSystem.current == null)
            return null;
        return EventSystem.current.currentInputModule as InputSystemUIInputModule;
    }

    // The default UI Navigate is PassThrough and would also move on the gamepad, so it gets a keyboard-only Value action; the gamepad stick and dpad are handled in Moves.
    private void PatchNavigateAction()
    {
        InputSystemUIInputModule module = CurrentUIModule();

        if (module == null || module == patchedModule)
            return;
        if (navigateAction == null)
            navigateAction = CreateKeyboardNavigateAction();
        patchedModule = module;
        module.move = InputActionReference.Create(navigateAction);
    }
}
