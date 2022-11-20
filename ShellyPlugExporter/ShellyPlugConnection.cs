using System.Text.Json;
using Utilities.Configs;

namespace ShellyPlugExporter;

public class ShellyPlugConnection
{
    string targetName;
    string targetUrl;

    DateTime lastRequest = DateTime.UtcNow;
        
    // A minimum time between requests of 0.8s - the Shelly Plug updates the reading 1/s, it takes time to request the data and respond to Prometheus, 200ms should be enough
    TimeSpan minimumTimeBetweenRequests = TimeSpan.FromSeconds(0.8);

    bool ignoreRelayState;
    bool relayStatus;

    bool ignoreCurrentPower;
    float currentlyUsedPower;

    bool ignoreTemperature;
    float temperature;

    public ShellyPlugConnection(TargetDevice target)
    {
        targetName = target.name;
        targetUrl = target.url + "/status";

        ignoreCurrentPower = target.ignorePowerMetric;
        ignoreTemperature = target.ignoreTemperatureMetric;
        ignoreRelayState = target.ignoreRelayStateMetric;

        if (target.RequiresAuthentication())
        {
            Utilities.HttpClient.AddCredentials(targetUrl, target.username, target.password);
        }
    }

    ~ShellyPlugConnection()
    {
        Utilities.HttpClient.RemoveCredentials(targetUrl);
    }

    public string GetTargetName()
    {
        return targetName;
    }

    public string GetTargetUrl()
    {
        return targetUrl;
    }

    public bool IsPowerIngored()
    {
        return ignoreCurrentPower;
    }

    public string GetCurrentPowerAsString()
    {
        UpdateMetricsIfNecessary().Wait();

        return currentlyUsedPower.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture);
    }

    public bool IsRelayStateIgnored()
    {
        return ignoreRelayState;
    }

    public string IsRelayOnAsString()
    {
        UpdateMetricsIfNecessary().Wait();

        return relayStatus ? "1" : "0";
    }

    public bool IsTemperatureIgnored()
    {
        return ignoreTemperature;
    }

    public string GetTemperatureAsString()
    {
        UpdateMetricsIfNecessary().Wait();

        return temperature.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture);
    }

    // Gets the current power flowing through the plug but only when necessary - set through minimumTimeBetweenRequests
    async Task UpdateMetricsIfNecessary()
    {
        if (DateTime.UtcNow - lastRequest < minimumTimeBetweenRequests)
        {
            return;
        }

        string requestResponse = await Utilities.HttpClient.GetRequestString(targetUrl);
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
        
        lastRequest = DateTime.Now;
    }
}