using UnityEditor;
using UnityEngine;

// Scene handle to drag a SortingLayer_Auto's sort pivot.
[CustomEditor(typeof(SortingLayer_Auto))]
[CanEditMultipleObjects]
public class SortingLayer_AutoEditor : Editor
{
    private const float PivotDiscRadius = 0.08f;

    private static void DrawPivot(Vector3 position, Vector3 worldPivot)
    {
        Handles.color = Color.yellow;
        Handles.DrawWireDisc(worldPivot, Vector3.forward, PivotDiscRadius);
        Handles.DrawLine(position, worldPivot);
    }

    private void OnSceneGUI()
    {
        SortingLayer_Auto sortScript = (SortingLayer_Auto)target;
        Transform t = sortScript.transform;
        Vector3 worldPivot = sortScript.SortPivotWorldPosition;
        Vector3 newWorldPivot;

        DrawPivot(t.position, worldPivot);
        EditorGUI.BeginChangeCheck();
        newWorldPivot = Handles.PositionHandle(worldPivot, Quaternion.identity);
        if (!EditorGUI.EndChangeCheck())
            return;
        Undo.RecordObject(sortScript, "Move Sort Pivot");
        sortScript.SortPivotOffset = t.InverseTransformPoint(newWorldPivot);
        EditorUtility.SetDirty(sortScript);
    }
}
