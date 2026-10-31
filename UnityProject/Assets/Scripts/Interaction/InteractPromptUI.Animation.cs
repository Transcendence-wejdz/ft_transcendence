using System.Collections;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

// Prompt open/close tween and the keycap blink.
public partial class InteractPromptUI
{
    private void StopBlink()
    {
        if (blinkAnimation == null)
            return;
        StopCoroutine(blinkAnimation);
        blinkAnimation = null;
        keyCap.localScale = Vector3.one;
        keyCapGroup.alpha = 1f;
    }

    // The keycap animates alone when the label always shows, else the whole prompt.
    private void StartAnimation(bool opening)
    {
        if (activeAnimation != null)
            StopCoroutine(activeAnimation);
        if (labelAlwaysVisible)
            activeAnimation = StartCoroutine(Animate(keyCap, keyCapGroup, opening));
        else
            activeAnimation = StartCoroutine(Animate(promptRoot, canvasGroup, opening));
    }

    private void OnAnimationDone(RectTransform target, bool opening)
    {
        activeAnimation = null;
        if (opening && labelAlwaysVisible && blinkAnimation == null)
            blinkAnimation = StartCoroutine(BlinkKeyCap());
        if (!opening && target == promptRoot)
            promptRoot.gameObject.SetActive(false);
    }

    // Unscaled-time scale and alpha tween from the current state, so it works while paused and when interrupted.
    private IEnumerator Animate(RectTransform target, CanvasGroup group, bool opening)
    {
        float fromScale = target.localScale.x;
        float fromAlpha = group.alpha;
        float toScale = closedScale;
        float toAlpha = 0f;
        float t = 0f;
        float eased;

        if (opening)
        {
            toScale = 1f;
            toAlpha = 1f;
        }
        while (t < animationDuration)
        {
            t += Time.unscaledDeltaTime;
            eased = 1f - Mathf.Pow(1f - Mathf.Clamp01(t / animationDuration), 3f);
            target.localScale = Vector3.one * Mathf.Lerp(fromScale, toScale, eased);
            group.alpha = Mathf.Lerp(fromAlpha, toAlpha, eased);
            yield return null;
        }
        target.localScale = Vector3.one * toScale;
        group.alpha = toAlpha;
        OnAnimationDone(target, opening);
    }

    // Pulses the keycap's alpha and scale.
    private IEnumerator BlinkKeyCap()
    {
        float wave;

        while (true)
        {
            wave = (Mathf.Sin(Time.unscaledTime * keyCapBlinkSpeed) + 1f) * 0.5f;
            keyCapGroup.alpha = Mathf.Lerp(keyCapBlinkMinAlpha, 1f, wave);
            keyCap.localScale = Vector3.one * Mathf.Lerp(1f - keyCapBlinkScaleAmplitude, 1f, wave);
            yield return null;
        }
    }
}
