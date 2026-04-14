using System.Globalization;
using Prometheus;
using Serilog;

namespace Utilities.Metrics;

public class CounterMetric(Counter.Child counterWithLabel, Func<string> metricValueGetterFunction) : IMetric
{
    static readonly ILogger log = Log.ForContext<CounterMetric>();
    public bool IsPublished { get; private set; }
    
    public void Update()
    {
        string stringValue = metricValueGetterFunction();
        
        if (!double.TryParse(stringValue, CultureInfo.InvariantCulture, out double metricValue))
        {
            log.Error("Could not parse metric value");
            return;
        }
        
        counterWithLabel.IncTo(metricValue);
    }
    
    public void Unpublish()
    {
        counterWithLabel.Unpublish();
        
        IsPublished = false;
    }
    
    public void Publish()
    {
        counterWithLabel.Publish();
        
        IsPublished = true;
    }
}