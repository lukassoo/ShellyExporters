﻿using System.Text.Json;
using Serilog;
using Utilities.Networking.RequestHandling.WebSockets;

namespace ShellyPlusPmMiniExporter;

public class ShellyPlusPmMiniConnection
{
    static readonly ILogger log = Log.ForContext<ShellyPlusPmMiniConnection>();
    
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
        
    public bool IgnoreInputState { get; }
    public bool InputState { get; private set; }
    
    public bool IgnoreInputPercent { get; }
    public float InputPercent { get; private set; }
    
    public bool IgnoreInputCountTotal { get; }
    public int InputCountTotal { get; private set; }
    
    public bool IgnoreInputFrequency { get; }
    public float InputFrequency { get; private set; }
    
    readonly WebSocketHandler pm1RequestHandler;
    readonly WebSocketHandler? inputRequestHandler;
    
    public ShellyPlusPmMiniConnection(TargetDevice target)
    {
        targetName = target.name;
        string targetUrl = target.url + (target.url.EndsWith('/') ? "" : "/") + "rpc";

        IgnoreTotalPower = target.ignoreTotalPowerMetric;
        IgnoreCurrentPower = target.ignorePowerMetric;
        IgnoreVoltage = target.ignoreVoltageMetric;
        IgnoreCurrent = target.ignoreCurrentMetric;

        RequestObject requestObject = new("PM1.GetStatus")
        {
            MethodParams = new IdParam
            {
                Id = 0
            }
        };
        
        TimeSpan requestTimeoutTime = TimeSpan.FromSeconds(target.requestTimeoutTime);
        
        pm1RequestHandler = new WebSocketHandler(targetUrl, requestObject, requestTimeoutTime);

        if (target.NeedsInputStatusRequests())
        {
            IgnoreInputState = target.ignoreInputState;
            IgnoreInputPercent = target.ignoreInputPercent;
            IgnoreInputCountTotal = target.ignoreInputCountTotal;
            IgnoreInputFrequency = target.ignoreInputFrequency;
            
            requestObject = new RequestObject("PM1.GetStatus")
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
            pm1RequestHandler.SetAuth(target.password);
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
        
        string? requestResponse = await pm1RequestHandler.Request();
        
        if (string.IsNullOrEmpty(requestResponse))
        {
            log.Error("Request response null or empty - could not update metrics");
            return false;
        }

        if (!UpdatePm1Metrics(requestResponse))
        {
            log.Error("Failed to update PM metrics");
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

    bool UpdatePm1Metrics(string requestResponse)
    {
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
        
            if (!IgnoreCurrent)
            {
                Current = resultElement.GetProperty("current").GetSingle();
            }

            // errors status ignored

            return true;
        }
        catch (Exception exception)
        {
            log.Error(exception, "Failed to parse PM metrics response, response:\n{response}", requestResponse);
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