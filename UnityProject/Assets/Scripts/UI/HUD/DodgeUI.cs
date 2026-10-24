using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// One pip per dodge charge of the local player; the recharging pip's overlay unwinds as it refills.
public class DodgeUI : MonoBehaviour
{
    [SerializeField] private GameObject pipTemplate;

    private readonly List<Image> overlays = new List<Image>();

    void Awake()
    {
        pipTemplate.SetActive(false);
    }

    void Update()
    {
        Player player = LocalPlayer.GetPlayer();
        int max = player != null ? player.MaxDodgeCharges : 0;
        if (overlays.Count != max)
            RebuildPips(max);
        if (max == 0)
            return;

        int charges = player.DodgeCharges;
        float recharge = player.DodgeRecharge01;
        for (int i = 0; i < overlays.Count; i++)
            overlays[i].fillAmount = i < charges ? 0f : i == charges ? 1f - recharge : 1f;
    }

    // Pips follow the class's charge count, which changes when the player switches class.
    private void RebuildPips(int count)
    {
        foreach (Image overlay in overlays)
            Destroy(overlay.transform.parent.gameObject);
        overlays.Clear();

        for (int i = 0; i < count; i++)
        {
            GameObject pip = Instantiate(pipTemplate, pipTemplate.transform.parent);
            pip.SetActive(true);
            overlays.Add(pip.transform.GetChild(0).GetComponent<Image>());
        }
    }
}
