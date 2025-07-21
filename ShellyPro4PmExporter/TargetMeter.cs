namespace ShellyPro4PmExporter;

public class TargetMeter
{
    public int index;
    public bool ignoreVoltage;
    public bool ignoreCurrent;
    public bool ignoreActivePower;
    public bool ignorePowerFactor;
    public bool ignoreFrequency;
    public bool ignoreActiveEnergy;
    public bool ignoreReturnedActiveEnergy;
    public bool ignoreTemperature;
    public bool ignoreOutput;

    // Parameterless constructor for deserialization
    public TargetMeter() {}

    public TargetMeter(int index)
    {
        this.index = index;
    }
}