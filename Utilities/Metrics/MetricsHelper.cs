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
        
    public static async Task UpdateDeviceMetrics(Dictionary<Device, List<IMetric>> deviceMetricDictionary)
    {
        foreach ((Device device, List<IMetric> metrics) in deviceMetricDictionary)
        {
            if (!await device.UpdateMetricsIfNecessary())
            {
                log.Error("Failed to update metrics for target device: {targetName}", device.TargetName);

                foreach (IMetric metric in metrics)
                {
                    if (metric.IsPublished)
                    {
                        metric.Unpublish();
                    }
                }
                
                continue;
            }
            
            foreach (IMetric metric in metrics)
            {
                metric.Update();
                
                if (!metric.IsPublished)
                {
                    metric.Publish();
                }
            }
        }
    }
}