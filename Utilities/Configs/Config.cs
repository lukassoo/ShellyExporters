namespace Utilities.Configs;

public class Config<T>
{
    public bool logToFile = false;
    public string logLevel = "Information";
    public int listenPort = -1;
    
    /// When the project was created and for quite some time, the metric names were incorrect.
    /// They contained the target name in the metric name instead of as a label.
    /// Many users are running exporters with the old incorrect metric names.
    /// To avoid breaking their dashboards, this option is enabled by default on existing configurations when the exporter is updated.
    /// Only new users that generate a new config file will have this option disabled from the start.
    public bool useOldIncorrectMetricNames = true;
    
    public List<T> targets = new(1);
}