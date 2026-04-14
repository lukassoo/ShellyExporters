using System.Text.Json;
using Serilog;

namespace Utilities.Deserializers;

public class Gen2Deserializer : IDeserializer
{
    static readonly ILogger log = Log.ForContext<Gen2Deserializer>();

    readonly List<Action<JsonElement>> deserializationCallbacks = [];
    
    public bool Deserialize(string jsonString)
    {
        try
        {
            using JsonDocument jsonDocument = JsonDocument.Parse(jsonString);
            JsonElement resultElement = jsonDocument.RootElement.GetProperty("result");
            
            foreach (Action<JsonElement> callback in deserializationCallbacks)
            {
                callback.Invoke(resultElement);
            }
            
            return true;
        }
        catch (Exception exception)
        {
            log.Error(exception, "Exception during deserialization:");
            return false;
        }
    }
    
    // Switches
    
    public void AddDeserializeSwitchTemperature(Action<float> callback, int switchIndex = 0)
    {
        deserializationCallbacks.Add(resultElement =>
        {
            float temperature = resultElement.GetProperty($"switch:{switchIndex}").GetProperty("aenergy").GetProperty("total").GetSingle();
            callback.Invoke(temperature);
        });
    }

    public void AddDeserializeSwitchVoltage(Action<float> callback, int switchIndex = 0)
    {
        deserializationCallbacks.Add(resultElement =>
        {
            float voltage = resultElement.GetProperty($"switch:{switchIndex}").GetProperty("voltage").GetSingle();
            callback.Invoke(voltage);
        });
    }
    
    public void AddDeserializeSwitchCurrent(Action<float> callback, int switchIndex = 0)
    {
        deserializationCallbacks.Add(resultElement =>
        {
            float current = resultElement.GetProperty($"switch:{switchIndex}").GetProperty("current").GetSingle();
            callback.Invoke(current);
        });
    }

    public void AddDeserializeSwitchPowerFactor(Action<float> callback, int switchIndex = 0)
    {
        deserializationCallbacks.Add(resultElement =>
        {
            float powerFactor = resultElement.GetProperty($"switch:{switchIndex}").GetProperty("pf").GetSingle();
            callback.Invoke(powerFactor);
        });
    }

    public void AddDeserializeSwitchFrequency(Action<float> callback, int switchIndex = 0)
    {
        deserializationCallbacks.Add(resultElement =>
        {
            float frequency = resultElement.GetProperty($"switch:{switchIndex}").GetProperty("freq").GetSingle();
            callback.Invoke(frequency);
        });
    }
    
    public void AddDeserializeSwitchActivePower(Action<float> callback, int switchIndex = 0)
    {
        deserializationCallbacks.Add(resultElement =>
        {
            float power = resultElement.GetProperty($"switch:{switchIndex}").GetProperty("apower").GetSingle();
            callback.Invoke(power);
        });
    }
    
    public void AddDeserializeSwitchTotalEnergy(Action<float> callback, int switchIndex = 0)
    {
        deserializationCallbacks.Add(resultElement =>
        {
            float energy = resultElement.GetProperty($"switch:{switchIndex}").GetProperty("aenergy").GetProperty("total").GetSingle();
            callback.Invoke(energy);
        });
    }
    
    public void AddDeserializeSwitchTotalReturnedEnergy(Action<float> callback, int switchIndex = 0)
    {
        deserializationCallbacks.Add(resultElement =>
        {
            float energy = resultElement.GetProperty($"switch:{switchIndex}").GetProperty("ret_aenergy").GetProperty("total").GetSingle();
            callback.Invoke(energy);
        });
    }
    
    public void AddDeserializeSwitchOutputState(Action<bool> callback, int switchIndex = 0)
    {
        deserializationCallbacks.Add(resultElement =>
        {
            bool outputState = resultElement.GetProperty($"switch:{switchIndex}").GetProperty("output").GetBoolean();
            callback.Invoke(outputState);
        });
    }
    
    // Inputs

    public void AddDeserializeInputState(Action<bool> callback, int inputIndex = 0)
    {
        deserializationCallbacks.Add(resultElement =>
        {
            bool inputState;

            if (resultElement.TryGetProperty($"input:{inputIndex}", out JsonElement inputProperty) &&
                inputProperty.TryGetProperty("state", out JsonElement stateProperty))
            {
                inputState = stateProperty.GetBoolean();
            }
            else
            {
                inputState = false;
            }
            
            callback.Invoke(inputState);
        });
    }
    
