using System.Globalization;

namespace Utilities.Metrics;

/// <summary>
/// A Gauge metric with Prometheus labels - represents a value that can change in time with additional metadata
/// </summary>
public class LabeledGaugeMetric : BaseMetric
{
    readonly Func<double> valueGetterFunction;
    readonly Func<string> labelsGetterFunction;

    /// <summary>
    /// Constructs a Labeled Gauge metric instance
    /// </summary>
    /// <param name="name">This will be the name under which Prometheus will see this metric</param>
    /// <param name="description">This will be seen as the help/description of the metric</param>
    /// <param name="valueGetterFunction">A function that returns the metric value</param>
    /// <param name="labelsGetterFunction">A function that returns the Prometheus labels (e.g., {device="name",type="temp"})</param>
    public LabeledGaugeMetric(string name, string description, Func<double> valueGetterFunction, Func<string> labelsGetterFunction) 
        : base(name, description, MetricType.Gauge)
    {
        this.valueGetterFunction = valueGetterFunction;
        this.labelsGetterFunction = labelsGetterFunction;
    }

    protected override string GetMetricString()
    {
        double value = valueGetterFunction();
        string labels = labelsGetterFunction();
        
        return GetName() + labels + " " + value.ToString("0.###", CultureInfo.InvariantCulture) + '\n';
    }
}
