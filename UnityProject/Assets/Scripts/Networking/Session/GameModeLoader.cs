using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

// Starts a game mode scene: through the party ready check when networked, straight away solo.
public class GameModeLoader : MonoBehaviour
{
    // Loads sceneName for the whole party right away, no ready check: host-driven scene change when networked, loading screen offline.
    public static void LoadNow(string sceneName)
    {
        NetworkManager network = NetworkManager.Singleton;

        if (network != null && network.IsListening)
            network.SceneManager.LoadScene(sceneName, LoadSceneMode.Single);
        else
            LoadingScreenManager.LoadScene(sceneName);
    }

    public void LoadScene(string sceneName)
    {
        bool isMultiplayerSession = NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening;

        if (isMultiplayerSession && ModeReadyCheck.Instance != null)
        {
            ModeReadyCheck.Instance.RequestReadyCheck(sceneName);
            return;
        }
        Time.timeScale = 1f;
        PauseManager.SetExternalPause(false);
        LoadingScreenManager.LoadScene(sceneName);
    }
}
