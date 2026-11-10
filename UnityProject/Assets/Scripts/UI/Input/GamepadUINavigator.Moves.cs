using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// Gamepad moves: stick and dpad with hysteresis, slider edit mode, per-column memory and wrapping inside a column.
public partial class GamepadUINavigator
{
    private const float StickPressThreshold = 0.6f;
    private const float StickReleaseThreshold = 0.4f;
    private const float StickKeepDirectionDeg = 60f;
    private const float StickRepeatDelay = 0.5f;
    private const float StickRepeatRate = 0.1f;
    private const float EditingHandleScale = 1.35f;
    private const float RowTolerance = 8f;
    private const float SliderStepFraction = 0.1f;

    private MoveDirection stickDirection = MoveDirection.None;
    private float nextStickMoveTime;
    private readonly Dictionary<Transform, Selectable> lastInColumn = new Dictionary<Transform, Selectable>();
    private Slider editingSlider;
    private Vector3 editingHandleBaseScale;

    private static bool IsHorizontal(MoveDirection direction)
    {
        return direction == MoveDirection.Left || direction == MoveDirection.Right;
    }

    private static Selectable SideNeighbour(Selectable from, MoveDirection direction)
    {
        if (direction == MoveDirection.Left)
            return from.FindSelectableOnLeft();
        return from.FindSelectableOnRight();
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

    // The dpad wins over the left stick when both are held.
    private static Vector2 ReadGamepadNavigation()
    {
        Gamepad pad = Gamepad.current;
        Vector2 dpad;

        if (pad == null)
            return Vector2.zero;
        dpad = pad.dpad.ReadValue();
        if (dpad.sqrMagnitude > 0f)
            return dpad;
        return pad.leftStick.ReadValue();
    }

    private static MoveDirection AxisDirection(Vector2 stick)
    {
        if (Mathf.Abs(stick.x) > Mathf.Abs(stick.y))
        {
            if (stick.x > 0f)
                return MoveDirection.Right;
            return MoveDirection.Left;
        }
        if (stick.y > 0f)
            return MoveDirection.Up;
        return MoveDirection.Down;
    }

    // A direction holds within 60 degrees and releases at a lower tilt than it engages, so a diagonal push never alternates.
    private MoveDirection StickDirection(Vector2 stick)
    {
        float threshold = StickPressThreshold;

        if (stickDirection != MoveDirection.None)
            threshold = StickReleaseThreshold;
        if (stick.magnitude < threshold)
            return MoveDirection.None;
        if (stickDirection != MoveDirection.None && Vector2.Angle(stick, DirectionVector(stickDirection)) < StickKeepDirectionDeg)
            return stickDirection;
        return AxisDirection(stick);
    }

    // A held direction repeats after a delay, then faster.
    private bool IsRepeatTooSoon(MoveDirection direction)
    {
        bool repeat = direction == stickDirection;

        if (repeat && Time.unscaledTime < nextStickMoveTime)
            return true;
        if (repeat)
            nextStickMoveTime = Time.unscaledTime + StickRepeatRate;
        else
            nextStickMoveTime = Time.unscaledTime + StickRepeatDelay;
        return false;
    }

    private bool TrySpecialMove(GameObject selected, MoveDirection direction)
    {
        return TryWrap(selected, direction) || TryMoveSlider(selected, direction) || TryCrossColumn(selected, direction);
    }

    // Left stick and dpad navigation with hysteresis, so a diagonal push no longer alternates between two moves.
    private void UpdateStickNavigation(GameObject selected)
    {
        Vector2 stick = ReadGamepadNavigation();
        MoveDirection direction = StickDirection(stick);
        AxisEventData data;

        if (direction == MoveDirection.None)
        {
            stickDirection = MoveDirection.None;
            return;
        }
        if (IsRepeatTooSoon(direction))
            return;
        stickDirection = direction;
        if (selected == null || TrySpecialMove(selected, direction))
            return;
        data = new AxisEventData(EventSystem.current) { moveDir = direction, moveVector = stick };
        ExecuteEvents.Execute(selected, data, ExecuteEvents.moveHandler);
    }
}
