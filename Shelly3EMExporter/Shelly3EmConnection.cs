using System.Text.Json;
using Utilities.Networking.RequestHandling;

namespace Shelly3EMExporter;

public class Shelly3EmConnection
{
    string targetName;

    DateTime lastRequest = DateTime.MinValue;

    // A minimum time between requests of 0.8s - the device updates the reading 1/s, it takes time to request the data and respond to Prometheus, a bit of delay will reduce load
    TimeSpan minimumTimeBetweenRequests = TimeSpan.FromSeconds(0.8);
    
    bool ignoreRelayState;
    bool relayStatus;

    MeterReading[] meterReadings;

    readonly HttpRequestHandler requestHandler;
    
    public Shelly3EmConnection(TargetDevice targetDevice)
    {
        targetName = targetDevice.name;
        string targetUrl = targetDevice.url + "/status";
        
        ignoreRelayState = targetDevice.ignoreRelayStateMetric;
        
        requestHandler = new(targetUrl, targetDevice.RequiresAuthentication());
        
        if (targetDevice.RequiresAuthentication())
        {
            requestHandler.SetAuth(targetDevice.username, targetDevice.password);
        }

        int targetMeterCount = targetDevice.targetMeters.Length;
        meterReadings = new MeterReading[targetMeterCount];

        TargetMeter[] targetMeters = targetDevice.targetMeters;
        
        for (int i = 0; i < targetMeters.Length; i++)
        {
            meterReadings[i] = new MeterReading(targetMeters[i].index,
                                                targetMeters[i].ignorePower,
                                                targetMeters[i].ignoreCurrent,
                                                targetMeters[i].ignoreVoltage,
                                                targetMeters[i].ignorePowerFactor);
        }
    }

    public string GetTargetName()
    {
        return targetName;
    }
    
    public MeterReading[] GetCurrentMeterReadings()
    {
        UpdateMetricsIfNecessary().Wait();

        return meterReadings;
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
    
    public async Task UpdateMetricsIfNecessary()
    {
        if (DateTime.UtcNow - lastRequest < minimumTimeBetweenRequests)
        {
            return;
        }
        
        lastRequest = DateTime.UtcNow;

        string? requestResponse = await requestHandler.Request();
        
        if (string.IsNullOrEmpty(requestResponse))
        {
            Console.WriteLine("[ERR Request response null or empty - could not update metrics");
            throw new Exception("Update metrics request failed");
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

                if (!meterReading.currentIgnored)
                {
                    meterReading.current = targetMeterNode.GetProperty("current").GetSingle();
                }

                if (!meterReading.voltageIgnored)
                {
                    meterReading.voltage = targetMeterNode.GetProperty("voltage").GetSingle();
                }

                if (!meterReading.powerFactorIgnored)
                {
                    meterReading.powerFactor = targetMeterNode.GetProperty("pf").GetSingle();
                }
            }

            if (!ignoreRelayState)
            {
                relayStatus = json.RootElement.GetProperty("relays")[0].GetProperty("ison").GetBoolean();
            }
        }
        catch (Exception exception)
        {
            Console.WriteLine("[ERR] Failed to parse response, exception: \n" + exception.Message);
            throw;
        }
    }
}