namespace ShellyProEmExporter;

public class MeterReading(TargetMeter targetMeter)
{
    public readonly int meterIndex = targetMeter.index;
    public readonly bool currentIgnored = targetMeter.ignoreCurrent;
    public readonly bool voltageIgnored = targetMeter.ignoreVoltage;
    public readonly bool activePowerIgnored = targetMeter.ignoreActivePower;
    public readonly bool apparentPowerIgnored = targetMeter.ignoreApparentPower;
    public readonly bool powerFactorIgnored = targetMeter.ignorePowerFactor;
    
    public float current;
    public float voltage;
    public float activePower;
    public float apparentPower;
    public float powerFactor;
}