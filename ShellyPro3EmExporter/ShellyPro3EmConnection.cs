using System.Diagnostics;
using System.Globalization;
using System.Text.Json;
using Serilog;
using Utilities.Networking.RequestHandling.WebSockets;

namespace ShellyPro3EmExporter;

public class ShellyPro3EmConnection
{
    static readonly ILogger log = Log.ForContext(typeof(ShellyPro3EmConnection));

    readonly string targetName;

    DateTime lastSuccessfulRequest = DateTime.MinValue;
    readonly Stopwatch requestStopWatch = new();

    // A minimum time between requests of 0.8s - the device updates the reading 1/s, it takes time to request the data and respond to Prometheus, a bit of delay will reduce load
    readonly TimeSpan minimumTimeBetweenRequests = TimeSpan.FromSeconds(0.8);

    readonly MeterReading[] meterReadings;
    static readonly string[] indexToPhaseMap = ["a", "b", "c"];

    public bool IsTotalActivePowerIgnored { get; }
    public float TotalActivePower { get; private set; }
    
    public bool IsTotalApparentPowerIgnored { get; }
    public float TotalApparentPower { get; private set; }

    public bool IsTotalCurrentIgnored { get; }
    public float TotalCurrent { get; private set; }
    
    public bool IsTotalActiveEnergyIgnored { get; }
    public float TotalActiveEnergy { get; private set; }
    
    public bool IsTotalActiveEnergyReturnedIgnored { get; }
    public float TotalActiveEnergyReturned { get; private set; }
    
    public bool IsTotalActiveEnergyPhase1Ignored { get; }
    public float TotalActiveEnergyPhase1 { get; private set; }
    
    public bool IsTotalActiveEnergyPhase2Ignored { get; }
    public float TotalActiveEnergyPhase2 { get; private set; }
    
    public bool IsTotalActiveEnergyPhase3Ignored { get; }
    public float TotalActiveEnergyPhase3 { get; private set; }
    
    public bool IsTotalActiveEnergyReturnedPhase1Ignored { get; }
    public float TotalActiveEnergyReturnedPhase1 { get; private set; }
    
    public bool IsTotalActiveEnergyReturnedPhase2Ignored { get; }
    public float TotalActiveEnergyReturnedPhase2 { get; private set; }
    
    public bool IsTotalActiveEnergyReturnedPhase3Ignored { get; }
    public float TotalActiveEnergyReturnedPhase3 { get; private set; }
    
    readonly WebSocketHandler requestHandler;
    readonly WebSocketHandler? totalEnergyRequestHandler;
    
    public ShellyPro3EmConnection(TargetDevice target)
    {
        targetName = target.name;
        string targetUrl = target.url + "/rpc";

        IsTotalCurrentIgnored = target.ignoreTotalCurrent;
        IsTotalActivePowerIgnored = target.ignoreTotalActivePower;
        IsTotalApparentPowerIgnored = target.ignoreTotalApparentPower;
        
        RequestObject requestObject = new("EM.GetStatus")
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

            IsTotalActiveEnergyIgnored = target.ignoreTotalActiveEnergy;
            IsTotalActiveEnergyReturnedIgnored = target.ignoreTotalActiveReturnedEnergy;
            
            IsTotalActiveEnergyPhase1Ignored = target.ignoreTotalActiveEnergyPhase1;
            IsTotalActiveEnergyPhase2Ignored = target.ignoreTotalActiveEnergyPhase2;
            IsTotalActiveEnergyPhase3Ignored = target.ignoreTotalActiveEnergyPhase3;
            
            IsTotalActiveEnergyReturnedPhase1Ignored = target.ignoreTotalActiveReturnedEnergyPhase1;
            IsTotalActiveEnergyReturnedPhase2Ignored = target.ignoreTotalActiveReturnedEnergyPhase2;
            IsTotalActiveEnergyReturnedPhase3Ignored = target.ignoreTotalActiveReturnedEnergyPhase3;
            
            RequestObject totalEnergyRequestObject = new("EMData.GetStatus")
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
        
        log.Debug("Starting regular metrics request");
        
        requestStopWatch.Start();
        string? requestResponse = await requestHandler.Request();
        requestStopWatch.Stop();

        log.Debug("Regular metrics request ended");
        
        TimeSpan requestTime = requestStopWatch.Elapsed;
        requestStopWatch.Reset();
        
        log.Debug("Regular metrics request took: {requestTime} ms", requestTime.TotalMilliseconds.ToString("F1", CultureInfo.InvariantCulture));
        
        if (string.IsNullOrEmpty(requestResponse))
        {
            log.Error("Request response null or empty - could not update metrics");
            return false;
        }

        if (!UpdateRegularMetrics(requestResponse))
        {
            log.Error("Failed to update regular metrics");
            return false;
        }

        if (totalEnergyRequestHandler == null)
        {
            log.Debug("Updating metrics completed");
            lastSuccessfulRequest = DateTime.UtcNow;
            return true;
        }
        
        log.Debug("Starting total energy metrics request");
        
        requestStopWatch.Start();
        requestResponse = await totalEnergyRequestHandler.Request();
        requestStopWatch.Stop();

        log.Debug("Total energy metrics request ended");
        
        requestTime = requestStopWatch.Elapsed;
        requestStopWatch.Reset();
        
        log.Debug("Total energy metrics request took: {requestTime} ms", requestTime.TotalMilliseconds.ToString("F1", CultureInfo.InvariantCulture));
        
        if (string.IsNullOrEmpty(requestResponse))
        {
            log.Error("Total energy request response null or empty - could not update total energy metrics");
            return false;
        }

        if (!UpdateTotalEnergyMetrics(requestResponse))
        {
            log.Error("Failed to update total energy metrics");
            return false;
        }
        
        log.Debug("Updating metrics completed");
        lastSuccessfulRequest = DateTime.UtcNow;
        return true;
    }

