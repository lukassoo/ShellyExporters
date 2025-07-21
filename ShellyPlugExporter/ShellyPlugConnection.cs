using System.Text.Json;
using Serilog;
using Utilities.Networking.RequestHandling;

namespace ShellyPlugExporter;

public class ShellyPlugConnection
{
    static readonly ILogger log = Log.ForContext<ShellyPlugConnection>();
    
    readonly string targetName;

    DateTime lastRequest = DateTime.MinValue;
        
    // A minimum time between requests of 0.8s - the device updates the reading 1/s, it takes time to request the data and respond to Prometheus, 200ms should be enough
    readonly TimeSpan minimumTimeBetweenRequests = TimeSpan.FromSeconds(0.8);

    public bool IgnoreRelayState { get; }
    public bool RelayStatus { get; private set; }

    public bool IgnoreCurrentPower { get; }
    public float CurrentlyUsedPower { get; private set; }

    public bool IgnoreTemperature { get; }
    public float Temperature { get; private set; }
    
    readonly HttpRequestHandler requestHandler;
    
    public ShellyPlugConnection(TargetDevice target)
    {
        targetName = target.name;
        string targetUrl1 = target.url + "/status";

        IgnoreCurrentPower = target.ignorePowerMetric;
        IgnoreTemperature = target.ignoreTemperatureMetric;
        IgnoreRelayState = target.ignoreRelayStateMetric;

        requestHandler = new HttpRequestHandler(targetUrl1, target.RequiresAuthentication());
        
        if (target.RequiresAuthentication())
        {
            requestHandler.SetAuth(target.username, target.password);
        }
    }

    public string GetTargetName()
    {
        return targetName;
    }

    // Gets the current power flowing through the plug but only when necessary - set through minimumTimeBetweenRequests
    public async Task<bool> UpdateMetricsIfNecessary()
    {
        if (DateTime.UtcNow - lastRequest < minimumTimeBetweenRequests)
        {
            return true;
        }
        
        lastRequest = DateTime.UtcNow;

        string? requestResponse = await requestHandler.Request();

        if (string.IsNullOrEmpty(requestResponse))
        {
            log.Error("Request response null or empty - could not update metrics");
            return false;
        }

        try
        {
            JsonDocument json = JsonDocument.Parse(requestResponse);

            if (!IgnoreCurrentPower)
            {
                CurrentlyUsedPower = json.RootElement.GetProperty("meters")[0].GetProperty("power").GetSingle();
            }

            if (!IgnoreTemperature)
            {
                Temperature = json.RootElement.GetProperty("temperature").GetSingle();
            }
        
            if (!IgnoreRelayState)
            {
                RelayStatus = json.RootElement.GetProperty("relays")[0].GetProperty("ison").GetBoolean();
            }

            return true;
        }
        catch (Exception exception)
        {
            log.Error(exception, "Failed to parse response");
            return false;
        }
    }
}