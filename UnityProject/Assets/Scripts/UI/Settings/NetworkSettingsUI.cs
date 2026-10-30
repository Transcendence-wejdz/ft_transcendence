using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Network settings tab: every Relay region as a radio row sorted by QoS ping, plus the session ping averaged over each refresh window.
public class NetworkSettingsUI : MonoBehaviour
{
    [SerializeField] private Toggle rowTemplate;
    [SerializeField] private TMP_Text sessionPingLabel;
    [SerializeField] private float refreshInterval = 10f;
    [SerializeField] private float sampleInterval = 0.5f;

    private readonly Dictionary<string, Toggle> rows = new Dictionary<string, Toggle>();
    private readonly List<string> regions = new List<string>();
    private Dictionary<string, int> latencies = new Dictionary<string, int>();
    private float refreshTimer;
    private float sampleTimer;
    private int rttSum;
    private int rttSamples;
    private bool probing;
    private bool regionsLoaded;
    private bool warnedQos;

    private void Awake()
    {
        rowTemplate.gameObject.SetActive(false);
        AddRow(string.Empty);
    }

    // Probes right away on open, then every refreshInterval while the tab stays visible.
    private void OnEnable()
    {
        AddRow(GameSettings.RelayRegion);
        ShowRows();
        refreshTimer = refreshInterval;
        sampleTimer = 0f;
        rttSum = 0;
        rttSamples = 0;
        sessionPingLabel.text = FormatPing(PerfStatsHUD.CurrentPingMs());
    }

    // Unscaled since the pause menu may freeze time.
    private void Update()
    {
        sampleTimer += Time.unscaledDeltaTime;
        if (sampleTimer >= sampleInterval)
        {
            sampleTimer = 0f;
            int rtt = PerfStatsHUD.CurrentPingMs();
            if (rtt >= 0)
            {
                rttSum += rtt;
                rttSamples++;
            }
        }

        refreshTimer += Time.unscaledDeltaTime;
        if (refreshTimer < refreshInterval)
            return;

        refreshTimer = 0f;
        sessionPingLabel.text = rttSamples > 0 ? FormatPing(Mathf.RoundToInt((float)rttSum / rttSamples)) : FormatPing(-1);
        rttSum = 0;
        rttSamples = 0;
        _ = ProbeAsync();
    }

    // Empty id is the Auto row.
    private void AddRow(string region)
    {
        if (rows.ContainsKey(region))
            return;

        Toggle row = Instantiate(rowTemplate, rowTemplate.transform.parent);
        row.name = string.IsNullOrEmpty(region) ? "Region_Auto" : "Region_" + region;
        row.gameObject.SetActive(true);
        row.onValueChanged.AddListener(isOn => { if (isOn) GameSettings.RelayRegion = region; });
        rows[region] = row;
        regions.Add(region);
    }

    // Region list is fetched once, latencies on every call; failures leave "--" instead of breaking the tab.
    private async System.Threading.Tasks.Task ProbeAsync()
    {
        if (probing)
            return;
        probing = true;

        try
        {
            if (!regionsLoaded)
            {
                List<string> ids = await RelayRegions.ListAsync();
                if (this == null)
                    return;
                foreach (string id in ids)
                    AddRow(id);
                regionsLoaded = true;
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"NetworkSettingsUI: region list failed - {e.Message}");
        }

        try
        {
            latencies = await RelayRegions.PingAsync();
        }
        catch (System.Exception e)
        {
            latencies = new Dictionary<string, int>();
            if (!warnedQos)
                Debug.LogWarning($"NetworkSettingsUI: QoS ping failed - {e.Message}");
            warnedQos = true;
        }
        finally
        {
            probing = false;
        }

        if (this != null)
            ShowRows();
    }

    // Auto first (labelled with the region Relay would pick), then fastest to slowest, unprobed regions last by name.
    private void ShowRows()
    {
        string best = null;
        int bestMs = -1;
        foreach (KeyValuePair<string, int> pair in latencies)
        {
            if (bestMs < 0 || pair.Value < bestMs)
            {
                best = pair.Key;
                bestMs = pair.Value;
            }
        }

        regions.Sort(CompareRegions);
        for (int i = 0; i < regions.Count; i++)
        {
            string region = regions[i];
            Toggle row = rows[region];
            bool auto = string.IsNullOrEmpty(region);
            row.transform.SetSiblingIndex(i + 1);
            row.transform.Find("Label").GetComponent<TMP_Text>().text = auto ? (best != null ? $"Auto ({best})" : "Auto") : region;
            row.transform.Find("Ping").GetComponent<TMP_Text>().text = FormatPing(auto ? bestMs : Latency(region));
        }

        string selected = GameSettings.RelayRegion;
        rows[selected].SetIsOnWithoutNotify(true);
        foreach (KeyValuePair<string, Toggle> pair in rows)
            if (pair.Key != selected)
                pair.Value.SetIsOnWithoutNotify(false);
    }

    private int CompareRegions(string a, string b)
    {
        if (string.IsNullOrEmpty(a) || string.IsNullOrEmpty(b))
            return string.IsNullOrEmpty(a) ? (string.IsNullOrEmpty(b) ? 0 : -1) : 1;

        int msA = Latency(a);
        int msB = Latency(b);
        if (msA != msB)
            return msA < 0 ? 1 : msB < 0 ? -1 : msA.CompareTo(msB);
        return string.CompareOrdinal(a, b);
    }

    private int Latency(string region)
    {
        return latencies.TryGetValue(region, out int ms) ? ms : -1;
    }

    private static string FormatPing(int ms)
    {
        return ms >= 0 ? $"{ms} ms" : "--";
    }
}
