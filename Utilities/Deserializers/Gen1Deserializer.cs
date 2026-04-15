using System.Text.Json;
using Serilog;
using Utilities.Logging;

namespace Utilities.Deserializers;

public class Gen1Deserializer : IDeserializer
{
    static readonly ILogger log = Log.ForContext<Gen1Deserializer>();

    readonly List<Action<JsonDocument>> deserializationCallbacks = [];

    public JsonElement EMetersElement { get; private set; }
    bool cacheEMeters;
    
    public bool Deserialize(string jsonString)
    {
        try
        {
            using JsonDocument jsonDocument = JsonDocument.Parse(jsonString);

            if (cacheEMeters)
            {
                EMetersElement = jsonDocument.RootElement.GetProperty("emeters");
            }

            try
            {
                foreach (Action<JsonDocument> callback in deserializationCallbacks)
                {
                    callback.Invoke(jsonDocument);
                }
            }
            catch (Exception exception)
            {
                log.Error(exception, "Exception during deserialization callbacks");
                
                LogAdditions.CheckExceptionLogIndexError(exception, log);
                return false;
            }
            
            return true;
        }
        catch (Exception exception)
        {
            log.Error(exception, "Exception during deserialization:");
            return false;
        }
    }

    public void AddDeserializeTemperature(Action<float> callback)
    {
        deserializationCallbacks.Add(document =>
        {
            float temperature = document.RootElement.GetProperty("temperature").GetSingle();
            callback.Invoke(temperature);
        });
    }
    
    public void AddDeserializePower(Action<float> callback, int meterIndex)
    {
        deserializationCallbacks.Add(document =>
        {
            float power = document.RootElement.GetProperty("meters")[meterIndex].GetProperty("power").GetSingle();
            callback.Invoke(power);
        });
    }
    
    public void AddDeserializeTotalEnergy(Action<float> callback, int meterIndex)
    {
        deserializationCallbacks.Add(document =>
        {
            float totalEnergy = document.RootElement.GetProperty("meters")[meterIndex].GetProperty("total").GetSingle();
            callback.Invoke(totalEnergy);
        });
    }
    
    public void AddDeserializeRelayState(Action<bool> callback, int relayIndex = 0)
    {
        deserializationCallbacks.Add(document =>
        {
            bool relayState = document.RootElement.GetProperty("relays")[relayIndex].GetProperty("ison").GetBoolean();
            callback.Invoke(relayState);
        });
    }
    
    public void AddDeserializeEMeterPower(Action<float> callback, int meterIndex)
    {
        cacheEMeters = true;
        
        deserializationCallbacks.Add(_ =>
        {
            JsonElement targetMeterNode = EMetersElement[meterIndex];
            
            float power = targetMeterNode.GetProperty("power").GetSingle();
            callback.Invoke(power);
        });
    }
    
    public void AddDeserializeEMeterReactivePower(Action<float> callback, int meterIndex)
    {
        cacheEMeters = true;
        
        deserializationCallbacks.Add(_ =>
        {
            JsonElement targetMeterNode = EMetersElement[meterIndex];
            
            float reactivePower = targetMeterNode.GetProperty("reactive").GetSingle();
            callback.Invoke(reactivePower);
        });
    }

    public void AddDeserializeEMeterVoltage(Action<float> callback, int meterIndex)
    {
        cacheEMeters = true;
        
        deserializationCallbacks.Add(_ =>
        {
            JsonElement targetMeterNode = EMetersElement[meterIndex];
            
            float voltage = targetMeterNode.GetProperty("voltage").GetSingle();
            callback.Invoke(voltage);
        });
    }

    public void AddDeserializeEMeterCurrent(Action<float> callback, int meterIndex)
    {
        cacheEMeters = true;
        
        deserializationCallbacks.Add(_ =>
        {
            JsonElement targetMeterNode = EMetersElement[meterIndex];
            
            float voltage = targetMeterNode.GetProperty("current").GetSingle();
            callback.Invoke(voltage);
        });
    }
    
    public void AddDeserializeEMeterPowerFactor(Action<float> callback, int meterIndex)
    {
        cacheEMeters = true;
        
        deserializationCallbacks.Add(_ =>
        {
            JsonElement targetMeterNode = EMetersElement[meterIndex];
            
            float powerFactor = targetMeterNode.GetProperty("pf").GetSingle();
            callback.Invoke(powerFactor);
        });
    }

    public void AddDeserializeEMeterTotalEnergy(Action<float> callback, int meterIndex)
    {
        cacheEMeters = true;
        
        deserializationCallbacks.Add(_ =>
        {
            JsonElement targetMeterNode = EMetersElement[meterIndex];
            
            float totalEnergy = targetMeterNode.GetProperty("total").GetSingle();
            callback.Invoke(totalEnergy);
        });
    }
    
    public void AddDeserializeEMeterTotalReturnedEnergy(Action<float> callback, int meterIndex)
    {
        cacheEMeters = true;
        
        deserializationCallbacks.Add(_ =>
        {
            JsonElement targetMeterNode = EMetersElement[meterIndex];
            
            float totalReturnedEnergy = targetMeterNode.GetProperty("total_returned").GetSingle();
            callback.Invoke(totalReturnedEnergy);
        });
    }
    
    public void AddDeserializedEMeterComputedCurrent(Action<float> callback, int meterIndex)
    {
        cacheEMeters = true;
        
        deserializationCallbacks.Add(_ =>
        {
            JsonElement targetMeterNode = EMetersElement[meterIndex];
            
            float current = targetMeterNode.GetProperty("power").GetSingle() / targetMeterNode.GetProperty("voltage").GetSingle();
            callback.Invoke(current);
        });
    }
}