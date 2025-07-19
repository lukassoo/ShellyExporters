namespace ShellyPro4PmExporter;

public class MeterReading
{
    public int meterIndex;
    public bool currentIgnored;
    public bool voltageIgnored;
    public bool activePowerIgnored;
    public bool powerFactorIgnored;
    public bool frequencyIgnored;
    public bool totalActiveEnergyIgnored;
    public bool totalReturnedActiveEnergyIgnored;
    public bool temperatureIgnored;
    public bool outputIgnored;

    public float current;
    public float voltage;
    public float activePower;
    public float powerFactor;
    public float frequency;
    public float totalActiveEnergy;
    public float totalReturnedActiveEnergy;
    public float temperature;
    public bool output;

    public MeterReading(TargetMeter targetMeter)
    {
        meterIndex = targetMeter.index;
        currentIgnored = targetMeter.ignoreCurrent;
        voltageIgnored = targetMeter.ignoreVoltage;
        activePowerIgnored = targetMeter.ignoreActivePower;
        powerFactorIgnored = targetMeter.ignorePowerFactor;
        frequencyIgnored = targetMeter.ignoreFrequency;
        totalActiveEnergyIgnored = targetMeter.ignoreActiveEnergy;
        totalReturnedActiveEnergyIgnored = targetMeter.ignoreReturnedActiveEnergy;
        temperatureIgnored = targetMeter.ignoreTemperature;
        outputIgnored = targetMeter.ignoreOutput;
    }
}