using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(PowerRangeAttribute))]
public class PowerRangeDrawer : PropertyDrawer
{
    private const float FieldWidth = 55f;

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        PowerRangeAttribute range = (PowerRangeAttribute)attribute;
        label = EditorGUI.BeginProperty(position, label, property);
        Rect area = EditorGUI.PrefixLabel(position, label);
        int indent = EditorGUI.indentLevel;
        EditorGUI.indentLevel = 0;

        Rect sliderRect = new Rect(area.x, area.y, area.width - FieldWidth - 4f, area.height);
        Rect fieldRect = new Rect(area.xMax - FieldWidth, area.y, FieldWidth, area.height);

        float t = Mathf.Pow(Mathf.InverseLerp(range.Min, range.Max, property.floatValue), 1f / range.Power);
        EditorGUI.BeginChangeCheck();
        t = GUI.HorizontalSlider(sliderRect, t, 0f, 1f);
        if (EditorGUI.EndChangeCheck())
            property.floatValue = (float)System.Math.Round(Mathf.Lerp(range.Min, range.Max, Mathf.Pow(t, range.Power)), 4);

        EditorGUI.BeginChangeCheck();
        float typed = EditorGUI.FloatField(fieldRect, property.floatValue);
        if (EditorGUI.EndChangeCheck())
            property.floatValue = Mathf.Clamp(typed, range.Min, range.Max);

        EditorGUI.indentLevel = indent;
        EditorGUI.EndProperty();
    }
}

[CustomPropertyDrawer(typeof(MinMaxRangeAttribute))]
public class MinMaxRangeDrawer : PropertyDrawer
{
    private const float FieldWidth = 45f;

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        MinMaxRangeAttribute range = (MinMaxRangeAttribute)attribute;
        label = EditorGUI.BeginProperty(position, label, property);
        Rect area = EditorGUI.PrefixLabel(position, label);
        int indent = EditorGUI.indentLevel;
        EditorGUI.indentLevel = 0;

        Rect minRect = new Rect(area.x, area.y, FieldWidth, area.height);
        Rect sliderRect = new Rect(area.x + FieldWidth + 4f, area.y, area.width - 2f * (FieldWidth + 4f), area.height);
        Rect maxRect = new Rect(area.xMax - FieldWidth, area.y, FieldWidth, area.height);

        Vector2 value = property.vector2Value;
        float min = value.x;
        float max = value.y;
        EditorGUI.BeginChangeCheck();
        min = EditorGUI.FloatField(minRect, (float)System.Math.Round(min, 3));
        EditorGUI.MinMaxSlider(sliderRect, ref min, ref max, range.Min, range.Max);
        max = EditorGUI.FloatField(maxRect, (float)System.Math.Round(max, 3));
        if (EditorGUI.EndChangeCheck())
        {
            min = Mathf.Clamp(min, range.Min, range.Max);
            max = Mathf.Clamp(max, min, range.Max);
            property.vector2Value = new Vector2(min, max);
        }

        EditorGUI.indentLevel = indent;
        EditorGUI.EndProperty();
    }
}