    public void AddDeserializeInputPercent(Action<float> callback, int inputIndex = 0)
    {
        deserializationCallbacks.Add(resultElement =>
        {
            float inputPercent;

            if (resultElement.TryGetProperty($"input:{inputIndex}", out JsonElement inputProperty) &&
                inputProperty.TryGetProperty("percent", out JsonElement percentProperty))
            {
                inputPercent = percentProperty.GetSingle();
            }
            else
            {
                inputPercent = 0;
            }
            
            callback.Invoke(inputPercent);
        });
    }
    
    public void AddDeserializeInputCountTotal(Action<int> callback, int inputIndex = 0)
    {
        deserializationCallbacks.Add(resultElement =>
        {
            int inputCountTotal;
            
            if (resultElement.TryGetProperty($"input:{inputIndex}", out JsonElement inputProperty) &&
                inputProperty.TryGetProperty("counts", out JsonElement countsProperty) &&
                countsProperty.TryGetProperty("total", out JsonElement totalProperty))
            {
                inputCountTotal = totalProperty.GetInt32();
            }
            else
            {
                inputCountTotal = 0;
            }
            
            callback.Invoke(inputCountTotal);
        });
    }
    
    public void AddDeserializeInputFrequency(Action<float> callback, int inputIndex = 0)
    {
        deserializationCallbacks.Add(resultElement =>
        {
            float inputFrequency;
            
            if (resultElement.TryGetProperty($"input:{inputIndex}", out JsonElement inputProperty) &&
                inputProperty.TryGetProperty("freq", out JsonElement frequencyProperty))
            {
                inputFrequency = frequencyProperty.GetSingle();
            }
            else
            {
                inputFrequency = 0;
            }
            
            callback.Invoke(inputFrequency);
        });
    }
    
    // Power meter
    
    public void AddDeserializePowerMeterVoltage(Action<float> callback, int meterIndex)
    {
        deserializationCallbacks.Add(resultElement =>
        {
            float voltage = resultElement.GetProperty($"pm1:{meterIndex}").GetProperty("voltage").GetSingle();
            callback.Invoke(voltage);
        });
    }
    
    public void AddDeserializePowerMeterCurrent(Action<float> callback, int meterIndex)
    {
        deserializationCallbacks.Add(resultElement =>
        {
            float current = resultElement.GetProperty($"pm1:{meterIndex}").GetProperty("current").GetSingle();
            callback.Invoke(current);
        });
    }
    
    public void AddDeserializePowerMeterActivePower(Action<float> callback, int meterIndex)
    {
        deserializationCallbacks.Add(resultElement =>
        {
            float power = resultElement.GetProperty($"pm1:{meterIndex}").GetProperty("apower").GetSingle();
            callback.Invoke(power);
        });
    }
    
    public void AddDeserializePowerMeterPowerFactor(Action<float> callback, int meterIndex)
    {
        deserializationCallbacks.Add(resultElement =>
        {
            float powerFactor = resultElement.GetProperty($"pm1:{meterIndex}").GetProperty("pf").GetSingle();
            callback.Invoke(powerFactor);
        });
    }
    
    public void AddDeserializePowerMeterFrequency(Action<float> callback, int meterIndex)
    {
        deserializationCallbacks.Add(resultElement =>
        {
            float frequency = resultElement.GetProperty($"pm1:{meterIndex}").GetProperty("freq").GetSingle();
            callback.Invoke(frequency);
        });
    }
    
    public void AddDeserializePowerMeterApparentPower(Action<float> callback, int meterIndex)
    {
        deserializationCallbacks.Add(resultElement =>
        {
            float apparentPower = resultElement.GetProperty($"pm1:{meterIndex}").GetProperty("aprtpower").GetSingle();
            callback.Invoke(apparentPower);
        });
    }
    
    public void AddDeserializePowerMeterTotalEnergy(Action<float> callback, int meterIndex)
    {
        deserializationCallbacks.Add(resultElement =>
        {
            float energy = resultElement.GetProperty($"pm1:{meterIndex}").GetProperty("aenergy").GetProperty("total").GetSingle();
            callback.Invoke(energy);
        });
    }
    
