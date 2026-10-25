using TMPro;
using UnityEngine;

// Lobby gold balance (UI_Lobby only).
public class GoldCounterUI : MonoBehaviour
{
    [SerializeField] private TMP_Text amountText;

    void OnEnable()
    {
        GoldWallet.OnChanged += Refresh;
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
