namespace ShellyPro4PmExporter;

public class MeterReading
{
    public int meterIndex;
    public bool currentIgnored;
    public bool voltageIgnored;
    public bool activePowerIgnored;
    public bool apparentPowerIgnored;
    public bool powerFactorIgnored;
    public bool frequencyIgnored;
    public bool activeEnergyIgnored;
    public bool returnedActiveEnergyIgnored;
    public bool temperatureIgnored;
    public bool outputIgnored;

    public float current;
    public float voltage;
    public float activePower;
    public float apparentPower;
    public float powerFactor;
    public float frequency;
    public float activeEnergy;
    public float returnedActiveEnergy;
    public float temperature;
    public bool output;

    public MeterReading(TargetMeter targetMeter)
    {
        meterIndex = targetMeter.index;
        currentIgnored = targetMeter.ignoreCurrent;
        voltageIgnored = targetMeter.ignoreVoltage;
        activePowerIgnored = targetMeter.ignoreActivePower;
        apparentPowerIgnored = targetMeter.ignoreApparentPower;
        powerFactorIgnored = targetMeter.ignorePowerFactor;
        frequencyIgnored = targetMeter.ignoreFrequency;
        activeEnergyIgnored = targetMeter.ignoreActiveEnergy;
        returnedActiveEnergyIgnored = targetMeter.ignoreReturnedActiveEnergy;
        temperatureIgnored = targetMeter.ignoreTemperature;
        outputIgnored = targetMeter.ignoreOutput;
    }
}