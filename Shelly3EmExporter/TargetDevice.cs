namespace Shelly3EmExporter;

public class TargetDevice
{
    public string name;
    public string url;
    public string username;
    public string password;
    public TargetMeter[] targetMeters = null!;
    
    public bool ignoreRelayStateMetric;
    
    // Parameterless constructor for deserialization
    public TargetDevice()
    {
        name = "";
        url = "";
        username = "";
        password = "";
    }
    
    public TargetDevice(string name, string url, string username, string password, TargetMeter[] targetMeters)
    {
        this.name = name;
        this.url = url;
        this.username = username;
        this.password = password;
        this.targetMeters = targetMeters;
    }
    
    public bool RequiresAuthentication()
    {
        return !string.IsNullOrEmpty(username) || !string.IsNullOrEmpty(password);
    }
}