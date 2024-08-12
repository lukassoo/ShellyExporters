using System.Text.Json;
using Serilog;
using Utilities.Networking.RequestHandling;

namespace ShellyPlugExporter;

public class ShellyPlugConnection
{
    static readonly ILogger log = Log.ForContext(typeof(ShellyPlugConnection));
    
    readonly string targetName;
    readonly string targetUrl;

    DateTime lastRequest = DateTime.MinValue;
        
    // A minimum time between requests of 0.8s - the device updates the reading 1/s, it takes time to request the data and respond to Prometheus, 200ms should be enough
    readonly TimeSpan minimumTimeBetweenRequests = TimeSpan.FromSeconds(0.8);

    readonly bool ignoreRelayState;
    readonly bool ignoreCurrentPower;
    readonly bool ignoreTemperature;

    bool relayStatus;
    float currentlyUsedPower;
    float temperature;

    readonly HttpRequestHandler requestHandler;
    
    public ShellyPlugConnection(TargetDevice target)
    {
        targetName = target.name;
        targetUrl = target.url + "/status";

        ignoreCurrentPower = target.ignorePowerMetric;
        ignoreTemperature = target.ignoreTemperatureMetric;
        ignoreRelayState = target.ignoreRelayStateMetric;

        requestHandler = new HttpRequestHandler(targetUrl, target.RequiresAuthentication());
        
        if (target.RequiresAuthentication())
        {
            requestHandler.SetAuth(target.username, target.password);
        }
    }

    public string GetTargetName()
    {
        return targetName;
    }

    public string GetTargetUrl()
    {
        return targetUrl;
    }

    public bool IsPowerIgnored()
    {
        return ignoreCurrentPower;
    }

    public string GetCurrentPowerAsString()
    {
        return currentlyUsedPower.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture);
    }

    public bool IsRelayStateIgnored()
    {
        return ignoreRelayState;
    }

    public string IsRelayOnAsString()
    {
        return relayStatus ? "1" : "0";
    }

    public bool IsTemperatureIgnored()
    {
        return ignoreTemperature;
    }

    public string GetTemperatureAsString()
    {
        return temperature.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture);
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

            if (!ignoreCurrentPower)
            {
                currentlyUsedPower = json.RootElement.GetProperty("meters")[0].GetProperty("power").GetSingle();
            }

            if (!ignoreTemperature)
            {
                temperature = json.RootElement.GetProperty("temperature").GetSingle();
            }
        
            if (!ignoreRelayState)
            {
                relayStatus = json.RootElement.GetProperty("relays")[0].GetProperty("ison").GetBoolean();
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