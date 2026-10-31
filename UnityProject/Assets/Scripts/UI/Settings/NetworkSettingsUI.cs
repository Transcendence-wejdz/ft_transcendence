using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Network settings tab: every Relay region as a radio row sorted by QoS ping, plus the session ping averaged over each refresh window.
public partial class NetworkSettingsUI : MonoBehaviour
{
    private const string AutoRegion = "";

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

    private static string FormatPing(int ms)
    {
        if (ms >= 0)
            return $"{ms} ms";
        return "--";
    }

    private static string RowName(string region)
    {
        if (string.IsNullOrEmpty(region))
            return "Region_Auto";
        return "Region_" + region;
    }

    private static void SelectRegion(bool isOn, string region)
    {
        if (isOn)
            GameSettings.RelayRegion = region;
    }

    // The region list is fetched once; a failure only leaves the list shorter.
    private async System.Threading.Tasks.Task LoadRegionsAsync()
    {
        List<string> ids;

        try
        {
            ids = await RelayRegions.ListAsync();
            if (this == null)
                return;
            foreach (string id in ids)
                AddRow(id);
            regionsLoaded = true;
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"NetworkSettingsUI: region list failed - {e.Message}");
        }
    }

    // QoS probes are UDP, so browsers always fail: warned once, then just "--".
    private async System.Threading.Tasks.Task PingRegionsAsync()
    {
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
    }

    // Region list is fetched once, latencies on every call; failures leave "--" instead of breaking the tab.
    private async System.Threading.Tasks.Task ProbeAsync()
    {
        if (probing)
            return;
        probing = true;
        if (!regionsLoaded)
            await LoadRegionsAsync();
        if (this == null)
        {
            probing = false;
            return;
        }
        await PingRegionsAsync();
        probing = false;
        if (this != null)
            ShowRows();
    }

    private void SamplePing()
    {
        int rtt = PerfStatsHUD.CurrentPingMs();

        if (rtt < 0)
            return;
        rttSum += rtt;
        rttSamples++;
    }

    private int AveragePing()
    {
        if (rttSamples <= 0)
            return -1;
        return Mathf.RoundToInt((float)rttSum / rttSamples);
    }

    private void ResetPingAverage()
    {
        rttSum = 0;
        rttSamples = 0;
    }

    private void Awake()
    {
        rowTemplate.gameObject.SetActive(false);
        AddRow(AutoRegion);
    }

    // Probes right away on open, then every refreshInterval while the tab stays visible.
    private void OnEnable()
    {
        AddRow(GameSettings.RelayRegion);
        ShowRows();
        refreshTimer = refreshInterval;
        sampleTimer = 0f;
        ResetPingAverage();
        sessionPingLabel.text = FormatPing(PerfStatsHUD.CurrentPingMs());
    }

    // Unscaled since the pause menu may freeze time.
    private void Update()
    {
        sampleTimer += Time.unscaledDeltaTime;
        if (sampleTimer >= sampleInterval)
        {
            sampleTimer = 0f;
            SamplePing();
        }
        refreshTimer += Time.unscaledDeltaTime;
        if (refreshTimer < refreshInterval)
            return;
        refreshTimer = 0f;
        sessionPingLabel.text = FormatPing(AveragePing());
        ResetPingAverage();
        _ = ProbeAsync();
    }
}
