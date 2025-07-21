using System.Diagnostics;
using System.Globalization;
using System.Text.Json;
using Serilog;
using Utilities.Networking.RequestHandling.WebSockets;

namespace ShellyPro4PmExporter;

public class ShellyPro4PmConnection
{
    static readonly ILogger log = Log.ForContext<ShellyPro4PmConnection>();

    readonly string targetName;

    DateTime lastSuccessfulRequest = DateTime.MinValue;
    readonly Stopwatch requestStopWatch = new();

    readonly TimeSpan minimumTimeBetweenRequests = TimeSpan.FromSeconds(0.8);

    readonly MeterReading[] meterReadings;

    readonly WebSocketHandler requestHandler;

    public ShellyPro4PmConnection(TargetDevice target)
    {
        targetName = target.name;
        string targetUrl = target.url + "/rpc";

        TimeSpan requestTimeoutTime = TimeSpan.FromSeconds(target.requestTimeoutTime);

        RequestObject requestObject = new("Shelly.GetStatus");

        requestHandler = new WebSocketHandler(targetUrl, requestObject, requestTimeoutTime);
        
        if (target.RequiresAuthentication())
        {
            requestHandler.SetAuth(target.password);
        }
        
        int targetMeterCount = target.targetMeters.Length;
        meterReadings = new MeterReading[targetMeterCount];

        TargetMeter[] targetMeters = target.targetMeters;
        
        for (int i = 0; i < targetMeters.Length; i++)
        {
            meterReadings[i] = new MeterReading(targetMeters[i]);
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

    public async Task<bool> UpdateMetricsIfNecessary()
    {
        if (DateTime.UtcNow - lastSuccessfulRequest < minimumTimeBetweenRequests)
        {
            return true;
        }

        log.Debug("Updating metrics");

        requestStopWatch.Restart();
        string? requestResponse = await requestHandler.Request();
        TimeSpan requestTime = requestStopWatch.Elapsed;
        log.Debug("Metrics request took: {requestTime} ms", requestTime.TotalMilliseconds.ToString("F1", CultureInfo.InvariantCulture));

        if (string.IsNullOrEmpty(requestResponse))
        {
            log.Error("Request response null or empty - could not update metrics");
            return false;
        }

        if (!UpdateMetrics(requestResponse))
        {
            log.Error("Failed to update metrics");
            return false;
        }
        
        log.Debug("Updating metrics completed");
        lastSuccessfulRequest = DateTime.UtcNow;
        return true;
    }

    bool UpdateMetrics(string requestResponse)
    {
        try
        {
            JsonDocument json = JsonDocument.Parse(requestResponse);
            JsonElement resultElement = json.RootElement.GetProperty("result");

            foreach (MeterReading meterReading in meterReadings)
            {
                JsonElement paramsElement = resultElement.GetProperty($"switch:{meterReading.meterIndex}");
                
                if (!meterReading.currentIgnored)
                {
                    meterReading.current = paramsElement.GetProperty("current").GetSingle();
                }

                if (!meterReading.voltageIgnored)
                {
                    meterReading.voltage = paramsElement.GetProperty("voltage").GetSingle();
                }

                if (!meterReading.activePowerIgnored)
                {
                    meterReading.activePower = paramsElement.GetProperty("apower").GetSingle();
                }

                if (!meterReading.powerFactorIgnored)
                {
                    meterReading.powerFactor = paramsElement.GetProperty("pf").GetSingle();
                }

                if (!meterReading.frequencyIgnored)
                {
                    meterReading.frequency = paramsElement.GetProperty("freq").GetSingle();
                }

                if (!meterReading.totalActiveEnergyIgnored)
                {
                    meterReading.totalActiveEnergy = paramsElement.GetProperty("aenergy").GetProperty("total").GetSingle();
                }

                if (!meterReading.totalReturnedActiveEnergyIgnored)
                {
                    meterReading.totalReturnedActiveEnergy = paramsElement.GetProperty("ret_aenergy").GetProperty("total").GetSingle();
                }

                if (!meterReading.temperatureIgnored)
                {
                    meterReading.temperature = paramsElement.GetProperty("temperature").GetProperty("tC").GetSingle();
                }

                if (!meterReading.outputIgnored)
                {
                    meterReading.output = paramsElement.GetProperty("output").GetBoolean();
                }
            }
            
            return true;
        }
        catch (Exception exception)
        {
            log.Error(exception, "Failed to parse metrics response");
            return false;
        }
    }
}