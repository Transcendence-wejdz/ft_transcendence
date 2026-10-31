using UnityEditor;
using UnityEngine;

// Mouse input of the canvas: scroll zoom, background pan and draggable points.
public partial class PreviewPanel
{
    private const float SnapStep = 0.05f;
    private const float GrabMargin = 4f;
    private const float ZoomPerScroll = 0.05f;
    private const int LeftButton = 0;
    private const int RightButton = 1;
    private const int MiddleButton = 2;

    private static float Snap(float value)
    {
        return Mathf.Round(value / SnapStep) * SnapStep;
    }

    private static float MirrorX(float x, bool mirrorX)
    {
        if (mirrorX)
            return -x;
        return x;
    }

    private static void ReleaseHot(int id, Event e)
    {
        if (GUIUtility.hotControl != id)
            return;
        GUIUtility.hotControl = 0;
        e.Use();
    }

    // The point under the cursor stays put.
    private void ZoomAround(Event e, Vector2 anchor, float fitScale)
    {
        float oldScale = fitScale * Zoom;
        float ratio;

        Zoom = Mathf.Clamp(Zoom * (1f - e.delta.y * ZoomPerScroll), MinZoom, MaxZoom);
        ratio = fitScale * Zoom / oldScale;
        Pan = e.mousePosition - (e.mousePosition - (anchor + Pan)) * ratio - anchor;
    }

    private static bool IsPanButton(int button)
    {
        return button == LeftButton || button == MiddleButton;
    }

    // Scroll zooms around the cursor, left or middle drag on the background pans.
    private void HandleZoomPan(Rect rect, Vector2 anchor, float fitScale, System.Action repaint)
    {
        int id = GUIUtility.GetControlID(FocusType.Passive, rect);
        Event e = Event.current;
        EventType type = e.GetTypeForControl(id);

        if (type == EventType.ScrollWheel && rect.Contains(e.mousePosition))
        {
            ZoomAround(e, anchor, fitScale);
            e.Use();
            repaint();
        }
        else if (type == EventType.MouseDown && IsPanButton(e.button) && rect.Contains(e.mousePosition))
        {
            GUIUtility.hotControl = id;
            e.Use();
        }
        else if (type == EventType.MouseDrag && GUIUtility.hotControl == id)
        {
            Pan += e.delta;
            e.Use();
            repaint();
        }
        else if (type == EventType.MouseUp)
            ReleaseHot(id, e);
    }

    // Right-click resets the point, left-click grabs it and freezes the view scale.
    private void GrabPoint(int id, Event e, Rect rect, Vector2 gui, float scale, SerializedProperty property)
    {
        float grab = HandleRadius + GrabMargin;

        if ((e.mousePosition - gui).sqrMagnitude > grab * grab || !rect.Contains(e.mousePosition))
            return;
        e.Use();
        if (e.button == RightButton)
        {
            property.vector2Value = Vector2.zero;
            return;
        }
        GUIUtility.hotControl = id;
        LockedScale = scale;
    }

    private static void DragPointTo(Event e, Vector2 worldZero, float scale, SerializedProperty property, bool mirrorX)
    {
        Vector2 local = (e.mousePosition - worldZero) / scale;

        property.vector2Value = new Vector2(Snap(MirrorX(local.x, mirrorX)), Snap(-local.y));
    }

    // Draggable world point bound to a Vector2 property (snapped, view frozen while dragging), right-click resets it; mirrorX shows and edits it flipped on X. Call before Begin.
    public void DragPoint(Rect rect, Vector2 anchor, float fitScale, SerializedProperty property, bool mirrorX, System.Action repaint)
    {
        float scale = ScaleFor(fitScale);
        Vector2 worldZero = anchor + Pan;
        Vector2 value = property.vector2Value;
        Vector2 gui = worldZero + new Vector2(MirrorX(value.x, mirrorX), -value.y) * scale;
        int id = GUIUtility.GetControlID(FocusType.Passive, rect);
        Event e = Event.current;
        EventType type = e.GetTypeForControl(id);

        if (type == EventType.MouseDown)
            GrabPoint(id, e, rect, gui, scale, property);
        else if (type == EventType.MouseDrag && GUIUtility.hotControl == id)
        {
            DragPointTo(e, worldZero, scale, property, mirrorX);
            e.Use();
            repaint();
        }
        else if (type == EventType.MouseUp && GUIUtility.hotControl == id)
        {
            LockedScale = -1f;
            ReleaseHot(id, e);
        }
        else if (type == EventType.Repaint)
            EditorGUIUtility.AddCursorRect(new Rect(gui.x - HandleRadius, gui.y - HandleRadius, HandleRadius * 2f, HandleRadius * 2f), MouseCursor.MoveArrow);
    }
}
