using Utilities;
using Utilities.Deserializers;
using Utilities.Networking.RequestHandling.WebSockets;

namespace ShellyPlus1PmExporter;

public class ShellyPlus1Pm : Device
{
    public bool IgnoreTotalEnergy { get; }
    public float TotalEnergy { get; private set; }
    
    public bool IgnoreCurrentPower { get; }
    public float CurrentlyUsedPower { get; private set; }

    public bool IgnoreVoltage { get; }
    public float Voltage { get; private set; }

    public bool IgnoreCurrent { get; }
    public float Current { get; private set; }
    
    public bool IgnoreOutputState { get; }
    public bool OutputState { get; private set; }

    public bool IgnoreTemperature { get; }
    public float Temperature { get; private set; }
    
    public bool IgnoreInputState { get; }
    public bool InputState { get; private set; }
    
    public bool IgnoreInputPercent { get; }
    public float InputPercent { get; private set; }
    
    public bool IgnoreInputCountTotal { get; }
    public int InputCountTotal { get; private set; }
    
    public bool IgnoreInputFrequency { get; }
    public float InputFrequency { get; private set; }
    
    public ShellyPlus1Pm(TargetDevice target)
    {
        TargetName = target.name;
        string targetUrl = target.url + "/rpc";

        IgnoreTotalEnergy = target.ignoreTotalPowerMetric;
        IgnoreCurrentPower = target.ignorePowerMetric;
        IgnoreVoltage = target.ignoreVoltageMetric;
        IgnoreCurrent = target.ignoreCurrentMetric;
        IgnoreTemperature = target.ignoreTemperatureMetric;
        IgnoreOutputState = target.ignoreOutputStateMetric;
        IgnoreInputState = target.ignoreInputState;
        IgnoreInputPercent = target.ignoreInputPercent;
        IgnoreInputCountTotal = target.ignoreInputCountTotal;
        IgnoreInputFrequency = target.ignoreInputFrequency;
        
        RequestObject requestObject = new("Shelly.GetStatus");
        TimeSpan requestTimeoutTime = TimeSpan.FromSeconds(target.requestTimeoutTime);
        
        WebSocketHandler switchRequestHandler = new(targetUrl, requestObject, requestTimeoutTime);

        if (target.RequiresAuthentication())
        {
            switchRequestHandler.SetAuth(target.password);
        }
        
        Gen2Deserializer gen2Deserializer = new();
        
        RegisterRequestHandlerAndDeserializer(switchRequestHandler, gen2Deserializer);

        if (!IgnoreTotalEnergy)
        {
            gen2Deserializer.AddDeserializeSwitchTotalEnergy(energy => TotalEnergy = energy);
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
        
        if (!IgnoreTemperature)
        {
            gen2Deserializer.AddDeserializeSwitchTemperature(temperature => Temperature = temperature);
        }

        if (!IgnoreOutputState)
        {
            gen2Deserializer.AddDeserializeSwitchOutputState(relayState => OutputState = relayState);
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