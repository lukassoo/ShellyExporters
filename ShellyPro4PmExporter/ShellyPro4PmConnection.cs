using System.Diagnostics;
using System.Globalization;
using System.Text.Json;
using Serilog;
using Utilities.Components;
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
    readonly BtHomeComponentsHandler? componentsHandler;

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

        // Setup components handler if enabled
        if (target.enableComponentMetrics)
        {
            string? password = target.RequiresAuthentication() ? target.password : null;
            componentsHandler = new BtHomeComponentsHandler(targetUrl, requestTimeoutTime, password);
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

    public IReadOnlyCollection<BtHomeDevice>? GetBtHomeDevices()
     => componentsHandler?.GetDevices();

    public bool HasComponentsEnabled()
        => componentsHandler != null;

    public string GetComponentMetrics(string metricPrefix)
     => componentsHandler?.GenerateMetrics(metricPrefix) ?? "";

    public async Task<bool> UpdateMetricsIfNecessary()
    {
        if (DateTime.UtcNow - lastSuccessfulRequest < minimumTimeBetweenRequests)
        {
            return true;
        }

        log.Debug("Updating metrics");

        requestStopWatch.Restart();

        // Make simultaneous requests
        Task<string?> statusTask = requestHandler.Request();
        Task<bool>? componentsTask = componentsHandler?.UpdateComponentsFromDevice();

        // Wait for status request (required)
        string? statusResponse = await statusTask;
        TimeSpan requestTime = requestStopWatch.Elapsed;
        log.Debug("Status request took: {requestTime} ms", requestTime.TotalMilliseconds.ToString("F1", CultureInfo.InvariantCulture));

        if (string.IsNullOrEmpty(statusResponse))
        {
            log.Error("Status request response null or empty - could not update metrics");
            return false;
        }

        // Update status metrics
        if (!UpdateMetrics(statusResponse))
        {
            log.Error("Failed to update status metrics");
            return false;
        }

        // Wait for and update components if enabled
        if (componentsTask != null)
        {
            bool componentsUpdated = await componentsTask;
            if (!componentsUpdated)
            {
                log.Warning("Failed to update component metrics, but continuing");
            }
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