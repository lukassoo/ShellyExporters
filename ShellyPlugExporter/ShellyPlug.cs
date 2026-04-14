using Utilities;
using Utilities.Deserializers;
using Utilities.Networking.RequestHandling;

namespace ShellyPlugExporter;

public class ShellyPlug : Device
{
    public bool IgnoreRelayState { get; }
    public bool RelayStatus { get; private set; }

    public bool IgnoreCurrentPower { get; }
    public float CurrentlyUsedPower { get; private set; }

    public bool IgnoreTemperature { get; }
    public float Temperature { get; private set; }
    
    public ShellyPlug(TargetDevice target)
    {
        TargetName = target.name;
        string targetUrl1 = target.url + "/status";

        IgnoreCurrentPower = target.ignorePowerMetric;
        IgnoreTemperature = target.ignoreTemperatureMetric;
        IgnoreRelayState = target.ignoreRelayStateMetric;

        HttpRequestHandler httpRequestHandler = new(targetUrl1, target.RequiresAuthentication());
        
        if (target.RequiresAuthentication())
        {
            httpRequestHandler.SetAuth(target.username, target.password);
        }
        
        Gen1Deserializer gen1Deserializer = new();
        
        RegisterRequestHandlerAndDeserializer(httpRequestHandler, gen1Deserializer);

        if (!IgnoreCurrentPower)
        {
            gen1Deserializer.AddDeserializePower(power => CurrentlyUsedPower = power, 0);
        }

        if (!IgnoreTemperature)
        {
            gen1Deserializer.AddDeserializeTemperature(temperature => Temperature = temperature);
        }

        if (!IgnoreRelayState)
        {
            gen1Deserializer.AddDeserializeRelayState(relayState => RelayStatus = relayState);
        }
    }
}