namespace Utilities.Metrics;

/// <summary>
/// A very simple abstract base class for all metrics
/// <br/> Provides a single public <see cref="GetMetric()"/> method that gets the metric string ready for Prometheus
/// </summary>
public abstract class BaseMetric
{
    string name;
    string description;
    string type;

    string helpLine;
    string typeLine;

    public BaseMetric(string name, string description, string type)
    {
        this.name = name;
        this.description = description;
        this.type = type;

        helpLine = GetHelpLine();
        typeLine = GetTypeLine();
    }

    public async Task<string> GetMetric()
    {
        string metric = helpLine + typeLine + await GetMetricString();

        if (!metric.EndsWith(Environment.NewLine))
        {
            metric += Environment.NewLine;
        }
        
        return metric;
    }

    /// <summary>
    /// Gets the metric name
    /// </summary>
    protected string GetName()
    {
        return name;
    }

    /// <summary>
    /// Gets the metric string - should contain the metric name and value
    /// <br/> This is the thing that Prometheus will see when it requests the metrics
    /// </summary>
    protected abstract Task<string> GetMetricString();

    // Gets the help line - Prometheus uses this for metric help/description
    string GetHelpLine()
    {
        return "# HELP " + name + " " + description + '\n';
    }

    // Gets the type line - Prometheus uses this to know what type of metric this is (Gauge, Counter, etc.)
    string GetTypeLine()
    {
        return "# TYPE " + name + " " + type + '\n';
    }
}

