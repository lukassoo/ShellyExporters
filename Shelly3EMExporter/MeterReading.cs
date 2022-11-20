namespace Shelly3EMExporter;

public class MeterReading
{
    public int meterIndex;
    public bool powerIgnored;
    public bool currentIgnored;
    public bool voltageIgnored;
    public bool powerFactorIgnored;
    
    public float power;
    public float current;
    public float voltage;
    public float powerFactor;

    public MeterReading(int meterIndex, bool powerIgnored, bool currentIgnored, bool voltageIgnored, bool powerFactorIgnored)
    {
        this.meterIndex = meterIndex;
        this.powerIgnored = powerIgnored;
        this.currentIgnored = currentIgnored;
        this.voltageIgnored = voltageIgnored;
        this.powerFactorIgnored = powerFactorIgnored;
    }
}