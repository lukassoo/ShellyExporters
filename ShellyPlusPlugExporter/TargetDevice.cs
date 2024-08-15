namespace ShellyPlusPlugExporter;

public class TargetDevice
{
    public string name;
    public string url;
    public string password;
    
    public float requestTimeoutTime = 3;
    
    public bool ignorePowerMetric;
    public bool ignoreVoltageMetric;
    public bool ignoreCurrentMetric;
    public bool ignoreTemperatureMetric;
    public bool ignoreRelayStateMetric;

    // Parameterless constructor for deserialization
    public TargetDevice()
    {
        name = "";
        url = "";
        password = "";
    }

    public TargetDevice(string name, string url, string password)
    {
        this.name = name;
        this.url = url;
        this.password = password;
    }

    public bool RequiresAuthentication()
    {
        return !string.IsNullOrEmpty(password);
    }
}
