using Utilities;
using Utilities.Deserializers;
using Utilities.Networking.RequestHandling.WebSockets;

namespace ShellyPro3EmExporter;

public class ShellyPro3Em : Device
{
    readonly MeterReading[] meterReadings;

    public bool IsTotalActivePowerIgnored { get; }
    public float TotalActivePower { get; private set; }
    
    public bool IsTotalApparentPowerIgnored { get; }
    public float TotalApparentPower { get; private set; }

    public bool IsTotalCurrentIgnored { get; }
    public float TotalCurrent { get; private set; }
    
    public bool IsTotalActiveEnergyIgnored { get; }
    public float TotalActiveEnergy { get; private set; }
    
    public bool IsTotalActiveEnergyReturnedIgnored { get; }
    public float TotalActiveEnergyReturned { get; private set; }
    
    public bool IsTotalActiveEnergyPhase1Ignored { get; }
    public float TotalActiveEnergyPhase1 { get; private set; }
    
    public bool IsTotalActiveEnergyPhase2Ignored { get; }
    public float TotalActiveEnergyPhase2 { get; private set; }
    
    public bool IsTotalActiveEnergyPhase3Ignored { get; }
    public float TotalActiveEnergyPhase3 { get; private set; }
    
    public bool IsTotalActiveEnergyReturnedPhase1Ignored { get; }
    public float TotalActiveEnergyReturnedPhase1 { get; private set; }
    
    public bool IsTotalActiveEnergyReturnedPhase2Ignored { get; }
    public float TotalActiveEnergyReturnedPhase2 { get; private set; }
    
    public bool IsTotalActiveEnergyReturnedPhase3Ignored { get; }
    public float TotalActiveEnergyReturnedPhase3 { get; private set; }
    
    public ShellyPro3Em(TargetDevice target)
    {
        TargetName = target.name;
        string targetUrl = target.url + "/rpc";

        IsTotalCurrentIgnored = target.ignoreTotalCurrent;
        IsTotalActivePowerIgnored = target.ignoreTotalActivePower;
        IsTotalApparentPowerIgnored = target.ignoreTotalApparentPower;
        
        IsTotalActiveEnergyIgnored = target.ignoreTotalActiveEnergy;
        IsTotalActiveEnergyReturnedIgnored = target.ignoreTotalActiveReturnedEnergy;
            
        IsTotalActiveEnergyPhase1Ignored = target.ignoreTotalActiveEnergyPhase1;
        IsTotalActiveEnergyPhase2Ignored = target.ignoreTotalActiveEnergyPhase2;
        IsTotalActiveEnergyPhase3Ignored = target.ignoreTotalActiveEnergyPhase3;
            
        IsTotalActiveEnergyReturnedPhase1Ignored = target.ignoreTotalActiveReturnedEnergyPhase1;
        IsTotalActiveEnergyReturnedPhase2Ignored = target.ignoreTotalActiveReturnedEnergyPhase2;
        IsTotalActiveEnergyReturnedPhase3Ignored = target.ignoreTotalActiveReturnedEnergyPhase3;
        
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
                gen2Deserializer.AddDeserializeSwitchVoltage(voltage => meterReading.voltage = voltage, meterReading.meterIndex);
            }
            
            if (!meterReading.activePowerIgnored)
            {
                gen2Deserializer.AddDeserializeEnergyMeterActivePower(power => meterReading.activePower = power, meterReading.meterIndex);
            }

            if (!meterReading.apparentPowerIgnored)
            {
                gen2Deserializer.AddDeserializeEnergyMeterApparentPower(apparentPower => meterReading.apparentPower = apparentPower, meterReading.meterIndex);
            }
            
            if (!meterReading.powerFactorIgnored)
            {
                gen2Deserializer.AddDeserializeEnergyMeterPowerFactor(powerFactor => meterReading.powerFactor = powerFactor, meterReading.meterIndex);
            }
            
            if (!meterReading.currentIgnored)
            {
                gen2Deserializer.AddDeserializeEnergyMeterCurrent(current => meterReading.current = current, meterReading.meterIndex);
            }
            
            if (!meterReading.frequencyIgnored)
            {
                gen2Deserializer.AddDeserializeEnergyMeterFrequency(frequency => meterReading.frequency = frequency, meterReading.meterIndex);
            }
        }

        if (!IsTotalActiveEnergyPhase1Ignored)
        {
            gen2Deserializer.AddDeserializeEnergyMeterPhaseTotalActiveEnergy(energy => TotalActiveEnergyPhase1 = energy, 0);
        }

        if (!IsTotalActiveEnergyPhase2Ignored)
        {
            gen2Deserializer.AddDeserializeEnergyMeterPhaseTotalActiveEnergy(energy => TotalActiveEnergyPhase2 = energy, 1);
        }

        if (!IsTotalActiveEnergyPhase3Ignored)
        {
            gen2Deserializer.AddDeserializeEnergyMeterPhaseTotalActiveEnergy(energy => TotalActiveEnergyPhase3 = energy, 2);
        }

        if (!IsTotalActiveEnergyReturnedPhase1Ignored)
        {
            gen2Deserializer.AddDeserializeEnergyMeterPhaseTotalReturnedActiveEnergy(energy => TotalActiveEnergyReturnedPhase1 = energy, 0);
        }
        
        if (!IsTotalActiveEnergyReturnedPhase2Ignored)
        {
            gen2Deserializer.AddDeserializeEnergyMeterPhaseTotalReturnedActiveEnergy(energy => TotalActiveEnergyReturnedPhase2 = energy, 1);
        }

        if (!IsTotalActiveEnergyReturnedPhase3Ignored)
        {
            gen2Deserializer.AddDeserializeEnergyMeterPhaseTotalReturnedActiveEnergy(energy => TotalActiveEnergyReturnedPhase3 = energy, 2);
        }
            
        if (!IsTotalActivePowerIgnored)
        {
            gen2Deserializer.AddDeserializeEnergyMeterTotalActivePower(power => TotalActivePower = power);
        }
        
        if (!IsTotalApparentPowerIgnored)
        {
            gen2Deserializer.AddDeserializeEnergyMeterTotalApparentPower(power => TotalApparentPower = power);
        }
        
        if (!IsTotalCurrentIgnored)
        {
            gen2Deserializer.AddDeserializeEnergyMeterTotalCurrent(current => TotalCurrent = current);
        }
        
        if (!IsTotalActiveEnergyIgnored)
        {
            gen2Deserializer.AddDeserializeEnergyMeterTotalActiveEnergy(energy => TotalActiveEnergy = energy);
        }
        
        if (!IsTotalActiveEnergyReturnedIgnored)
        {
            gen2Deserializer.AddDeserializeEnergyMeterTotalReturnedActiveEnergy(energy => TotalActiveEnergyReturned = energy);
        }
    }

    public MeterReading[] GetMeterReadings()
    {
        return meterReadings;
    }
}