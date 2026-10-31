using TMPro;
using UnityEngine;

// Lobby gold balance (UI_Lobby only).
public class GoldCounterUI : MonoBehaviour
{
    [SerializeField] private TMP_Text amountText;

    private void Refresh(int balance)
    {
        if (GoldWallet.CanEarn)
            amountText.text = balance.ToString();
        else
            amountText.text = "-";
    }

    void OnEnable()
    {
        GoldWallet.OnChanged += Refresh;
        Refresh(GoldWallet.Balance);
    }

    void OnDisable()
    {
        GoldWallet.OnChanged -= Refresh;
    }
}
