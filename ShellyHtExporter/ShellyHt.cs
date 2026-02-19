using Utilities;
using Utilities.Metrics;

namespace ShellyHtExporter;

public class ShellyHt : Device
{
    public bool OnlyAcceptFromAddress { get; }
    public string Address { get; }

    public DateTimeOffset LastUpdate
    {
        get;
        set
        {
            field = value;
            timeSinceUpdateMetric?.Publish();
        }
    } = DateTimeOffset.MinValue;

    public float Temperature
    {
        get;
        set
        {
            field = value;
            temperatureMetric?.Publish();
        }
    }

    public float Humidity
    {
        get;
        set
        {
            field = value;
            humidityMetric?.Publish();
        }
    }

    IMetric? temperatureMetric;
    IMetric? humidityMetric;
    IMetric? timeSinceUpdateMetric;
    
    public ShellyHt(TargetDevice target)
    {
        TargetName = target.name;
        ErrorIfMetricsUpdateFails = false;
        
        OnlyAcceptFromAddress = target.onlyAcceptFromAddress;
        Address = target.address;
        
        SetupMetrics();
        UnpublishMetrics();
    }

    public override Task<bool> UpdateMetrics()
    {
        if (LastUpdate == DateTimeOffset.MinValue)
        {
            return Task.FromResult(false);
        }
        
        foreach (IMetric metric in Metrics)
        {
            if (metric.IsPublished)
            {
                metric.Update();
            }
        }
        
        return Task.FromResult(true);
    }
    
    void SetupMetrics()
    {
        const string deviceModel = "HT";

        temperatureMetric = PredefinedMetrics.CreateTemperatureMetric(TargetName, deviceModel, () => Temperature);
        Metrics.Add(temperatureMetric);
        
        humidityMetric = PredefinedMetrics.CreateHumidityMetric(TargetName, deviceModel, () => Humidity);
        Metrics.Add(humidityMetric);
        
        timeSinceUpdateMetric = PredefinedMetrics.CreateTimeSinceUpdateMetric(TargetName, deviceModel, () => (float)(DateTimeOffset.UtcNow - LastUpdate).TotalSeconds);
        Metrics.Add(timeSinceUpdateMetric);
    }
}