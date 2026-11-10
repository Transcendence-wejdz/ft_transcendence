using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Services.Qos;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;

// Relay region list and QoS latency probes behind the Network settings tab.
public static class RelayRegions
{
    private const string QosServiceName = "relay";

    // Region passed to WithRelayNetwork; null lets Relay pick the best one.
    public static string Selected
    {
        get
        {
            if (string.IsNullOrEmpty(GameSettings.RelayRegion))
                return null;
            return GameSettings.RelayRegion;
        }
    }

    public static async Task<List<string>> ListAsync()
    {
        List<Region> regions;
        List<string> ids;

        await ServicesAuth.EnsureSignedInAsync();
        regions = await RelayService.Instance.ListRegionsAsync();
        ids = new List<string>(regions.Count);
        foreach (Region region in regions)
            ids.Add(region.Id);
        ids.Sort(System.StringComparer.Ordinal);
        return ids;
    }

    // Average latency in ms per region; throws where QoS can't run (UDP probes, so not in browsers).
    public static async Task<Dictionary<string, int>> PingAsync()
    {
        IList<IQosResult> results;
        Dictionary<string, int> latencies;

        await ServicesAuth.EnsureSignedInAsync();
        results = await QosService.Instance.GetSortedQosResultsAsync(QosServiceName, null);
        latencies = new Dictionary<string, int>(results.Count);
        foreach (IQosResult result in results)
            latencies[result.Region] = result.AverageLatencyMs;
        return latencies;
    }
}
