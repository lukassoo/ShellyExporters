namespace ShellyProEmExporter;

public class TargetDevice
{
    public string name;
    public string url;
    public string password;
    public float requestTimeoutTime = 3;
    
    public TargetMeter[] targetMeters;
    
    public bool ignoreTotalActiveEnergyPhase1;
    public bool ignoreTotalActiveEnergyPhase2;
    public bool ignoreTotalActiveReturnedEnergyPhase1;
    public bool ignoreTotalActiveReturnedEnergyPhase2;
    
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

    public bool NeedsTotalEnergyRequests()
    {
        return !ignoreTotalActiveEnergyPhase1 || !ignoreTotalActiveEnergyPhase2 || !ignoreTotalActiveReturnedEnergyPhase1 || !ignoreTotalActiveReturnedEnergyPhase2; 
    }
}