using Utilities;
using Utilities.Deserializers;
using Utilities.Networking.RequestHandling;

namespace ShellyEmExporter;

public class ShellyEm : Device
{
    public bool IsRelayStateIgnored { get; }
    bool relayStatus;

    readonly MeterReading[] meterReadings;
    
    public ShellyEm(TargetDevice target)
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
                targetMeters[i].ignoreReactive,
                targetMeters[i].ignoreVoltage,
                targetMeters[i].ignorePowerFactor,
                targetMeters[i].ignoreTotal,
                targetMeters[i].ignoreTotalReturned,
                targetMeters[i].computeCurrent);
            
            meterReadings[i] = meterReading;
            
            if (!targetMeters[i].ignorePower)
            {
                gen1Deserializer.AddDeserializeEMeterPower(power => meterReading.power = power, meterReading.meterIndex);
            }
            
            if (!targetMeters[i].ignoreReactive)
            {
                gen1Deserializer.AddDeserializeEMeterReactivePower(reactive => meterReading.current = reactive, meterReading.meterIndex);
            }
            
            if (!targetMeters[i].ignoreVoltage)
            {
                gen1Deserializer.AddDeserializeEMeterVoltage(voltage => meterReading.voltage = voltage, meterReading.meterIndex);
            }
            
            if (!targetMeters[i].ignorePowerFactor)
            {
                gen1Deserializer.AddDeserializeEMeterPowerFactor(powerFactor => meterReading.powerFactor = powerFactor, meterReading.meterIndex);
            }
            
            if (!targetMeters[i].ignoreTotal)
            {
                gen1Deserializer.AddDeserializeEMeterTotalEnergy(total => meterReading.total = total, meterReading.meterIndex);
            }
            
            if (!targetMeters[i].ignoreTotalReturned)
            {
                gen1Deserializer.AddDeserializeEMeterTotalReturnedEnergy(totalReturned => meterReading.totalReturned = totalReturned, meterReading.meterIndex);
            }
            
            if (!targetMeters[i].computeCurrent)
            {
                gen1Deserializer.AddDeserializedEMeterComputedCurrent(current => meterReading.current = current, meterReading.meterIndex);
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