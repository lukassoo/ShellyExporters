using System.Text.Json;
using Serilog;
using Utilities.Networking.RequestHandling.WebSockets;

namespace ShellyPlus1PmExporter;

public class ShellyPlus1PmConnection
{
    static readonly ILogger log = Log.ForContext(typeof(ShellyPlus1PmConnection));
    
    readonly string targetName;

    DateTime lastRequest = DateTime.MinValue;
        
    // A minimum time between requests of 0.8s - the device updates the reading 1/s, it takes time to request the data and respond to Prometheus, 200ms should be enough
    readonly TimeSpan minimumTimeBetweenRequests = TimeSpan.FromSeconds(0.8);

    public bool IgnoreTotalPower { get; }
    public float TotalPower { get; private set; }

    public bool IgnoreTotalPowerReturned { get; }
    public float TotalPowerReturned { get; private set; }
    
    public bool IgnoreCurrentPower { get; }
    public float CurrentlyUsedPower { get; private set; }

    public bool IgnoreVoltage { get; }
    public float Voltage { get; private set; }

    public bool IgnoreCurrent { get; }
    public float Current { get; private set; }

    public bool IgnorePowerFactor { get; }
    public float PowerFactor { get; private set; }
    
    public bool IgnoreFrequency { get; }
    public float Frequency { get; private set; }
    
    public bool IgnoreOutputState { get; }
    public bool OutputState { get; private set; }

    public bool IgnoreTemperature { get; }
    public float Temperature { get; private set; }
    
    public bool IgnoreInputState { get; }
    public bool InputState { get; private set; }
    
    public bool IgnoreInputPercent { get; }
    public float InputPercent { get; private set; }
    
    public bool IgnoreInputCountTotal { get; }
    public int InputCountTotal { get; private set; }
    
    public bool IgnoreInputFrequency { get; }
    public float InputFrequency { get; private set; }
    
    readonly WebSocketHandler switchRequestHandler;
    readonly WebSocketHandler? inputRequestHandler;
    
