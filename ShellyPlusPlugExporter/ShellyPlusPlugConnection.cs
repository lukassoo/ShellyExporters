using System.Text.Json;
using Serilog;
using Utilities.Networking.RequestHandling.WebSockets;

namespace ShellyPlusPlugExporter;

public class ShellyPlusPlugConnection
{
    static readonly ILogger log = Log.ForContext<ShellyPlusPlugConnection>();
    
    readonly string targetName;

    DateTime lastRequest = DateTime.MinValue;
        
    // A minimum time between requests of 0.8s - the device updates the reading 1/s, it takes time to request the data and respond to Prometheus, 200ms should be enough
    readonly TimeSpan minimumTimeBetweenRequests = TimeSpan.FromSeconds(0.8);

    public bool IgnoreTotalPower { get; }
    public float TotalPower { get; private set; }

    public bool IgnoreCurrentPower { get; }
    public float CurrentlyUsedPower { get; private set; }

    public bool IgnoreVoltage { get; }
    public float Voltage { get; private set; }

    public bool IgnoreCurrent { get; }
    public float Current { get; private set; }

    public bool IgnoreRelayState { get; }
    public bool RelayStatus { get; private set; }

    public bool IgnoreTemperature { get; }
    public float Temperature { get; private set; }

    readonly WebSocketHandler requestHandler;
    
    public ShellyPlusPlugConnection(TargetDevice target)
    {
        targetName = target.name;
        string targetUrl = target.url + "/rpc";

        IgnoreTotalPower = target.ignoreTotalPowerMetric;
        IgnoreCurrentPower = target.ignorePowerMetric;
        IgnoreVoltage = target.ignoreVoltageMetric;
        IgnoreCurrent = target.ignoreCurrentMetric;
        IgnoreTemperature = target.ignoreTemperatureMetric;
        IgnoreRelayState = target.ignoreRelayStateMetric;

        RequestObject requestObject = new("Switch.GetStatus")
        {
            MethodParams = new IdParam
            {
                Id = 0
            }
        };
        
        TimeSpan requestTimeoutTime = TimeSpan.FromSeconds(target.requestTimeoutTime);
        
        requestHandler = new WebSocketHandler(targetUrl, requestObject, requestTimeoutTime);
        
        if (target.RequiresAuthentication())
        {
            requestHandler.SetAuth(target.password);
        }
    }

    public string GetTargetName()
    {
        return targetName;
    }
    
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
            JsonElement resultElement = json.RootElement.GetProperty("result");

            if (!IgnoreTotalPower)
            {
                TotalPower = resultElement.GetProperty("aenergy").GetProperty("total").GetSingle();
            }
            
            if (!IgnoreCurrentPower)
            {
                CurrentlyUsedPower = resultElement.GetProperty("apower").GetSingle();
            }

            if (!IgnoreVoltage)
            {
                Voltage = resultElement.GetProperty("voltage").GetSingle();
            }
        
            if (!IgnoreVoltage)
            {
                Current = resultElement.GetProperty("current").GetSingle();
            }
        
            if (!IgnoreTemperature)
            {
                Temperature = resultElement.GetProperty("temperature").GetProperty("tC").GetSingle();
            }
        
            if (!IgnoreRelayState)
            {
                RelayStatus = resultElement.GetProperty("output").GetBoolean();
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