    public void AddDeserializePowerMeterTotalReturnedEnergy(Action<float> callback, int meterIndex)
    {
        deserializationCallbacks.Add(resultElement =>
        {
            float energy = resultElement.GetProperty($"pm1:{meterIndex}").GetProperty("ret_aenergy").GetProperty("total").GetSingle();
            callback.Invoke(energy);
        });
    }
    
    // Energy meter (EM1)
    
    public void AddDeserializeEnergyMeterVoltage_EM1(Action<float> callback, int meterIndex)
    {
        deserializationCallbacks.Add(resultElement =>
        {
            float voltage = resultElement.GetProperty($"em1:{meterIndex}").GetProperty("voltage").GetSingle();
            callback.Invoke(voltage);
        });
    }
    
    public void AddDeserializeEnergyMeterCurrent_EM1(Action<float> callback, int meterIndex)
    {
        deserializationCallbacks.Add(resultElement =>
        {
            float current = resultElement.GetProperty($"em1:{meterIndex}").GetProperty("current").GetSingle();
            callback.Invoke(current);
        });
    }
    
    public void AddDeserializeEnergyMeterActivePower_EM1(Action<float> callback, int meterIndex)
    {
        deserializationCallbacks.Add(resultElement =>
        {
            float power = resultElement.GetProperty($"em1:{meterIndex}").GetProperty("act_power").GetSingle();
            callback.Invoke(power);
        });
    }
    
    public void AddDeserializeEnergyMeterApparentPower_EM1(Action<float> callback, int meterIndex)
    {
        deserializationCallbacks.Add(resultElement =>
        {
            float apparentPower = resultElement.GetProperty($"em1:{meterIndex}").GetProperty("aprt_power").GetSingle();
            callback.Invoke(apparentPower);
        });
    }
    
    public void AddDeserializeEnergyMeterPowerFactor_EM1(Action<float> callback, int meterIndex)
    {
        deserializationCallbacks.Add(resultElement =>
        {
            float powerFactor = resultElement.GetProperty($"em1:{meterIndex}").GetProperty("pf").GetSingle();
            callback.Invoke(powerFactor);
        });
    }
    
    public void AddDeserializeEnergyMeterFrequency_EM1(Action<float> callback, int meterIndex)
    {
        deserializationCallbacks.Add(resultElement =>
        {
            float frequency = resultElement.GetProperty($"em1:{meterIndex}").GetProperty("freq").GetSingle();
            callback.Invoke(frequency);
        });
    }
    
    public void AddDeserializeEnergyMeterPhaseTotalActiveEnergy_EM1(Action<float> callback, int meterIndex)
    {
        deserializationCallbacks.Add(resultElement =>
        {
            float energy = resultElement.GetProperty($"em1data:{meterIndex}").GetProperty("total_act_energy").GetSingle();
            callback.Invoke(energy);
        });
    }
    
    public void AddDeserializeEnergyMeterPhaseTotalReturnedActiveEnergy_EM1(Action<float> callback, int meterIndex)
    {
        deserializationCallbacks.Add(resultElement =>
        {
            float energy = resultElement.GetProperty($"em1data:{meterIndex}").GetProperty("total_act_ret_energy").GetSingle();
            callback.Invoke(energy);
        });
    }
    
    public void AddDeserializeEnergyMeterTotalActiveEnergy_EM1(Action<float> callback, int meterIndex)
    {
        deserializationCallbacks.Add(resultElement =>
        {
            float energy = resultElement.GetProperty($"em1data:{meterIndex}").GetProperty("total_act_energy").GetSingle();
            callback.Invoke(energy);
        });
    }
    
    public void AddDeserializeEnergyMeterTotalReturnedActiveEnergy_EM1(Action<float> callback, int meterIndex)
    {
        deserializationCallbacks.Add(resultElement =>
        {
            float energy = resultElement.GetProperty($"em1data:{meterIndex}").GetProperty("total_act_ret_energy").GetSingle();
            callback.Invoke(energy);
        });
    }

    
    
    // Energy meter (EM)

