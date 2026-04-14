using Utilities;
using Utilities.Deserializers;
using Utilities.Networking.RequestHandling.WebSockets;

namespace ShellyProEmExporter;

public class ShellyProEm : Device
{
    readonly MeterReading[] meterReadings;
    
    public bool IsTotalActiveEnergyPhase1Ignored { get; }
    public float TotalActiveEnergyPhase1 { get; private set; }
    
    public bool IsTotalActiveEnergyPhase2Ignored { get; }
    public float TotalActiveEnergyPhase2 { get; private set; }
    
    public bool IsTotalActiveEnergyReturnedPhase1Ignored { get; }
    public float TotalActiveEnergyReturnedPhase1 { get; private set; }
    
    public bool IsTotalActiveEnergyReturnedPhase2Ignored { get; }
    public float TotalActiveEnergyReturnedPhase2 { get; private set; }
    
    public ShellyProEm(TargetDevice target)
    {
        TargetName = target.name;
        string targetUrl = target.url + "/rpc";
        
        IsTotalActiveEnergyPhase1Ignored = target.ignoreTotalActiveEnergyPhase1;
        IsTotalActiveEnergyPhase2Ignored = target.ignoreTotalActiveEnergyPhase2;
            
        IsTotalActiveEnergyReturnedPhase1Ignored = target.ignoreTotalActiveReturnedEnergyPhase1;
        IsTotalActiveEnergyReturnedPhase2Ignored = target.ignoreTotalActiveReturnedEnergyPhase2;
        
        RequestObject requestObject = new("Shelly.GetStatus");
        TimeSpan requestTimeoutTime = TimeSpan.FromSeconds(target.requestTimeoutTime);
        
        WebSocketHandler requestHandler = new(targetUrl, requestObject, requestTimeoutTime);
        
        if (target.RequiresAuthentication())
        {
            requestHandler.SetAuth(target.password);
        }
        
        Gen2Deserializer gen2Deserializer = new();
        
        RegisterRequestHandlerAndDeserializer(requestHandler, gen2Deserializer);

        int targetMeterCount = target.targetMeters.Length;
        meterReadings = new MeterReading[targetMeterCount];

        TargetMeter[] targetMeters = target.targetMeters;
        
        for (int i = 0; i < targetMeters.Length; i++)
        {
            MeterReading meterReading = new(targetMeters[i]);
            meterReadings[i] = meterReading;

            if (!meterReading.voltageIgnored)
            {
                gen2Deserializer.AddDeserializeEnergyMeterVoltage_EM1(voltage => meterReading.voltage = voltage, meterReading.meterIndex);
            }

            if (!meterReading.currentIgnored)
            {
                gen2Deserializer.AddDeserializeEnergyMeterCurrent_EM1(current => meterReading.current = current, meterReading.meterIndex);
            }
            
            if (!meterReading.activePowerIgnored)
            {
                gen2Deserializer.AddDeserializeEnergyMeterActivePower_EM1(power => meterReading.activePower = power, meterReading.meterIndex);
            }
            
            if (!meterReading.apparentPowerIgnored)
            {
                gen2Deserializer.AddDeserializeEnergyMeterApparentPower_EM1(apparentPower => meterReading.apparentPower = apparentPower, meterReading.meterIndex);
            }
            
            if (!meterReading.powerFactorIgnored)
            {
                gen2Deserializer.AddDeserializeEnergyMeterPowerFactor_EM1(powerFactor => meterReading.powerFactor = powerFactor, meterReading.meterIndex);
            }

            if (!meterReading.frequencyIgnored)
            {
                gen2Deserializer.AddDeserializeEnergyMeterFrequency_EM1(frequency => meterReading.frequency = frequency, meterReading.meterIndex);
            }
        }

        if (!IsTotalActiveEnergyPhase1Ignored)
        {
            gen2Deserializer.AddDeserializeEnergyMeterPhaseTotalActiveEnergy_EM1(energy => TotalActiveEnergyPhase1 = energy, 0);
        }
        
        if (!IsTotalActiveEnergyPhase2Ignored)
        {
            gen2Deserializer.AddDeserializeEnergyMeterPhaseTotalActiveEnergy_EM1(energy => TotalActiveEnergyPhase2 = energy, 1);
        }
        
        if (!IsTotalActiveEnergyReturnedPhase1Ignored)
        {
            gen2Deserializer.AddDeserializeEnergyMeterPhaseTotalReturnedActiveEnergy_EM1(energy => TotalActiveEnergyReturnedPhase1 = energy, 0);
        }
        
        if (!IsTotalActiveEnergyReturnedPhase2Ignored)
        {
            gen2Deserializer.AddDeserializeEnergyMeterPhaseTotalReturnedActiveEnergy_EM1(energy => TotalActiveEnergyReturnedPhase2 = energy, 1);
        }
    }
    
    public MeterReading[] GetCurrentMeterReadings()
    {
        return meterReadings;
    }
}