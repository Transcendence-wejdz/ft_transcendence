using UnityEngine;

// Slider whose travel is curved (value = t^power), so small values get most of the precision; a field beside it takes exact input.
public class PowerRangeAttribute : PropertyAttribute
{
    public readonly float Min;
    public readonly float Max;
    public readonly float Power;

    public PowerRangeAttribute(float min, float max, float power)
    {
        Min = min;
        Max = max;
        Power = power;
    }
}

// Two-handle slider for a Vector2 (x = min, y = max).
public class MinMaxRangeAttribute : PropertyAttribute
{
    public readonly float Min;
    public readonly float Max;

    public MinMaxRangeAttribute(float min, float max)
    {
        Min = min;
        Max = max;
    }
}
