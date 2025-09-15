using System.Text.Json;
using Serilog;
using Utilities.Networking;
using Utilities.Networking.RequestHandling;

namespace ShellyEmExporter;

public class ShellyEmConnection : IDeviceConnection
{
    static readonly ILogger log = Log.ForContext<ShellyEmConnection>();

    readonly string targetName;

    DateTime lastRequest = DateTime.MinValue;

    // A minimum time between requests of 0.8s - the device updates the reading 1/s, it takes time to request the data and respond to Prometheus, a bit of delay will reduce load
    readonly TimeSpan minimumTimeBetweenRequests = TimeSpan.FromSeconds(0.8);
    
    public bool IsRelayStateIgnored { get; }
    bool relayStatus;

    readonly MeterReading[] meterReadings;

    readonly HttpRequestHandler requestHandler;
    
    public ShellyEmConnection(TargetDevice target)
    {
        targetName = target.name;
        string targetUrl = target.url + "/status";
        
        IsRelayStateIgnored = target.ignoreRelayStateMetric;
        
        requestHandler = new HttpRequestHandler(targetUrl, target.RequiresAuthentication());
        
        if (target.RequiresAuthentication())
        {
            requestHandler.SetAuth(target.username, target.password);
        }

        int targetMeterCount = target.targetMeters.Length;
        meterReadings = new MeterReading[targetMeterCount];

        TargetMeter[] targetMeters = target.targetMeters;
        
        for (int i = 0; i < targetMeters.Length; i++)
        {
            meterReadings[i] = new MeterReading(targetMeters[i].index,
                                                targetMeters[i].ignorePower,
                                                targetMeters[i].ignoreReactive,
                                                targetMeters[i].ignoreVoltage,
                                                targetMeters[i].ignorePowerFactor,
                                                targetMeters[i].ignoreTotal,
                                                targetMeters[i].ignoreTotalReturned,
                                                targetMeters[i].computeCurrent);
        }
    }

    public string GetTargetName()
    {
        return targetName;
    }
    
    public MeterReading[] GetCurrentMeterReadings()
    {
        return meterReadings;
    }
    
    public bool IsRelayOn()
    {
        return relayStatus;
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

            JsonElement metersNode = json.RootElement.GetProperty("emeters");
        
            foreach (MeterReading meterReading in meterReadings)
            {
                JsonElement targetMeterNode = metersNode[meterReading.meterIndex];

                if (!meterReading.powerIgnored)
                {
                    meterReading.power = targetMeterNode.GetProperty("power").GetSingle();
                }

                if (!meterReading.reactiveIgnored)
                {
                    meterReading.current = targetMeterNode.GetProperty("reactive").GetSingle();
                }

                if (!meterReading.voltageIgnored)
                {
                    meterReading.voltage = targetMeterNode.GetProperty("voltage").GetSingle();
                }

                if (!meterReading.powerFactorIgnored)
                {
                    meterReading.powerFactor = targetMeterNode.GetProperty("pf").GetSingle();
                }
                
                if (!meterReading.totalIgnored)
                {
                    meterReading.total = targetMeterNode.GetProperty("total").GetSingle();
                }
                
                if (!meterReading.totalReturnedIgnored)
                {
                    meterReading.totalReturned = targetMeterNode.GetProperty("total_returned").GetSingle();
                }

                if (meterReading.currentComputed)
                {
                    meterReading.current = targetMeterNode.GetProperty("power").GetSingle() / targetMeterNode.GetProperty("voltage").GetSingle();
                }
            }

            if (!IsRelayStateIgnored)
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