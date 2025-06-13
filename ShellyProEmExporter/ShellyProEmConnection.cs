using System.Diagnostics;
using System.Globalization;
using System.Text.Json;
using Serilog;
using Utilities.Networking.RequestHandling.WebSockets;

namespace ShellyProEmExporter;

public class ShellyProEmConnection
{
    static readonly ILogger log = Log.ForContext<ShellyProEmConnection>();

    readonly string targetName;

    DateTime lastSuccessfulRequest = DateTime.MinValue;
    readonly Stopwatch requestStopWatch = new();

    // A minimum time between requests of 0.8s - the device updates the reading 1/s, it takes time to request the data and respond to Prometheus, a bit of delay will reduce load
    readonly TimeSpan minimumTimeBetweenRequests = TimeSpan.FromSeconds(0.8);

    readonly MeterReading[] meterReadings;
    static readonly string[] indexToPhaseMap = ["a", "b", "c"];
    
    public bool IsTotalActiveEnergyPhase1Ignored { get; }
    public float TotalActiveEnergyPhase1 { get; private set; }
    
    public bool IsTotalActiveEnergyPhase2Ignored { get; }
    public float TotalActiveEnergyPhase2 { get; private set; }
    
    public bool IsTotalActiveEnergyReturnedPhase1Ignored { get; }
    public float TotalActiveEnergyReturnedPhase1 { get; private set; }
    
    public bool IsTotalActiveEnergyReturnedPhase2Ignored { get; }
    public float TotalActiveEnergyReturnedPhase2 { get; private set; }
    
    readonly WebSocketHandler requestHandler;
    readonly WebSocketHandler? totalEnergyRequestHandler;
    
