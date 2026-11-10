using Unity.Cinemachine;
using UnityEngine;

// Scales the player camera's orthographic size by the Camera Distance setting.
[RequireComponent(typeof(CinemachineCamera))]
public class CameraDistanceSetting : MonoBehaviour
{
    private const float MinPunchDuration = 0.01f;

    private CinemachineCamera cinemachineCamera;
    private float baseSize;
    private float punchAmount;
    private float punchDuration;
    private float punchTime = -1f;

    // Brief zoom-in kick (amount = fraction of the size) that eases back out, layered on top of the distance setting.
    public void Punch(float amount, float duration)
    {
        punchAmount = Mathf.Max(punchAmount * PunchRemaining(), amount);
        punchDuration = Mathf.Max(MinPunchDuration, duration);
        punchTime = Time.unscaledTime;
    }

    private float PunchRemaining()
    {
        if (punchTime < 0f)
            return 0f;
        return 1f - Mathf.Clamp01((Time.unscaledTime - punchTime) / punchDuration);
    }

    private void Apply()
    {
        float remaining = PunchRemaining();
        LensSettings lens = cinemachineCamera.Lens;

        lens.OrthographicSize = baseSize * GameSettings.CameraDistance * (1f - punchAmount * remaining * remaining);
        cinemachineCamera.Lens = lens;
    }

    private void Awake()
    {
        cinemachineCamera = GetComponent<CinemachineCamera>();
        baseSize = cinemachineCamera.Lens.OrthographicSize;
    }

    private void OnEnable()
    {
        GameSettings.OnChanged += Apply;
        Apply();
    }

    private void OnDisable()
    {
        GameSettings.OnChanged -= Apply;
    }

    private void LateUpdate()
    {
        if (punchTime < 0f)
            return;
        if (PunchRemaining() <= 0f)
            punchTime = -1f;
        Apply();
    }
}
