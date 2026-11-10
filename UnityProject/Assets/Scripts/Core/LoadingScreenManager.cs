using System.Collections;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Persistent loading screen shown across scene transitions.
public partial class LoadingScreenManager : MonoBehaviour
{
    private const int SortingOrder = 9000;
    private const float FadeDuration = 0.15f;
    private const float MinimumVisibleTime = 0.3f;
    private const float LoadedProgress = 0.9f;
    private const float PlayerReadyTimeout = 3f;
    private const string DefaultLabel = "Loading...";

    private static readonly Vector2 Center = new Vector2(0.5f, 0.5f);

    private static LoadingScreenManager instance;

    private CanvasGroup canvasGroup;
    private TextMeshProUGUI loadingLabel;
    private GameObject progressBarRoot;
    private Image progressFill;
    private Coroutine fadeRoutine;
    private NetworkManager subscribedNetworkManager;
    private NetworkSceneManager subscribedSceneManager;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        GameObject go = new GameObject("LoadingScreenManager");

        DontDestroyOnLoad(go);
        instance = go.AddComponent<LoadingScreenManager>();
    }

    private static NetworkSceneManager CurrentSceneManager(NetworkManager manager)
    {
        if (manager == null)
            return null;
        return manager.SceneManager;
    }

    private static bool IsLocalPlayerDead()
    {
        GameObject localPlayer = LocalPlayer.Get();
        Player player;

        if (localPlayer == null)
            return false;
        player = localPlayer.GetComponent<Player>();
        return player != null && player.IsDead;
    }

    private void SetProgress(float value)
    {
        if (progressFill != null)
            progressFill.fillAmount = Mathf.Clamp01(value);
    }

    private IEnumerator FadeRoutine(float targetAlpha)
    {
        float start = canvasGroup.alpha;
        float t = 0f;

        canvasGroup.blocksRaycasts = true;
        while (t < FadeDuration)
        {
            t += Time.unscaledDeltaTime;
            canvasGroup.alpha = Mathf.Lerp(start, targetAlpha, t / FadeDuration);
            yield return null;
        }
        canvasGroup.alpha = targetAlpha;
        canvasGroup.blocksRaycasts = targetAlpha > 0f;
        fadeRoutine = null;
    }

    private void Fade(float targetAlpha)
    {
        if (fadeRoutine != null)
            StopCoroutine(fadeRoutine);
        fadeRoutine = StartCoroutine(FadeRoutine(targetAlpha));
    }

    private void ShowSceneProgress()
    {
        progressBarRoot.SetActive(true);
        loadingLabel.text = DefaultLabel;
        SetProgress(0f);
        Fade(1f);
    }

    // Holds the screen up for MinimumVisibleTime, so a fast load doesn't just flash it.
    private IEnumerator LoadSceneRoutine(string sceneName)
    {
        float shownAt = Time.unscaledTime;
        AsyncOperation op;
        float remaining;

        ShowSceneProgress();
        op = SceneManager.LoadSceneAsync(sceneName);
        op.allowSceneActivation = false;
        while (op.progress < LoadedProgress)
        {
            SetProgress(op.progress / LoadedProgress);
            yield return null;
        }
        SetProgress(1f);
        remaining = MinimumVisibleTime - (Time.unscaledTime - shownAt);
        if (remaining > 0f)
            yield return new WaitForSecondsRealtime(remaining);
        op.allowSceneActivation = true;
        yield return op;
        Hide();
    }

    // Waits for the local player to finish reviving/repositioning before dropping the curtain.
    private IEnumerator HideOnceLocalPlayerReady()
    {
        float deadline = Time.unscaledTime + PlayerReadyTimeout;

        while (Time.unscaledTime < deadline && IsLocalPlayerDead())
            yield return null;
        Hide();
    }

    private void HandleNetworkSceneEvent(SceneEvent sceneEvent)
    {
        if (sceneEvent.SceneEventType == SceneEventType.Load)
            ShowSceneProgress();
        else if (sceneEvent.SceneEventType == SceneEventType.LoadEventCompleted)
            StartCoroutine(HideOnceLocalPlayerReady());
    }

    public static void LoadScene(string sceneName)
    {
        if (instance != null)
            instance.StartCoroutine(instance.LoadSceneRoutine(sceneName));
        else
            SceneManager.LoadScene(sceneName);
    }

    // Manual show/hide for async work with no scene load, e.g. creating/joining a session.
    public static void Show(string label = DefaultLabel)
    {
        if (instance == null)
            return;
        instance.loadingLabel.text = label;
        instance.progressBarRoot.SetActive(false);
        instance.Fade(1f);
    }

    public static void Hide()
    {
        if (instance == null)
            return;
        instance.progressBarRoot.SetActive(true);
        instance.loadingLabel.text = DefaultLabel;
        instance.Fade(0f);
    }

    private void Awake()
    {
        BuildUI();
    }

    // NetworkManager.SceneManager only exists once hosting/joining starts, so re-check it every frame instead of gating on the NetworkManager reference alone.
    private void Update()
    {
        NetworkManager currentManager = NetworkManager.Singleton;
        NetworkSceneManager currentSceneManager = CurrentSceneManager(currentManager);

        if (subscribedNetworkManager == currentManager && subscribedSceneManager == currentSceneManager)
            return;
        if (subscribedSceneManager != null)
            subscribedSceneManager.OnSceneEvent -= HandleNetworkSceneEvent;
        subscribedNetworkManager = currentManager;
        subscribedSceneManager = currentSceneManager;
        if (subscribedSceneManager != null)
            subscribedSceneManager.OnSceneEvent += HandleNetworkSceneEvent;
    }
}
