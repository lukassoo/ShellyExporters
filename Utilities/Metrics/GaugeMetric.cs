namespace Utilities.Metrics;

/// <summary>
/// A Gauge metric - represents a value that can change in time
/// </summary>
public class GaugeMetric : BaseMetric
{
    Func<string> metricGetterFunction;

    /// <summary>
    /// Constructs a Gauge metric instance
    /// </summary>
    /// <param name="name">This will be the name under which Prometheus will see this metric</param>
    /// <param name="description">This will be seen as the help/description of the metric</param>
    /// <param name="metricGetterFunction">A function that returns the metric as a string - this will be called every time Prometheus requests/scrapes the metrics</param>
    public GaugeMetric(string name, string description, Func<string> metricGetterFunction) : base(name, description, MetricType.Gauge)
    {
        this.metricGetterFunction = metricGetterFunction;
    }

    protected override string GetMetricString()
    {
        return GetName() + " " + metricGetterFunction() + '\n';
    }
}