namespace ShellyPro3EmExporter;

public class TargetDevice
{
    public string name;
    public string url;
    public string password;
    public TargetMeter[] targetMeters;
    
    public bool ignoreTotalCurrent;
    public bool ignoreTotalActivePower;
    public bool ignoreTotalApparentPower;

    public bool ignoreTotalActiveEnergy;
    public bool ignoreTotalActiveReturnedEnergy;
    
    public bool ignoreTotalActiveEnergyPhase1;
    public bool ignoreTotalActiveEnergyPhase2;
    public bool ignoreTotalActiveEnergyPhase3;
    public bool ignoreTotalActiveReturnedEnergyPhase1;
    public bool ignoreTotalActiveReturnedEnergyPhase2;
    public bool ignoreTotalActiveReturnedEnergyPhase3;
    
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
        return !ignoreTotalActiveEnergy || !ignoreTotalActiveReturnedEnergy;
    }
}