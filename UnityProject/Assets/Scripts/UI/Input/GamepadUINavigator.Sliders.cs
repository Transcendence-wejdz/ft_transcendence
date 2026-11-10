using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// Slider edit mode: A starts or ends adjusting a slider, left/right then change its value.
public partial class GamepadUINavigator
{
    private static bool IsHorizontal(Slider slider)
    {
        return slider.direction == Slider.Direction.LeftToRight || slider.direction == Slider.Direction.RightToLeft;
    }

    private bool ShouldStopSliderEdit()
    {
        return !GamepadActive || !PauseManager.IsPaused || !editingSlider.isActiveAndEnabled;
    }

    private void StartSliderEdit(Slider slider)
    {
        editingSlider = slider;
        if (slider.handleRect == null)
            return;
        editingHandleBaseScale = slider.handleRect.localScale;
        slider.handleRect.localScale = editingHandleBaseScale * EditingHandleScale;
    }

    private void StopSliderEdit()
    {
        if (editingSlider != null && editingSlider.handleRect != null)
            editingSlider.handleRect.localScale = editingHandleBaseScale;
        editingSlider = null;
    }

    // A on a slider starts or ends adjusting it; moving off it ends it too.
    private void UpdateSliderEditing(GameObject selected)
    {
        Slider slider = null;

        if (selected != null)
            slider = selected.GetComponent<Slider>();
        if (editingSlider != null && editingSlider != slider)
            StopSliderEdit();
        if (slider == null || Gamepad.current == null || !Gamepad.current.buttonSouth.wasPressedThisFrame)
            return;
        if (editingSlider == slider)
            StopSliderEdit();
        else
            StartSliderEdit(slider);
    }

    private static void StepSlider(Slider slider, MoveDirection direction)
    {
        float sign = -1f;
        float step = (slider.maxValue - slider.minValue) * SliderStepFraction;

        if ((direction == MoveDirection.Right) == (slider.direction == Slider.Direction.LeftToRight))
            sign = 1f;
        if (slider.wholeNumbers)
            step = 1f;
        slider.value += sign * step;
    }

    private void SelectIfValid(Selectable target)
    {
        if (target != null && IsValidSelection(target.gameObject))
            EventSystem.current.SetSelectedGameObject(target.gameObject);
    }

    // Left/right on a horizontal slider adjust it in edit mode, otherwise leave it like any other control.
    private bool TryMoveSlider(GameObject selected, MoveDirection direction)
    {
        Slider slider;
        Selectable target;
        Selectable remembered;

        if (!IsHorizontal(direction))
            return false;
        slider = selected.GetComponent<Slider>();
        if (slider == null || !IsHorizontal(slider))
            return false;
        if (slider == editingSlider)
        {
            StepSlider(slider, direction);
            return true;
        }
        target = SideNeighbour(slider, direction);
        remembered = RememberedIn(target, slider);
        if (remembered != null)
            target = remembered;
        SelectIfValid(target);
        return true;
    }
}