    static readonly Dictionary<int, string> indexToPhaseMap = new()
    {
        { 0, "a" },
        { 1, "b" },
        { 2, "c" }
    };
    
    public void AddDeserializeEnergyMeterVoltage_EM(Action<float> callback, int meterIndex)
    {
        string phase = indexToPhaseMap[meterIndex];
        
        deserializationCallbacks.Add(resultElement =>
        {
            float voltage = resultElement.GetProperty("em:0").GetProperty($"{phase}_voltage").GetSingle();
            callback.Invoke(voltage);
        });
    }
    
    public void AddDeserializeEnergyMeterCurrent_EM(Action<float> callback, int meterIndex)
    {
        string phase = indexToPhaseMap[meterIndex];
        
        deserializationCallbacks.Add(resultElement =>
        {
            float current = resultElement.GetProperty("em:0").GetProperty($"{phase}_current").GetSingle();
            callback.Invoke(current);
        });
    }
    
    public void AddDeserializeEnergyMeterActivePower_EM(Action<float> callback, int meterIndex)
    {
        string phase = indexToPhaseMap[meterIndex];
        
        deserializationCallbacks.Add(resultElement =>
        {
            float power = resultElement.GetProperty("em:0").GetProperty($"{phase}_act_power").GetSingle();
            callback.Invoke(power);
        });
    }
    
    public void AddDeserializeEnergyMeterApparentPower_EM(Action<float> callback, int meterIndex)
    {
        string phase = indexToPhaseMap[meterIndex];

        deserializationCallbacks.Add(resultElement =>
        {
            float power = resultElement.GetProperty("em:0").GetProperty($"{phase}_aprt_power").GetSingle();
        });
    }
    
    public void AddDeserializeEnergyMeterPowerFactor_EM(Action<float> callback, int meterIndex)
    {
        string phase = indexToPhaseMap[meterIndex];
        
        deserializationCallbacks.Add(resultElement =>
        {
            float powerFactor = resultElement.GetProperty("em:0").GetProperty($"{phase}_pf").GetSingle();
            callback.Invoke(powerFactor);
        });
    }
    
    public void AddDeserializeEnergyMeterFrequency_EM(Action<float> callback, int meterIndex)
    {
        string phase = indexToPhaseMap[meterIndex];
        
        deserializationCallbacks.Add(resultElement =>
        {
            float frequency = resultElement.GetProperty("em:0").GetProperty($"{phase}_freq").GetSingle();
            callback.Invoke(frequency);
        });
    }
    
    public void AddDeserializeEnergyMeterTotalCurrent_EM(Action<float> callback)
    {
        deserializationCallbacks.Add(resultElement =>
        {
            float current = resultElement.GetProperty("em:0").GetProperty("total_current").GetSingle();
            callback.Invoke(current);
        });
    }
    
    public void AddDeserializeEnergyMeterTotalActivePower_EM(Action<float> callback)
    {
        deserializationCallbacks.Add(resultElement =>
        {
            float power = resultElement.GetProperty("em:0").GetProperty("total_act_power").GetSingle();
            callback.Invoke(power);
        });
    }
    
    public void AddDeserializeEnergyMeterTotalApparentPower_EM(Action<float> callback)
    {
        deserializationCallbacks.Add(resultElement =>
        {
            float power = resultElement.GetProperty("em:0").GetProperty("total_aprt_power").GetSingle();
            callback.Invoke(power);
        });
    }
    
    public void AddDeserializeEnergyMeterPhaseTotalActiveEnergy_EM(Action<float> callback, int meterIndex)
    {
        string phase = indexToPhaseMap[meterIndex];
        
        deserializationCallbacks.Add(resultElement =>
        {
            float energy = resultElement.GetProperty("emdata:0").GetProperty($"{phase}_total_act_energy").GetSingle();
            callback.Invoke(energy);
        });
    }
    
    public void AddDeserializeEnergyMeterPhaseTotalReturnedActiveEnergy_EM(Action<float> callback, int meterIndex)
    {
        string phase = indexToPhaseMap[meterIndex];

        deserializationCallbacks.Add(resultElement =>
        {
            float energy = resultElement.GetProperty("emdata:0").GetProperty($"{phase}_total_act_ret_energy").GetSingle();
            callback.Invoke(energy);
        });
    }
}