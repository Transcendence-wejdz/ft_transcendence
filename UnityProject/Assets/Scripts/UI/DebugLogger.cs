using UnityEngine;

// Logs a message from a UnityEvent, for wiring quick checks in the inspector.
public class DebugLogger : MonoBehaviour
{
    public void Log(string message)
    {
        Debug.Log(message);
    }
}
