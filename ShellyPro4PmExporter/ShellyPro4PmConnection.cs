using System.Diagnostics;
using System.Globalization;
using System.Text.Json;
using Serilog;
using Utilities.Networking.RequestHandling.WebSockets;

namespace ShellyPro4PmExporter;

public class ShellyPro4PmConnection
{
    static readonly ILogger log = Log.ForContext(typeof(ShellyPro4PmConnection));

    readonly string targetName;

    DateTime lastSuccessfulRequest = DateTime.MinValue;
    readonly Stopwatch requestStopWatch = new();

    readonly TimeSpan minimumTimeBetweenRequests = TimeSpan.FromSeconds(0.8);

    readonly MeterReading[] meterReadings;

    readonly WebSocketHandler[] requestHandlers;

    public ShellyPro4PmConnection(TargetDevice target)
    {
        targetName = target.name;
        string targetUrl = target.url + "/rpc";

        TimeSpan requestTimeoutTime = TimeSpan.FromSeconds(target.requestTimeoutTime);

        int targetMeterCount = target.targetMeters.Length;
        meterReadings = new MeterReading[targetMeterCount];
        requestHandlers = new WebSocketHandler[targetMeterCount];

        TargetMeter[] targetMeters = target.targetMeters;

        for (int i = 0; i < targetMeters.Length; i++)
        {
            meterReadings[i] = new MeterReading(targetMeters[i]);

            RequestObject requestObject = new("Switch.GetStatus")
            {
                MethodParams = new IdParam
                {
                    Id = targetMeters[i].meterIndex
                }
            };

            requestHandlers[i] = new WebSocketHandler(targetUrl, requestObject, requestTimeoutTime);
        }

        if (target.RequiresAuthentication())
        {
            foreach (WebSocketHandler requestHandler in requestHandlers)
            {
                requestHandler.SetAuth(target.password);
            }
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

        for (int i = 0; i < requestHandlers.Length; i++)
        {
            log.Debug("Starting metrics request for switch {switchId}", i);

            requestStopWatch.Start();
            string? requestResponse = await requestHandlers[i].Request();
            requestStopWatch.Stop();

            log.Debug("Metrics request for switch {switchId} ended", i);

            TimeSpan requestTime = requestStopWatch.Elapsed;
            requestStopWatch.Reset();

            log.Debug("Metrics request for switch {switchId} took: {requestTime} ms", i, requestTime.TotalMilliseconds.ToString("F1", CultureInfo.InvariantCulture));

            if (string.IsNullOrEmpty(requestResponse))
            {
                log.Error("Request response for switch {switchId} null or empty - could not update metrics", i);
                return false;
            }

            if (!UpdateMetrics(requestResponse, i))
            {
                log.Error("Failed to update metrics for switch {switchId}", i);
                return false;
            }
        }

        log.Debug("Updating metrics completed");
        lastSuccessfulRequest = DateTime.UtcNow;
        return true;
    }

    bool UpdateMetrics(string requestResponse, int meterIndex)
    {
        try
        {
            JsonDocument json = JsonDocument.Parse(requestResponse);

            JsonElement paramsElement = json.RootElement.GetProperty("result");

            MeterReading meterReading = meterReadings[meterIndex];

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

            return true;
        }
        catch (Exception exception)
        {
            log.Error(exception, "Failed to parse metrics response for switch {switchId}", meterIndex);
            return false;
        }
    }
}