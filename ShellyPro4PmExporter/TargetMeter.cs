namespace ShellyPro3EmExporter;

public class TargetMeter
{
    public int index;
    public bool ignoreVoltage;
    public bool ignoreCurrent;
    public bool ignoreActivePower;
    public bool ignoreApparentPower;
    public bool ignorePowerFactor;

    // Parameterless constructor for deserialization
    public TargetMeter() {}

    public TargetMeter(int index)
    {
        this.index = index;
    }
}