using Utilities;
using Utilities.Deserializers;
using Utilities.Networking.RequestHandling.WebSockets;

namespace ShellyPlusPmMiniExporter;

public class ShellyPlusPmMini : Device
{
    public bool IgnoreTotalEnergy { get; }
    public float TotalEnergy { get; private set; }
    
    public bool IgnoreTotalEnergyReturned { get; }
    public float TotalEnergyReturned { get; private set; }
    
    public bool IgnoreCurrentPower { get; }
    public float CurrentlyUsedPower { get; private set; }

    public bool IgnoreVoltage { get; }
    public float Voltage { get; private set; }

    public bool IgnoreCurrent { get; }
    public float Current { get; private set; }
        
    public bool IgnoreInputState { get; }
    public bool InputState { get; private set; }
    
    public bool IgnoreInputPercent { get; }
    public float InputPercent { get; private set; }
    
    public bool IgnoreInputCountTotal { get; }
    public int InputCountTotal { get; private set; }
    
    public bool IgnoreInputFrequency { get; }
    public float InputFrequency { get; private set; }
    
    public ShellyPlusPmMini(TargetDevice target)
    {
        TargetName = target.name;
        string targetUrl = target.url + (target.url.EndsWith('/') ? "" : "/") + "rpc";

        IgnoreTotalEnergy = target.ignoreTotalPowerMetric;
        IgnoreTotalEnergyReturned = target.ignoreTotalEnergyReturnedMetric;
        IgnoreCurrentPower = target.ignorePowerMetric;
        IgnoreVoltage = target.ignoreVoltageMetric;
        IgnoreCurrent = target.ignoreCurrentMetric;
        IgnoreInputState = target.ignoreInputState;
        IgnoreInputPercent = target.ignoreInputPercent;
        IgnoreInputCountTotal = target.ignoreInputCountTotal;
        IgnoreInputFrequency = target.ignoreInputFrequency;
        
        RequestObject requestObject = new("Shelly.GetStatus");
        TimeSpan requestTimeoutTime = TimeSpan.FromSeconds(target.requestTimeoutTime);
        
        WebSocketHandler requestHandler = new(targetUrl, requestObject, requestTimeoutTime);

        if (target.RequiresAuthentication())
        {
            requestHandler.SetAuth(target.password);
        }
        
        Gen2Deserializer gen2Deserializer = new();
        
        RegisterRequestHandlerAndDeserializer(requestHandler, gen2Deserializer);

        if (!IgnoreTotalEnergy)
        {
            gen2Deserializer.AddDeserializePowerMeterTotalEnergy(energy => TotalEnergy = energy, 0);
        }

        if (!IgnoreTotalEnergyReturned)
        {
            gen2Deserializer.AddDeserializePowerMeterTotalReturnedEnergy(energy => TotalEnergyReturned = energy, 0);
        }
        
        if (!IgnoreCurrentPower)
        {
            gen2Deserializer.AddDeserializeSwitchActivePower(power => CurrentlyUsedPower = power);
        }
        
        if (!IgnoreVoltage)
        {
            gen2Deserializer.AddDeserializeSwitchVoltage(voltage => Voltage = voltage);
        }

        if (!IgnoreCurrent)
        {
            gen2Deserializer.AddDeserializeSwitchCurrent(current => Current = current);
        }

        if (!IgnoreInputState)
        {
            gen2Deserializer.AddDeserializeInputState(inputState => InputState = inputState);
        }
            
        if (!IgnoreInputPercent)
        {
            gen2Deserializer.AddDeserializeInputPercent(inputPercent => InputPercent = inputPercent);
        }

        if (!IgnoreInputCountTotal)
        {
            gen2Deserializer.AddDeserializeInputCountTotal(inputCountTotal => InputCountTotal = inputCountTotal);
        }
            
        if (!IgnoreInputFrequency)
        {
            gen2Deserializer.AddDeserializeInputFrequency(inputFrequency => InputFrequency = inputFrequency);
        }
    }
}