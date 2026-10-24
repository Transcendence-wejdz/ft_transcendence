using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

// Lobby gold balance; lives in the shared in-game UI, so it hides itself outside the Lobby scene.
public class GoldCounterUI : MonoBehaviour
{
    [SerializeField] private GameObject content;
    [SerializeField] private TMP_Text amountText;
    [SerializeField] private string lobbySceneName = "Lobby";

    void OnEnable()
    {
        GoldWallet.OnChanged += Refresh;
        content.SetActive(SceneManager.GetActiveScene().name == lobbySceneName);
        Refresh(GoldWallet.Balance);
    }

    void OnDisable()
    {
        GoldWallet.OnChanged -= Refresh;
    }

    private void Refresh(int balance)
    {
        amountText.text = GoldWallet.CanEarn ? balance.ToString() : "-";
    }
}
