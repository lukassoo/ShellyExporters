using Prometheus;
using Serilog;
using Utilities.Networking;

namespace Utilities.Metrics;

public static class MetricsHelper
{
    static readonly ILogger log = Log.Logger.ForContext(typeof(MetricsHelper));
    
    public static IMetric CreateGauge(string metricName, string helpString, string targetName, Func<string> metricValueGetterFunction)
    {
        Gauge.Child gauge = Prometheus.Metrics.CreateGauge(metricName, helpString, ["targetName"]).WithLabels(targetName);
        
        return new GaugeMetric(gauge, metricValueGetterFunction);
    }
    
    public static async Task UpdateDeviceMetrics(Dictionary<IDeviceConnection, List<IMetric>> deviceMetricDictionary)
    {
        foreach ((IDeviceConnection deviceConnection, List<IMetric> metrics) in deviceMetricDictionary)
        {
            if (!await deviceConnection.UpdateMetricsIfNecessary())
            {
                log.Error("Failed to update metrics for target device: {targetName}", deviceConnection.GetTargetName());

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