    bool UpdateRegularMetrics(string requestResponse)
    {
        try
        {
            JsonDocument json = JsonDocument.Parse(requestResponse);

            JsonElement paramsElement = json.RootElement.GetProperty("result");
        
            foreach (MeterReading meterReading in meterReadings)
            {
                string phase = indexToPhaseMap[meterReading.meterIndex];
                
                if (!meterReading.currentIgnored)
                {
                    meterReading.current = paramsElement.GetProperty(phase + "_current").GetSingle();
                }

                if (!meterReading.voltageIgnored)
                {
                    meterReading.voltage = paramsElement.GetProperty(phase + "_voltage").GetSingle();
                }

                if (!meterReading.activePowerIgnored)
                {
                    meterReading.activePower = paramsElement.GetProperty(phase + "_act_power").GetSingle();
                }
                
                if (!meterReading.apparentPowerIgnored)
                {
                    meterReading.apparentPower = paramsElement.GetProperty(phase + "_aprt_power").GetSingle();
                }
                
                if (!meterReading.powerFactorIgnored)
                {
                    meterReading.powerFactor = paramsElement.GetProperty(phase + "_pf").GetSingle();
                }
            }

            if (!IsTotalCurrentIgnored)
            {
                TotalCurrent = paramsElement.GetProperty("total_current").GetSingle();
            }
            
            if (!IsTotalActivePowerIgnored)
            {
                TotalActivePower = paramsElement.GetProperty("total_act_power").GetSingle();
            }
            
            if (!IsTotalApparentPowerIgnored)
            {
                TotalApparentPower = paramsElement.GetProperty("total_aprt_power").GetSingle();
            }

            return true;
        }
        catch (Exception exception)
        {
            log.Error(exception, "Failed to parse regular metrics response");
            return false;
        }
    }
    
    bool UpdateTotalEnergyMetrics(string requestResponse)
    {
        try
        {
            JsonDocument json = JsonDocument.Parse(requestResponse);

            JsonElement paramsElement = json.RootElement.GetProperty("result");
        
            if (!IsTotalActiveEnergyIgnored)
            {
                TotalActiveEnergy = paramsElement.GetProperty("total_act").GetSingle();
            }
            
            if (!IsTotalActiveEnergyReturnedIgnored)
            {
                TotalActiveEnergyReturned = paramsElement.GetProperty("total_act_ret").GetSingle();
            }
            
            if (!IsTotalActiveEnergyPhase1Ignored)
            {
                TotalActiveEnergyPhase1 = paramsElement.GetProperty("a_total_act_energy").GetSingle();
            }
            
            if (!IsTotalActiveEnergyPhase2Ignored)
            {
                TotalActiveEnergyPhase2 = paramsElement.GetProperty("b_total_act_energy").GetSingle();
            }
            
            if (!IsTotalActiveEnergyPhase3Ignored)
            {
                TotalActiveEnergyPhase3 = paramsElement.GetProperty("c_total_act_energy").GetSingle();
            }

            if (!IsTotalActiveEnergyReturnedPhase1Ignored)
            {
                TotalActiveEnergyReturnedPhase1 = paramsElement.GetProperty("a_total_act_ret_energy").GetSingle();
            }
            
            if (!IsTotalActiveEnergyReturnedPhase2Ignored)
            {
                TotalActiveEnergyReturnedPhase2 = paramsElement.GetProperty("b_total_act_ret_energy").GetSingle();
            }
            
            if (!IsTotalActiveEnergyReturnedPhase3Ignored)
            {
                TotalActiveEnergyReturnedPhase3 = paramsElement.GetProperty("c_total_act_ret_energy").GetSingle();
            }

            return true;
        }
        catch (Exception exception)
        {
            log.Error(exception, "Failed to parse total metrics response");
            return false;
        }
    }
}