    public ShellyPlus1PmConnection(TargetDevice target)
    {
        targetName = target.name;
        string targetUrl = target.url + "/rpc";

        IgnoreTotalPower = target.ignoreTotalPowerMetric;
        IgnoreTotalPowerReturned = target.ignoreTotalPowerReturnedMetric;
        IgnorePowerFactor = target.ignorePowerFactor;
        IgnoreFrequency = target.ignoreFrequency;
        IgnoreCurrentPower = target.ignorePowerMetric;
        IgnoreVoltage = target.ignoreVoltageMetric;
        IgnoreCurrent = target.ignoreCurrentMetric;
        IgnoreTemperature = target.ignoreTemperatureMetric;
        IgnoreOutputState = target.ignoreOutputStateMetric;

        RequestObject requestObject = new("Switch.GetStatus")
        {
            MethodParams = new IdParam
            {
                Id = 0
            }
        };
        
        TimeSpan requestTimeoutTime = TimeSpan.FromSeconds(target.requestTimeoutTime);
        
        switchRequestHandler = new WebSocketHandler(targetUrl, requestObject, requestTimeoutTime);


        if (target.NeedsInputStatusRequests())
        {
            IgnoreInputState = target.ignoreInputState;
            IgnoreInputPercent = target.ignoreInputPercent;
            IgnoreInputCountTotal = target.ignoreInputCountTotal;
            IgnoreInputFrequency = target.ignoreInputFrequency;
            
            requestObject = new RequestObject("Switch.GetStatus")
            {
                MethodParams = new IdParam
                {
                    Id = 0
                }
            };
            
            inputRequestHandler = new WebSocketHandler(targetUrl, requestObject, requestTimeoutTime);
        }
        
        if (target.RequiresAuthentication())
        {
            switchRequestHandler.SetAuth(target.password);
            inputRequestHandler?.SetAuth(target.password);
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
        
        string? requestResponse = await switchRequestHandler.Request();
        
        if (string.IsNullOrEmpty(requestResponse))
        {
            log.Error("Request response null or empty - could not update metrics");
            return false;
        }

        if (!UpdateSwitchMetrics(requestResponse))
        {
            log.Error("Failed to update switch metrics");
            return false;
        }

        if (inputRequestHandler != null)
        {
            string? inputRequestResponse = await inputRequestHandler.Request();
        
            if (string.IsNullOrEmpty(inputRequestResponse))
            {
                log.Error("Request response null or empty - could not update metrics");
                return false;
            }

            if (!UpdateInputMetrics(inputRequestResponse))
            {
                log.Error("Failed to update input metrics");
                return false;
            }
        }

        return true;
    }

    bool UpdateSwitchMetrics(string requestResponse)
    {
        try
        {
            JsonDocument json = JsonDocument.Parse(requestResponse);
            JsonElement resultElement = json.RootElement.GetProperty("result");

            if (!IgnoreTotalPower)
            {
                TotalPower = resultElement.GetProperty("aenergy").GetProperty("total").GetSingle();
            }
            
            if (!IgnoreTotalPowerReturned)
            {
                TotalPowerReturned = resultElement.GetProperty("ret_aenergy").GetProperty("total").GetSingle();
            }
            
            if (!IgnoreCurrentPower)
            {
                CurrentlyUsedPower = resultElement.GetProperty("apower").GetSingle();
            }

            if (!IgnoreVoltage)
            {
                Voltage = resultElement.GetProperty("voltage").GetSingle();
            }
        
            if (!IgnoreCurrent)
            {
                Current = resultElement.GetProperty("current").GetSingle();
            }

            if (!IgnorePowerFactor)
            {
                PowerFactor = resultElement.GetProperty("pf").GetSingle();
            }

            if (!IgnoreFrequency)
            {
                Frequency = resultElement.GetProperty("freq").GetSingle();
            }
        
            if (!IgnoreTemperature)
            {
                Temperature = resultElement.GetProperty("temperature").GetProperty("tC").GetSingle();
            }
        
            if (!IgnoreOutputState)
            {
                OutputState = resultElement.GetProperty("output").GetBoolean();
            }

            return true;
        }
        catch (Exception exception)
        {
            log.Error(exception, "Failed to parse switch metrics response");
            return false;
        }
    }
    
    bool UpdateInputMetrics(string requestResponse)
    {
        try
        {
            JsonDocument json = JsonDocument.Parse(requestResponse);
            JsonElement resultElement = json.RootElement.GetProperty("result");

            if (!IgnoreInputState)
            {
                JsonElement stateProperty = resultElement.GetProperty("state");
                InputState = stateProperty.ValueKind != JsonValueKind.Null && stateProperty.GetBoolean();
            }
            
            if (!IgnoreInputPercent)
            {
                JsonElement percentProperty = resultElement.GetProperty("percent");

                if (percentProperty.ValueKind == JsonValueKind.Null)
                {
                    InputPercent = 0;
                }
                else
                {
                    InputPercent = percentProperty.GetSingle();   
                }
            }
            
            if (!IgnoreInputCountTotal)
            {
                if (resultElement.TryGetProperty("counts", out JsonElement countsProperty) &&
                    countsProperty.TryGetProperty("total", out JsonElement totalProperty))
                {
                    InputCountTotal = totalProperty.GetInt32();
                }
                else
                {
                    InputCountTotal = 0;
                }
            }

            if (!IgnoreInputFrequency)
            {
                if (resultElement.TryGetProperty("freq", out JsonElement frequencyProperty))
                {
                    InputFrequency = frequencyProperty.GetSingle();
                }
                else
                {
                    InputFrequency = 0;
                }
            }
            
            return true;
        }
        catch (Exception exception)
        {
            log.Error(exception, "Failed to parse input metrics response");
            return false;
        }
    }
}