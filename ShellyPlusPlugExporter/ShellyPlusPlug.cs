using Utilities;
using Utilities.Deserializers;
using Utilities.Networking.RequestHandling.WebSockets;

namespace ShellyPlusPlugExporter;

public class ShellyPlusPlug : Device
{
    public bool IgnoreTotalEnergy { get; }
    public float TotalEnergy { get; private set; }

    public bool IgnoreCurrentPower { get; }
    public float CurrentlyUsedPower { get; private set; }

    public bool IgnoreVoltage { get; }
    public float Voltage { get; private set; }

    public bool IgnoreCurrent { get; }
    public float Current { get; private set; }

    public bool IgnoreRelayState { get; }
    public bool RelayStatus { get; private set; }

    public bool IgnoreTemperature { get; }
    public float Temperature { get; private set; }
    
    public ShellyPlusPlug(TargetDevice target)
    {
        TargetName = target.name;
        string targetUrl = target.url + "/rpc";

        IgnoreTotalEnergy = target.ignoreTotalPowerMetric;
        IgnoreCurrentPower = target.ignorePowerMetric;
        IgnoreVoltage = target.ignoreVoltageMetric;
        IgnoreCurrent = target.ignoreCurrentMetric;
        IgnoreTemperature = target.ignoreTemperatureMetric;
        IgnoreRelayState = target.ignoreRelayStateMetric;

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
            gen2Deserializer.AddDeserializeSwitchTotalEnergy(power => TotalEnergy = power);
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
        
        if (!IgnoreRelayState)
        {
            gen2Deserializer.AddDeserializeSwitchOutputState(outputState => RelayStatus = outputState);
        }
    }
}