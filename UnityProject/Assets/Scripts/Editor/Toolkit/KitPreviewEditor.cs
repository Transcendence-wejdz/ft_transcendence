using UnityEditor;

// Base for inspectors with playable previews: advances them every editor frame and repaints while one plays.
public abstract class KitPreviewEditor : Editor
{
    private double lastTick;

    // Advances every preview by delta seconds; returns true when one needs a repaint.
    protected abstract bool AdvancePreviews(float delta);

    private void Tick()
    {
        double now = EditorApplication.timeSinceStartup;
        float delta = (float)(now - lastTick);

        lastTick = now;
        if (AdvancePreviews(delta))
            Repaint();
    }

    protected virtual void OnEnable()
    {
        lastTick = EditorApplication.timeSinceStartup;
        EditorApplication.update += Tick;
    }

    protected virtual void OnDisable()
    {
        EditorApplication.update -= Tick;
    }
}
