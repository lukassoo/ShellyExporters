namespace ShellyPlugExporter;

public class TargetDevice
{
    public string name;
    public string url;
    public string username;
    public string password;
    public bool ignorePowerMetric;
    public bool ignoreTemperatureMetric;
    public bool ignoreRelayStateMetric;

    // Parameterless constructor for deserialization
    public TargetDevice()
    {
        name = "";
        url = "";
        username = "";
        password = "";
    }

    public TargetDevice(string name, string url, string username, string password)
    {
        this.name = name;
        this.url = url;
        this.username = username;
        this.password = password;
    }

    public bool RequiresAuthentication()
    {
        return !(string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password));
    }
}
