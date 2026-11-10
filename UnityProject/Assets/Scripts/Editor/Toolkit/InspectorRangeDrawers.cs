using UnityEditor;
using UnityEngine;

// Slider on a power curve (fine control near Min) next to a typed field.
[CustomPropertyDrawer(typeof(PowerRangeAttribute))]
public class PowerRangeDrawer : PropertyDrawer
{
    private const float FieldWidth = 55f;
    private const float Spacing = 4f;
    private const int SliderDecimals = 4;

    private void DrawSlider(Rect rect, SerializedProperty property, PowerRangeAttribute range)
    {
        float t = Mathf.Pow(Mathf.InverseLerp(range.Min, range.Max, property.floatValue), 1f / range.Power);
        float value;

        EditorGUI.BeginChangeCheck();
        t = GUI.HorizontalSlider(rect, t, 0f, 1f);
        if (!EditorGUI.EndChangeCheck())
            return;
        value = Mathf.Lerp(range.Min, range.Max, Mathf.Pow(t, range.Power));
        property.floatValue = (float)System.Math.Round(value, SliderDecimals);
    }

    private void DrawField(Rect rect, SerializedProperty property, PowerRangeAttribute range)
    {
        float typed;

        EditorGUI.BeginChangeCheck();
        typed = EditorGUI.FloatField(rect, property.floatValue);
        if (EditorGUI.EndChangeCheck())
            property.floatValue = Mathf.Clamp(typed, range.Min, range.Max);
    }

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        PowerRangeAttribute range = (PowerRangeAttribute)attribute;
        Rect area;
        int indent;

        label = EditorGUI.BeginProperty(position, label, property);
        area = EditorGUI.PrefixLabel(position, label);
        indent = EditorGUI.indentLevel;
        EditorGUI.indentLevel = 0;
        DrawSlider(new Rect(area.x, area.y, area.width - FieldWidth - Spacing, area.height), property, range);
        DrawField(new Rect(area.xMax - FieldWidth, area.y, FieldWidth, area.height), property, range);
        EditorGUI.indentLevel = indent;
        EditorGUI.EndProperty();
    }
}

// Min and max fields around a MinMaxSlider, for a Vector2 range.
[CustomPropertyDrawer(typeof(MinMaxRangeAttribute))]
public class MinMaxRangeDrawer : PropertyDrawer
{
    private const float FieldWidth = 45f;
    private const float Spacing = 4f;
    private const int FieldDecimals = 3;

    private static float Rounded(float value)
    {
        return (float)System.Math.Round(value, FieldDecimals);
    }

    private void DrawRange(Rect area, SerializedProperty property, MinMaxRangeAttribute range)
    {
        Rect minRect = new Rect(area.x, area.y, FieldWidth, area.height);
        Rect sliderRect = new Rect(area.x + FieldWidth + Spacing, area.y, area.width - 2f * (FieldWidth + Spacing), area.height);
        Rect maxRect = new Rect(area.xMax - FieldWidth, area.y, FieldWidth, area.height);
        float min = property.vector2Value.x;
        float max = property.vector2Value.y;

        EditorGUI.BeginChangeCheck();
        min = EditorGUI.FloatField(minRect, Rounded(min));
        EditorGUI.MinMaxSlider(sliderRect, ref min, ref max, range.Min, range.Max);
        max = EditorGUI.FloatField(maxRect, Rounded(max));
        if (!EditorGUI.EndChangeCheck())
            return;
        min = Mathf.Clamp(min, range.Min, range.Max);
        max = Mathf.Clamp(max, min, range.Max);
        property.vector2Value = new Vector2(min, max);
    }

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        MinMaxRangeAttribute range = (MinMaxRangeAttribute)attribute;
        Rect area;
        int indent;

        label = EditorGUI.BeginProperty(position, label, property);
        area = EditorGUI.PrefixLabel(position, label);
        indent = EditorGUI.indentLevel;
        EditorGUI.indentLevel = 0;
        DrawRange(area, property, range);
        EditorGUI.indentLevel = indent;
        EditorGUI.EndProperty();
    }
}
