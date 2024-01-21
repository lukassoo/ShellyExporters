namespace Shelly3EmExporter;

public class MeterReading
{
    public int meterIndex;
    public bool powerIgnored;
    public bool currentIgnored;
    public bool voltageIgnored;
    public bool powerFactorIgnored;
    public bool totalIgnored;
    public bool totalReturnedIgnored;
    
    public float power;
    public float current;
    public float voltage;
    public float powerFactor;
    public float total;
    public float totalReturned;

    public MeterReading(int meterIndex, bool powerIgnored, bool currentIgnored, bool voltageIgnored, bool powerFactorIgnored, bool totalIgnored, bool totalReturnedIgnored)
    {
        this.meterIndex = meterIndex;
        this.powerIgnored = powerIgnored;
        this.currentIgnored = currentIgnored;
        this.voltageIgnored = voltageIgnored;
        this.powerFactorIgnored = powerFactorIgnored;
        this.totalIgnored = totalIgnored;
        this.totalReturnedIgnored = totalReturnedIgnored;
    }
}