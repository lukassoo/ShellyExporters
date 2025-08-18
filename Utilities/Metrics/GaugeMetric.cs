using System.Globalization;
using Prometheus;
using Serilog;

namespace Utilities.Metrics;

public class GaugeMetric(Gauge.Child gaugeWithLabel, Func<string> metricValueGetterFunction) : IMetric
{
    static readonly ILogger log = Log.ForContext<GaugeMetric>();

    public bool IsPublished { get; private set; }

    public void Update()
    {
        string stringValue = metricValueGetterFunction();
        
        if (!double.TryParse(stringValue, CultureInfo.InvariantCulture, out double metricValue))
        {
            log.Error("Could not parse metric value");
            return;
        }
        
        gaugeWithLabel.Set(metricValue);
    }
    
    public void Unpublish()
    {
        gaugeWithLabel.Unpublish();
        
        IsPublished = false;
    }
    
    public void Publish()
    {
        gaugeWithLabel.Publish();
        
        IsPublished = true;
    }
}