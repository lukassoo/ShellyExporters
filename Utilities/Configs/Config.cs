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
    
    public Dictionary<string, string> additionalKeyValuePairs = new();
    
    public List<T> targets = new(1);
    
    public bool TryGetAdditionalValueString(string key, out string? value)
    {
        return additionalKeyValuePairs.TryGetValue(key, out value);
    }
    
    public bool TryGetAdditionalValueBool(string key, out bool value)
    {
        return bool.TryParse(additionalKeyValuePairs[key], out value);
    }
    
    public bool TryGetAdditionalValueInt(string key, out int value)
    {
        return int.TryParse(additionalKeyValuePairs[key], out value);
    }
}