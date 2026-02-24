using Utilities;
using Utilities.Deserializers;
using Utilities.Networking.RequestHandling;

namespace Shelly3EmExporter;

public class Shelly3Em : Device
{
    public bool IsRelayStateIgnored { get; }
    bool relayStatus;

    readonly MeterReading[] meterReadings;
    
    public Shelly3Em(TargetDevice target)
    {
        TargetName = target.name;
        string targetUrl = target.url + "/status";
        
        IsRelayStateIgnored = target.ignoreRelayStateMetric;
        
        HttpRequestHandler httpRequestHandler = new(targetUrl, target.RequiresAuthentication());
        
        if (target.RequiresAuthentication())
        {
            httpRequestHandler.SetAuth(target.username, target.password);
        }

        Gen1Deserializer gen1Deserializer = new();

        RegisterRequestHandlerAndDeserializer(httpRequestHandler, gen1Deserializer);
        
        int targetMeterCount = target.targetMeters.Length;
        meterReadings = new MeterReading[targetMeterCount];

        TargetMeter[] targetMeters = target.targetMeters;
        
        for (int i = 0; i < targetMeters.Length; i++)
        {
            MeterReading meterReading = new(
                targetMeters[i].index,
                targetMeters[i].ignorePower,
                targetMeters[i].ignoreCurrent,
                targetMeters[i].ignoreVoltage,
                targetMeters[i].ignorePowerFactor,
                targetMeters[i].ignoreTotal,
                targetMeters[i].ignoreTotalReturned);
            
            meterReadings[i] = meterReading;
            
            if (!meterReading.powerIgnored)
            {
                gen1Deserializer.AddDeserializeEMeterPower(power => meterReading.power = power, meterReading.meterIndex);
            }
            
            if (!meterReading.currentIgnored)
            {
                gen1Deserializer.AddDeserializeEMeterCurrent(current => meterReading.current = current, meterReading.meterIndex);
            }
            
            if (!meterReading.voltageIgnored)
            {
                gen1Deserializer.AddDeserializeEMeterVoltage(voltage => meterReading.voltage = voltage, meterReading.meterIndex);
            }
            
            if (!meterReading.powerFactorIgnored)
            {
                gen1Deserializer.AddDeserializeEMeterPowerFactor(powerFactor => meterReading.powerFactor = powerFactor, meterReading.meterIndex);
            }
            
            if (!meterReading.totalIgnored)
            {
                gen1Deserializer.AddDeserializeEMeterTotalEnergy(total => meterReading.total = total, meterReading.meterIndex);
            }
            
            if (!meterReading.totalReturnedIgnored)
            {
                gen1Deserializer.AddDeserializeEMeterTotalReturnedEnergy(totalReturned => meterReading.totalReturned = totalReturned, meterReading.meterIndex);
            }       
        }
        
        if (!IsRelayStateIgnored)
        {
            gen1Deserializer.AddDeserializeRelayState(relayState => relayStatus = relayState);
        }
    }
    
    public MeterReading[] GetCurrentMeterReadings()
    {
        return meterReadings;
    }
    
    public bool IsRelayOn()
    {
        return relayStatus;
    }
}