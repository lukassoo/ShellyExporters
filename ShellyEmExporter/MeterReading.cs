namespace ShellyEmExporter;

public class MeterReading(int meterIndex, bool powerIgnored, bool reactiveIgnored, bool voltageIgnored, bool powerFactorIgnored, bool totalIgnored, bool totalReturnedIgnored, bool currentComputed)
{
    public readonly int meterIndex = meterIndex;
    public readonly bool powerIgnored = powerIgnored;
    public readonly bool reactiveIgnored = reactiveIgnored;
    public readonly bool voltageIgnored = voltageIgnored;
    public readonly bool powerFactorIgnored = powerFactorIgnored;
    public readonly bool totalIgnored = totalIgnored;
    public readonly bool totalReturnedIgnored = totalReturnedIgnored;
    public readonly bool currentComputed = currentComputed;
    
    public float power;
    public float reactive;    
    public float voltage;
    public float powerFactor;
    public float total;
    public float totalReturned;
    public float current;
}