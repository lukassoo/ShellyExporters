using System.Text.Json;
using Utilities.Networking.RequestHandling;
using Utilities.Networking.RequestHandling.Handlers;

namespace ShellyPlugExporter;

public class ShellyPlugConnection
{
    readonly string targetName;
    readonly string targetUrl;

    DateTime lastRequest = DateTime.UtcNow;
        
    // A minimum time between requests of 0.8s - the Shelly Plug updates the reading 1/s, it takes time to request the data and respond to Prometheus, 200ms should be enough
    readonly TimeSpan minimumTimeBetweenRequests = TimeSpan.FromSeconds(0.8);

    readonly bool ignoreRelayState;
    readonly bool ignoreCurrentPower;
    readonly bool ignoreTemperature;

    bool relayStatus;
    float currentlyUsedPower;
    float temperature;

    readonly IRequestHandler requestHandler;
    
    public ShellyPlugConnection(TargetDevice target)
    {
        targetName = target.name;
        targetUrl = target.url + "/status";

        ignoreCurrentPower = target.ignorePowerMetric;
        ignoreTemperature = target.ignoreTemperatureMetric;
        ignoreRelayState = target.ignoreRelayStateMetric;

        HttpRequestHandler httpRequestHandler = new(targetUrl, target.RequiresAuthentication());
        requestHandler = httpRequestHandler;
        
        if (target.RequiresAuthentication())
        {
            httpRequestHandler.SetAuth(target.username, target.password);
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

    public async Task<string> GetCurrentPowerAsString()
    {
        await UpdateMetricsIfNecessary();

        return currentlyUsedPower.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture);
    }

    public bool IsRelayStateIgnored()
    {
        return ignoreRelayState;
    }

    public async Task<string> IsRelayOnAsString()
    {
        await UpdateMetricsIfNecessary();

        return relayStatus ? "1" : "0";
    }

    public bool IsTemperatureIgnored()
    {
        return ignoreTemperature;
    }

    public async Task<string> GetTemperatureAsString()
    {
        await UpdateMetricsIfNecessary();

        return temperature.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture);
    }

    // Gets the current power flowing through the plug but only when necessary - set through minimumTimeBetweenRequests
    async Task UpdateMetricsIfNecessary()
    {
        if (DateTime.UtcNow - lastRequest < minimumTimeBetweenRequests)
        {
            return;
        }
        
        lastRequest = DateTime.UtcNow;

        string? requestResponse = await requestHandler.Request();

        if (string.IsNullOrEmpty(requestResponse))
        {
            Console.WriteLine("[WRN] Request response null or empty - could not update metrics");
            return;
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
        }
        catch (Exception exception)
        {
            Console.WriteLine("Failed to parse response, exception: " + exception.Message);
        }
    }
}