using System.Text.Json;
using Utilities.Networking.RequestHandling.WebSockets;

namespace ShellyPlusPlugExporter;

public class ShellyPlusPlugConnection
{
    readonly string targetName;

    DateTime lastRequest = DateTime.MinValue;
        
    // A minimum time between requests of 0.8s - the device updates the reading 1/s, it takes time to request the data and respond to Prometheus, 200ms should be enough
    readonly TimeSpan minimumTimeBetweenRequests = TimeSpan.FromSeconds(0.8);

    readonly bool ignoreCurrentPower;
    readonly bool ignoreVoltage;
    readonly bool ignoreCurrent;
    readonly bool ignoreRelayState;
    readonly bool ignoreTemperature;

    float currentlyUsedPower;
    float voltage;
    float current;
    bool relayStatus;
    float temperature;

    readonly WebSocketHandler requestHandler;
    
    public ShellyPlusPlugConnection(TargetDevice target)
    {
        targetName = target.name;
        string targetUrl = target.url + "/rpc";

        ignoreCurrentPower = target.ignorePowerMetric;
        ignoreVoltage = target.ignoreVoltageMetric;
        ignoreCurrent = target.ignoreCurrentMetric;
        ignoreTemperature = target.ignoreTemperatureMetric;
        ignoreRelayState = target.ignoreRelayStateMetric;

        RequestObject requestObject = new("Switch.GetStatus")
        {
            MethodParams = new IdParam
            {
                Id = 0
            }
        };

        requestHandler = new WebSocketHandler(targetUrl, requestObject);
        
        if (target.RequiresAuthentication())
        {
            requestHandler.SetAuth(target.password);
        }
    }

    public string GetTargetName()
    {
        return targetName;
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
    
    public bool IsVoltageIgnored()
    {
        return ignoreVoltage;
    }

    public async Task<string> GetVoltageAsString()
    {
        await UpdateMetricsIfNecessary();

        return voltage.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture);
    }
    
    public bool IsCurrentIgnored()
    {
        return ignoreCurrent;
    }

    public async Task<string> GetCurrentAsString()
    {
        await UpdateMetricsIfNecessary();

        return current.ToString("0.000", System.Globalization.CultureInfo.InvariantCulture);
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
            Console.WriteLine("[ERR] Request response null or empty - could not update metrics");
            throw new Exception("Update metrics request failed");
        }

        try
        {
            JsonDocument json = JsonDocument.Parse(requestResponse);
            JsonElement resultElement = json.RootElement.GetProperty("result");
        
            if (!ignoreCurrentPower)
            {
                currentlyUsedPower = resultElement.GetProperty("apower").GetSingle();
            }

            if (!ignoreVoltage)
            {
                voltage = resultElement.GetProperty("voltage").GetSingle();
            }
        
            if (!ignoreVoltage)
            {
                current = resultElement.GetProperty("current").GetSingle();
            }
        
            if (!ignoreTemperature)
            {
                temperature = resultElement.GetProperty("temperature").GetProperty("tC").GetSingle();
            }
        
            if (!ignoreRelayState)
            {
                relayStatus = resultElement.GetProperty("output").GetBoolean();
            }
        }
        catch (Exception exception)
        {
            Console.WriteLine("[ERR] Failed to parse response, exception: \n" + exception.Message);
            throw;
        }
    }
}