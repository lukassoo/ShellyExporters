﻿namespace Shelly3EmExporter;

public class TargetMeter
{
    public int index;
    public bool ignorePower;
    public bool ignoreVoltage;
    public bool ignoreCurrent;
    public bool ignorePowerFactor;
    public bool ignoreTotal;
    public bool ignoreTotalReturned;

    // Parameterless constructor for deserialization
    public TargetMeter() {}
    
    public TargetMeter(int index)
    {
        this.index = index;
    }
}