namespace ShellyPro4PmExporter;

public class TargetDevice
{
    public string name;
    public string url;
    public string password;
    public float requestTimeoutTime = 3;

    public TargetMeter[] targetMeters;
    
    public bool ignoreTotalActiveEnergy;
    public bool ignoreTotalActiveReturnedEnergy;
    
    // Parameterless constructor for deserialization
    public TargetDevice()
    {
        name = "";
        url = "";
        password = "";
        targetMeters = [];
    }

    public TargetDevice(string name, string url, string password, TargetMeter[] targetMeters)
    {
        this.name = name;
        this.url = url;
        this.password = password;
        this.targetMeters = targetMeters;
    }

    public bool RequiresAuthentication()
    {
        return !string.IsNullOrEmpty(password);
    }
}