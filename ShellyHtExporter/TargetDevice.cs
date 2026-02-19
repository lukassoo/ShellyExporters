namespace ShellyHtExporter;

public class TargetDevice
{
    public string name;
    public bool onlyAcceptFromAddress;
    public string address;
    
    // Parameterless constructor for deserialization
    public TargetDevice()
    {
        name = "";
        address = "";
    }
    
    public TargetDevice(string name, bool onlyAcceptFromAddress, string address)
    {
        this.name = name;
        this.onlyAcceptFromAddress = onlyAcceptFromAddress;
        this.address = address;
    }
}