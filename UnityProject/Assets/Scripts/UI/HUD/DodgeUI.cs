using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// One pip per dodge charge of the local player; the recharging pip's overlay unwinds as it refills.
public class DodgeUI : MonoBehaviour
{
    [SerializeField] private GameObject pipTemplate;

    private readonly List<Image> overlays = new List<Image>();

    // Pips follow the class's charge count, which changes when the player switches class.
    private void RebuildPips(int count)
    {
        GameObject pip;

        foreach (Image overlay in overlays)
            Destroy(overlay.transform.parent.gameObject);
        overlays.Clear();
        for (int i = 0; i < count; i++)
        {
            pip = Instantiate(pipTemplate, pipTemplate.transform.parent);
            pip.SetActive(true);
            overlays.Add(pip.transform.GetChild(0).GetComponent<Image>());
        }
    }

    // Spent pips are fully covered, ready ones clear, and the one recharging unwinds.
    private static float OverlayFill(int pip, int charges, float recharge)
    {
        if (pip < charges)
            return 0f;
        if (pip == charges)
            return 1f - recharge;
        return 1f;
    }

    private static int MaxCharges(Player player)
    {
        if (player == null)
            return 0;
        return player.MaxDodgeCharges;
    }

    void Awake()
    {
        pipTemplate.SetActive(false);
    }


    void Update()
    {
        Player player = LocalPlayer.GetPlayer();
        int max = MaxCharges(player);

        if (overlays.Count != max)
            RebuildPips(max);
        if (max == 0)
            return;
        for (int i = 0; i < overlays.Count; i++)
            overlays[i].fillAmount = OverlayFill(i, player.DodgeCharges, player.DodgeRecharge01);
    }
}
