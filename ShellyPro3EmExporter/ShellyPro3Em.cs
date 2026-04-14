using Serilog;
using Utilities;
using Utilities.Deserializers;
using Utilities.Networking.RequestHandling.WebSockets;

namespace ShellyPro3EmExporter;

public class ShellyPro3Em : Device
{
    static readonly ILogger log = Log.ForContext<ShellyPro3Em>();
    
    readonly MeterReading[] meterReadings;

    public bool TriphaseMode { get; private set; }
    
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

        TriphaseMode = target.triphaseMode;
        
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
                if (TriphaseMode)
                {
                    gen2Deserializer.AddDeserializeEnergyMeterVoltage_EM(voltage => meterReading.voltage = voltage, meterReading.meterIndex);
                }
                else
                {
                    gen2Deserializer.AddDeserializeEnergyMeterVoltage_EM1(voltage => meterReading.voltage = voltage, meterReading.meterIndex);
                }
            }
            
            if (!meterReading.activePowerIgnored)
            {
                if (TriphaseMode)
                {
                    gen2Deserializer.AddDeserializeEnergyMeterActivePower_EM(power => meterReading.activePower = power, meterReading.meterIndex);
                }
                else
                {
                    gen2Deserializer.AddDeserializeEnergyMeterActivePower_EM1(power => meterReading.activePower = power, meterReading.meterIndex);
                }
            }

            if (!meterReading.apparentPowerIgnored)
            {
                if (TriphaseMode)
                {
                    gen2Deserializer.AddDeserializeEnergyMeterApparentPower_EM(apparentPower => meterReading.apparentPower = apparentPower, meterReading.meterIndex);
                }
                else
                {
                    gen2Deserializer.AddDeserializeEnergyMeterApparentPower_EM1(apparentPower => meterReading.apparentPower = apparentPower, meterReading.meterIndex);
                }
            }
            
            if (!meterReading.powerFactorIgnored)
            {
                if (TriphaseMode)
                {
                    gen2Deserializer.AddDeserializeEnergyMeterPowerFactor_EM(powerFactor => meterReading.powerFactor = powerFactor, meterReading.meterIndex);
                }
                else
                {
                    gen2Deserializer.AddDeserializeEnergyMeterPowerFactor_EM1(powerFactor => meterReading.powerFactor = powerFactor, meterReading.meterIndex);
                }
            }
            
            if (!meterReading.currentIgnored)
            {
                if (TriphaseMode)
                {
                    gen2Deserializer.AddDeserializeEnergyMeterCurrent_EM(current => meterReading.current = current, meterReading.meterIndex);
                }
                else
                {
                    gen2Deserializer.AddDeserializeEnergyMeterCurrent_EM1(current => meterReading.current = current, meterReading.meterIndex);
                }
            }
            
            if (!meterReading.frequencyIgnored)
            {
                if (TriphaseMode)
                {
                    gen2Deserializer.AddDeserializeEnergyMeterFrequency_EM(frequency => meterReading.frequency = frequency, meterReading.meterIndex);
                }
                else
                {
                    gen2Deserializer.AddDeserializeEnergyMeterFrequency_EM1(frequency => meterReading.frequency = frequency, meterReading.meterIndex);
                }
            }
        }

        if (!IsTotalActiveEnergyPhase1Ignored)
        {
            if (TriphaseMode)
            {
                gen2Deserializer.AddDeserializeEnergyMeterPhaseTotalActiveEnergy_EM(energy => TotalActiveEnergyPhase1 = energy, 0);
            }
            else
            {
                gen2Deserializer.AddDeserializeEnergyMeterPhaseTotalActiveEnergy_EM1(energy => TotalActiveEnergyPhase1 = energy, 0);
            }
        }

        if (!IsTotalActiveEnergyPhase2Ignored)
        {
            if (TriphaseMode)
            {
                gen2Deserializer.AddDeserializeEnergyMeterPhaseTotalActiveEnergy_EM(energy => TotalActiveEnergyPhase2 = energy, 1);
            }
            else
            {
                gen2Deserializer.AddDeserializeEnergyMeterPhaseTotalActiveEnergy_EM1(energy => TotalActiveEnergyPhase2 = energy, 1);
            }
        }

        if (!IsTotalActiveEnergyPhase3Ignored)
        {
            if (TriphaseMode)
            {
                gen2Deserializer.AddDeserializeEnergyMeterPhaseTotalActiveEnergy_EM(energy => TotalActiveEnergyPhase3 = energy, 2);
            }
            else
            {
                gen2Deserializer.AddDeserializeEnergyMeterPhaseTotalActiveEnergy_EM1(energy => TotalActiveEnergyPhase3 = energy, 2);
            }
        }

        if (!IsTotalActiveEnergyReturnedPhase1Ignored)
        {
            if (TriphaseMode)
            {
                gen2Deserializer.AddDeserializeEnergyMeterPhaseTotalReturnedActiveEnergy_EM(energy => TotalActiveEnergyReturnedPhase1 = energy, 0);
            }
            else
            {
                gen2Deserializer.AddDeserializeEnergyMeterPhaseTotalReturnedActiveEnergy_EM1(energy => TotalActiveEnergyReturnedPhase1 = energy, 0);
            }
        }
        
        if (!IsTotalActiveEnergyReturnedPhase2Ignored)
        {
            if (TriphaseMode)
            {
                gen2Deserializer.AddDeserializeEnergyMeterPhaseTotalReturnedActiveEnergy_EM(energy => TotalActiveEnergyReturnedPhase2 = energy, 1);
            }
            else
            {
                gen2Deserializer.AddDeserializeEnergyMeterPhaseTotalReturnedActiveEnergy_EM1(energy => TotalActiveEnergyReturnedPhase2 = energy, 1);
            }
        }

        if (!IsTotalActiveEnergyReturnedPhase3Ignored)
        {
            if (TriphaseMode)
            {
                gen2Deserializer.AddDeserializeEnergyMeterPhaseTotalReturnedActiveEnergy_EM(energy => TotalActiveEnergyReturnedPhase3 = energy, 2);
            }
            else
            {
                gen2Deserializer.AddDeserializeEnergyMeterPhaseTotalReturnedActiveEnergy_EM1(energy => TotalActiveEnergyReturnedPhase3 = energy, 2);
            }
        }
            
        if (!IsTotalActivePowerIgnored)
        {
            if (TriphaseMode)
            {
                gen2Deserializer.AddDeserializeEnergyMeterTotalActivePower_EM(power => TotalActivePower = power);
            }
            else
            {
                // Not exposed as a sum in this mode 
            }
        }
        
        if (!IsTotalApparentPowerIgnored)
        {
            if (TriphaseMode)
            {
                gen2Deserializer.AddDeserializeEnergyMeterTotalApparentPower_EM(power => TotalApparentPower = power);
            }
            else
            {
                // Not exposed as a sum in this mode
            }
        }
        
        if (!IsTotalCurrentIgnored)
        {
            if (TriphaseMode)
            {
                gen2Deserializer.AddDeserializeEnergyMeterTotalCurrent_EM(current => TotalCurrent = current);
            }
            else
            {
                // Not exposed as a sum in this mode
            }
        }
    }

    public override async Task<bool> UpdateMetrics()
    {
        if (!await base.UpdateMetrics())
        {
            if (ErrorIfMetricsUpdateFails)
            {
                log.Error("Failed to update metrics, are you sure the device profile matches the configuration? (Config: triphaseMode: {triphaseMode})", TriphaseMode);
            }
            
            return false;
        }
        
        // Manually calculate energy totals.
        // This way they can stay exposed regardless of the mode.
        // These should actually not be exposed directly since they can be calculated from the per-phase values.
        // But for backwards compatibility they stay.
        TotalActiveEnergy = TotalActiveEnergyPhase1 + TotalActiveEnergyPhase2 + TotalActiveEnergyPhase3;
        TotalActiveEnergyReturned = TotalActiveEnergyReturnedPhase1 + TotalActiveEnergyReturnedPhase2 + TotalActiveEnergyReturnedPhase3;
        
        return true;
    }

    public MeterReading[] GetMeterReadings()
    {
        return meterReadings;
    }
}