namespace ShellyEmExporter;

public class TargetMeter
{
    public int index;
    public bool ignorePower;
    public bool ignoreVoltage;
    public bool ignoreReactive;
    public bool ignorePowerFactor;
    public bool ignoreTotal;
    public bool ignoreTotalReturned;
    public bool computeCurrent = true;

    // Parameterless constructor for deserialization
    public TargetMeter() {}
    
    public TargetMeter(int index)
    {
        this.index = index;
    }
}