    public ShellyProEmConnection(TargetDevice target)
    {
        targetName = target.name;
        string targetUrl = target.url + "/rpc";
        
        RequestObject requestObject = new("EM1.GetStatus")
        {
            MethodParams = new IdParam
            {
                Id = 0
            }
        };

        TimeSpan requestTimeoutTime = TimeSpan.FromSeconds(target.requestTimeoutTime);
        
        requestHandler = new WebSocketHandler(targetUrl, requestObject, requestTimeoutTime);

        if (target.NeedsTotalEnergyRequests())
        {
            log.Information("Target device wants energy totals - setting up second request");
            
            IsTotalActiveEnergyPhase1Ignored = target.ignoreTotalActiveEnergyPhase1;
            IsTotalActiveEnergyPhase2Ignored = target.ignoreTotalActiveEnergyPhase2;
            
            IsTotalActiveEnergyReturnedPhase1Ignored = target.ignoreTotalActiveReturnedEnergyPhase1;
            IsTotalActiveEnergyReturnedPhase2Ignored = target.ignoreTotalActiveReturnedEnergyPhase2;
            
            RequestObject totalEnergyRequestObject = new("EM1Data.GetStatus")
            {
                MethodParams = new IdParam
                {
                    Id = 0
                }
            };
            
            totalEnergyRequestHandler = new WebSocketHandler(targetUrl, totalEnergyRequestObject, requestTimeoutTime);
        }
        
        if (target.RequiresAuthentication())
        {
            requestHandler.SetAuth(target.password);
            totalEnergyRequestHandler?.SetAuth(target.password);
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
        
        log.Debug("Starting regular metrics requests");

        string? requestResponse;
        TimeSpan requestTime;
        
        foreach (MeterReading meterReading in meterReadings)
        {
            requestHandler.UpdateRequestObject(o =>
            {
                IdParam idParam = (o.MethodParams as IdParam)!;
                idParam.Id = meterReading.meterIndex;
            });
            
            requestStopWatch.Start();
            requestResponse = await requestHandler.Request();
            requestStopWatch.Stop();
            
            requestTime = requestStopWatch.Elapsed;
            requestStopWatch.Reset();
            log.Debug("Regular metrics request for meter index {meterIndex} took: {requestTime} ms", meterReading.meterIndex, requestTime.TotalMilliseconds.ToString("F1", CultureInfo.InvariantCulture));
            
            if (string.IsNullOrEmpty(requestResponse))
            {
                log.Error("Request response null or empty - could not update metrics");
                return false;
            }

            if (!UpdateRegularMetrics(meterReading, requestResponse))
            {
                log.Error("Failed to update regular metrics");
                return false;
            }
        }

        log.Debug("Regular metrics requests ended");
        

        if (totalEnergyRequestHandler == null)
        {
            log.Debug("Updating metrics completed");
            lastSuccessfulRequest = DateTime.UtcNow;
            return true;
        }
        
        log.Debug("Starting total energy metrics requests");

        if (!IsTotalActiveEnergyPhase1Ignored || !IsTotalActiveEnergyReturnedPhase1Ignored)
        {
            totalEnergyRequestHandler.UpdateRequestObject(o =>
            {
                IdParam idParam = (o.MethodParams as IdParam)!;
                idParam.Id = 0;
            });
            
            requestStopWatch.Start();
            requestResponse = await totalEnergyRequestHandler.Request();
            requestStopWatch.Stop();
            
            requestTime = requestStopWatch.Elapsed;
            requestStopWatch.Reset();
        
            log.Debug("Total energy metrics request for meter index 0 took: {requestTime} ms", requestTime.TotalMilliseconds.ToString("F1", CultureInfo.InvariantCulture));
            
            if (string.IsNullOrEmpty(requestResponse))
            {
                log.Error("Total energy request response null or empty - could not update total energy metrics");
                return false;
            }

            if (!UpdateTotalEnergyMetrics(requestResponse, true))
            {
                log.Error("Failed to update total energy metrics");
                return false;
            }
        }
        
        if (!IsTotalActiveEnergyPhase2Ignored || !IsTotalActiveEnergyReturnedPhase2Ignored)
        {
            totalEnergyRequestHandler.UpdateRequestObject(o =>
            {
                IdParam idParam = (o.MethodParams as IdParam)!;
                idParam.Id = 1;
            });
                
            requestStopWatch.Start();
            requestResponse = await totalEnergyRequestHandler.Request();
            requestStopWatch.Stop();
                
            requestTime = requestStopWatch.Elapsed;
            requestStopWatch.Reset();
            
            log.Debug("Total energy metrics request for meter index 1 took: {requestTime} ms", requestTime.TotalMilliseconds.ToString("F1", CultureInfo.InvariantCulture));
            
            if (string.IsNullOrEmpty(requestResponse))
            {
                log.Error("Total energy request response null or empty - could not update total energy metrics");
                return false;
            }

            if (!UpdateTotalEnergyMetrics(requestResponse, false))
            {
                log.Error("Failed to update total energy metrics");
                return false;
            }
        }

        log.Debug("Total energy metrics requests ended");
        
        log.Debug("Updating metrics completed");
        lastSuccessfulRequest = DateTime.UtcNow;
        return true;
    }

    bool UpdateRegularMetrics(MeterReading meterReading, string requestResponse)
    {
        try
        {
            JsonDocument json = JsonDocument.Parse(requestResponse);

            JsonElement paramsElement = json.RootElement.GetProperty("result");
        
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
                meterReading.activePower = paramsElement.GetProperty("act_power").GetSingle();
            }
                
            if (!meterReading.apparentPowerIgnored)
            {
                meterReading.apparentPower = paramsElement.GetProperty("aprt_power").GetSingle();
            }
                
            if (!meterReading.powerFactorIgnored)
            {
                meterReading.powerFactor = paramsElement.GetProperty("pf").GetSingle();
            }

            return true;
        }
        catch (Exception exception)
        {
            log.Error(exception, "Failed to parse regular metrics response: {requestResponse}", requestResponse);
            return false;
        }
    }
    
    bool UpdateTotalEnergyMetrics(string requestResponse, bool phase1)
    {
        try
        {
            JsonDocument json = JsonDocument.Parse(requestResponse);

            JsonElement paramsElement = json.RootElement.GetProperty("result");

            if (phase1)
            {
                TotalActiveEnergyPhase1 = paramsElement.GetProperty("total_act_energy").GetSingle();
                TotalActiveEnergyReturnedPhase1 = paramsElement.GetProperty("total_act_ret_energy").GetSingle();
            }
            else
            {
                TotalActiveEnergyPhase2 = paramsElement.GetProperty("total_act_energy").GetSingle();
                TotalActiveEnergyReturnedPhase2 = paramsElement.GetProperty("total_act_ret_energy").GetSingle();
            }

            return true;
        }
        catch (Exception exception)
        {
            log.Error(exception, "Failed to parse total metrics response: {requestResponse}", requestResponse);
            return false;
        }
    }
}