using Prometheus;
using Serilog;

namespace Utilities.Metrics;

public static class MetricsHelper
{
    static readonly ILogger log = Log.Logger.ForContext(typeof(MetricsHelper));

    public static IMetric CreateGauge(string metricName, string helpString, Func<string> metricValueGetterFunction)
    {
        Gauge.Child gauge = Prometheus.Metrics.CreateGauge(metricName, helpString).WithLabels([]);
        
        return new GaugeMetric(gauge, metricValueGetterFunction);
    }

    public static IMetric CreateCounter(string metricName, string helpString, Func<string> metricValueGetterFunction)
    {
        Counter.Child counter = Prometheus.Metrics.CreateCounter(metricName, helpString).WithLabels([]);
        
        return new CounterMetric(counter, metricValueGetterFunction);
    }
    
    public static IMetric CreateGauge(string metricName, string helpString, string targetName, Func<string> metricValueGetterFunction, string[]? additionalLabels = null, string[]? labelValues = null)
    {
        string[] finalLabels;
        
        if (additionalLabels != null)
        {
            finalLabels = ["targetName", ..additionalLabels];
        }
        else
        {
            finalLabels = ["targetName"];
        }
        
        string[] finalLabelValues;

        if (labelValues != null)
        {
            finalLabelValues = [targetName, ..labelValues];
        }
        else
        {
            finalLabelValues = [targetName];
        }
        
        if (finalLabels.Length != finalLabelValues.Length)
        {
            throw new ArgumentException("Label names and values must be of the same length");
        }
        
        Gauge.Child gauge = Prometheus.Metrics.CreateGauge(metricName, helpString, finalLabels).WithLabels(finalLabelValues);
        
        return new GaugeMetric(gauge, metricValueGetterFunction);
    }
    
    public static IMetric CreateGauge(string metricName, string helpString, string targetName, string deviceModel, Func<string> metricValueGetterFunction, string[]? additionalLabels = null, string[]? labelValues = null)
    {
        string[] finalLabels;
        
        if (additionalLabels != null)
        {
            finalLabels = ["targetName", "deviceModel", ..additionalLabels];
        }
        else
        {
            finalLabels = ["targetName", "deviceModel"];
        }
        
        string[] finalLabelValues;

        if (labelValues != null)
        {
            finalLabelValues = [targetName, deviceModel, ..labelValues];
        }
        else
        {
            finalLabelValues = [targetName, deviceModel];
        }
        
        if (finalLabels.Length != finalLabelValues.Length)
        {
            throw new ArgumentException("Label names and values must be of the same length");
        }
        
        Gauge.Child gauge = Prometheus.Metrics.CreateGauge(metricName, helpString, finalLabels).WithLabels(finalLabelValues);
        
        return new GaugeMetric(gauge, metricValueGetterFunction);
    }
    
    public static IMetric CreateCounter(string metricName, string helpString, string targetName, string deviceModel, Func<string> metricValueGetterFunction, string[]? additionalLabels = null, string[]? labelValues = null)
    {
        string[] finalLabels;
        
        if (additionalLabels != null)
        {
            finalLabels = ["targetName", "deviceModel", ..additionalLabels];
        }
        else
        {
            finalLabels = ["targetName", "deviceModel"];
        }
        
        string[] finalLabelValues;

        if (labelValues != null)
        {
            finalLabelValues = [targetName, deviceModel, ..labelValues];
        }
        else
        {
            finalLabelValues = [targetName, deviceModel];
        }
        
        if (finalLabels.Length != finalLabelValues.Length)
        {
            throw new ArgumentException("Label names and values must be of the same length");
        }
        
        Counter.Child counter = Prometheus.Metrics.CreateCounter(metricName, helpString, finalLabels).WithLabels(finalLabelValues);
        
        return new CounterMetric(counter, metricValueGetterFunction);
    }
    
    public static async Task UpdateDeviceMetrics(List<Device> devices)
    {
        foreach (Device device in devices)
        {
            if (!await device.UpdateMetrics())
            {
                if (device.ErrorIfMetricsUpdateFails)
                {
                    log.Error("Failed to update metrics for target device: {targetName}", device.TargetName);
                }
                
                continue;
            }

            foreach (IMetric metric in device.Metrics)
            {
                metric.Update();
            }
        }
    }
}