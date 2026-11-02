using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Region rows: sorted by latency, labelled with their ping, Auto showing the region Relay would pick.
public partial class NetworkSettingsUI
{
    private int Latency(string region)
    {
        int ms;

        if (latencies.TryGetValue(region, out ms))
            return ms;
        return -1;
    }

    // Auto always sorts first.
    private static int CompareAuto(string a, string b)
    {
        if (!string.IsNullOrEmpty(a))
            return 1;
        if (string.IsNullOrEmpty(b))
            return 0;
        return -1;
    }

    // Unprobed (-1) regions sort after every probed one.
    private static int CompareLatency(int msA, int msB)
    {
        if (msA < 0)
            return 1;
        if (msB < 0)
            return -1;
        return msA.CompareTo(msB);
    }

    private int CompareRegions(string a, string b)
    {
        int msA;
        int msB;

        if (string.IsNullOrEmpty(a) || string.IsNullOrEmpty(b))
            return CompareAuto(a, b);
        msA = Latency(a);
        msB = Latency(b);
        if (msA != msB)
            return CompareLatency(msA, msB);
        return string.CompareOrdinal(a, b);
    }

    // Empty id is the Auto row.
    private void AddRow(string region)
    {
        Toggle row;

        if (rows.ContainsKey(region))
            return;
        row = Instantiate(rowTemplate, rowTemplate.transform.parent);
        row.name = RowName(region);
        row.gameObject.SetActive(true);
        row.onValueChanged.AddListener(isOn => SelectRegion(isOn, region));
        rows[region] = row;
        regions.Add(region);
    }

    // The region Relay would pick: the lowest QoS latency, or none before any probe answered.
    private string BestRegion(out int bestMs)
    {
        string best = null;

        bestMs = -1;
        foreach (KeyValuePair<string, int> pair in latencies)
        {
            if (bestMs >= 0 && pair.Value >= bestMs)
                continue;
            best = pair.Key;
            bestMs = pair.Value;
        }
        return best;
    }

    private static string AutoLabel(string best)
    {
        if (best != null)
            return $"Auto ({best})";
        return "Auto";
    }

    private void LabelRow(Toggle row, string region, string best, int bestMs)
    {
        TMP_Text label = row.transform.Find("Label").GetComponent<TMP_Text>();
        TMP_Text ping = row.transform.Find("Ping").GetComponent<TMP_Text>();

        if (string.IsNullOrEmpty(region))
        {
            label.text = AutoLabel(best);
            ping.text = FormatPing(bestMs);
            return;
        }
        label.text = region;
        ping.text = FormatPing(Latency(region));
    }

    private void SyncSelection()
    {
        string selected = GameSettings.RelayRegion;

        rows[selected].SetIsOnWithoutNotify(true);
        foreach (KeyValuePair<string, Toggle> pair in rows)
        {
            if (pair.Key != selected)
                pair.Value.SetIsOnWithoutNotify(false);
        }
    }

    // Auto first (labelled with the region Relay would pick), then fastest to slowest, unprobed regions last by name.
    private void ShowRows()
    {
        int bestMs;
        string best = BestRegion(out bestMs);

        regions.Sort(CompareRegions);
        for (int i = 0; i < regions.Count; i++)
        {
            rows[regions[i]].transform.SetSiblingIndex(i + 1);
            LabelRow(rows[regions[i]], regions[i], best, bestMs);
        }
        SyncSelection();
    }
}
