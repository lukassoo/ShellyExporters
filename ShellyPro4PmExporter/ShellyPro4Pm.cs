using Utilities;
using Utilities.Deserializers;
using Utilities.Networking.RequestHandling.WebSockets;

namespace ShellyPro4PmExporter;

public class ShellyPro4Pm : Device
{
    readonly MeterReading[] meterReadings;
    
    public ShellyPro4Pm(TargetDevice target)
    {
        TargetName = target.name;
        string targetUrl = target.url + "/rpc";

        TimeSpan requestTimeoutTime = TimeSpan.FromSeconds(target.requestTimeoutTime);

        RequestObject requestObject = new("Shelly.GetStatus");

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
            
            if (!targetMeters[i].ignoreCurrent)
            {
                gen2Deserializer.AddDeserializeSwitchCurrent(current => meterReading.current = current, meterReading.meterIndex);
            }
            
            if (!targetMeters[i].ignoreVoltage)
            {
                gen2Deserializer.AddDeserializeSwitchVoltage(voltage => meterReading.voltage = voltage, meterReading.meterIndex);
            }
            
            if (!targetMeters[i].ignoreActivePower)
            {
                gen2Deserializer.AddDeserializeSwitchActivePower(power => meterReading.activePower = power, meterReading.meterIndex);
            }
            
            if (!targetMeters[i].ignorePowerFactor)
            {
                gen2Deserializer.AddDeserializeSwitchPowerFactor(powerFactor => meterReading.powerFactor = powerFactor, meterReading.meterIndex);
            }
            
            if (!targetMeters[i].ignoreFrequency)
            {
                gen2Deserializer.AddDeserializeSwitchFrequency(frequency => meterReading.frequency = frequency, meterReading.meterIndex);
            }
            
            if (!targetMeters[i].ignoreTotalActiveEnergy)
            {
                gen2Deserializer.AddDeserializeSwitchTotalEnergy(energy => meterReading.totalActiveEnergy = energy, meterReading.meterIndex);
            }
            
            if (!targetMeters[i].ignoreTotalReturnedActiveEnergy)
            {
                gen2Deserializer.AddDeserializeSwitchTotalReturnedEnergy(energy => meterReading.totalReturnedActiveEnergy = energy, meterReading.meterIndex);
            }
            
            if (!targetMeters[i].ignoreTemperature)
            {
                gen2Deserializer.AddDeserializeSwitchTemperature(temperature => meterReading.temperature = temperature, meterReading.meterIndex);
            }
            
            if (!targetMeters[i].ignoreOutput)
            {
                gen2Deserializer.AddDeserializeSwitchOutputState(output => meterReading.output = output, meterReading.meterIndex);
            }
        }
    }

    public MeterReading[] GetCurrentMeterReadings()
    {
        return meterReadings;
    }
}