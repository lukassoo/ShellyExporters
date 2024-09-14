namespace Shelly3EmExporter;

public class MeterReading(int meterIndex, bool powerIgnored, bool currentIgnored, bool voltageIgnored, bool powerFactorIgnored, bool totalIgnored, bool totalReturnedIgnored)
{
    public readonly int meterIndex = meterIndex;
    public readonly bool powerIgnored = powerIgnored;
    public readonly bool currentIgnored = currentIgnored;
    public readonly bool voltageIgnored = voltageIgnored;
    public readonly bool powerFactorIgnored = powerFactorIgnored;
    public readonly bool totalIgnored = totalIgnored;
    public readonly bool totalReturnedIgnored = totalReturnedIgnored;
    
    public float power;
    public float current;
    public float voltage;
    public float powerFactor;
    public float total;
    public float totalReturned;
}