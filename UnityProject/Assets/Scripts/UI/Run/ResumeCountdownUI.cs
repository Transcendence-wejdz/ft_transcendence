using TMPro;
using UnityEngine;

// The red 3-2-1 shown over the frozen game before it resumes, after a disconnect pause or a level-up.
public class ResumeCountdownUI : MonoBehaviour
{
    [SerializeField] private TMP_Text countdownText;
    [SerializeField] private float pulseScale = 1.5f;
    [SerializeField] private float pulseEndAlpha = 0.35f;

    private LevelUpManager levelUp;

    private bool TryGetSecondsLeft(out float secondsLeft)
    {
        secondsLeft = 0f;
        if (GameOverCheck.IsResuming)
        {
            secondsLeft = GameOverCheck.ResumeSecondsLeft;
            return true;
        }
        if (GameOverCheck.IsWaitingForPlayers)
            return false;
        if (levelUp == null)
            levelUp = FindAnyObjectByType<LevelUpManager>();
        if (levelUp == null || !levelUp.IsResuming)
            return false;
        secondsLeft = levelUp.ResumeSecondsLeft;
        return true;
    }

    // Each number starts big and fades as its second runs out, on unscaled values since time is frozen.
    private void Show(float secondsLeft)
    {
        int number = Mathf.Max(1, Mathf.CeilToInt(secondsLeft));
        float phase = Mathf.Clamp01(number - secondsLeft);
        Color color = countdownText.color;

        countdownText.text = number.ToString();
        countdownText.transform.localScale = Vector3.one * Mathf.Lerp(pulseScale, 1f, phase);
        color.a = Mathf.Lerp(1f, pulseEndAlpha, phase);
        countdownText.color = color;
    }

    private void Awake()
    {
        countdownText.gameObject.SetActive(false);
    }

    private void Update()
    {
        float secondsLeft;
        bool counting = TryGetSecondsLeft(out secondsLeft);

        if (countdownText.gameObject.activeSelf != counting)
            countdownText.gameObject.SetActive(counting);
        if (counting)
            Show(secondsLeft);
    }
}
