namespace ShellyPro3EmExporter;

public class MeterReading
{
    public int meterIndex;
    public bool currentIgnored;
    public bool voltageIgnored;
    public bool activePowerIgnored;
    public bool apparentPowerIgnored;
    public bool powerFactorIgnored;
    
    public float current;
    public float voltage;
    public float activePower;
    public float apparentPower;
    public float powerFactor;

    public MeterReading(TargetMeter targetMeter)
    {
        meterIndex = targetMeter.index;
        currentIgnored = targetMeter.ignoreCurrent;
        voltageIgnored = targetMeter.ignoreVoltage;
        activePowerIgnored = targetMeter.ignoreActivePower;
        apparentPowerIgnored = targetMeter.ignoreApparentPower;
        powerFactorIgnored = targetMeter.ignorePowerFactor;
    }
}