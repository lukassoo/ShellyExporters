using System.Globalization;

namespace Utilities.Metrics;

public static class PredefinedMetrics
{
    static readonly string[] phaseLabel = ["phase"];
    
    public static IMetric CreatePhaseVoltageMetric(string targetName, string deviceModel, int phase, Func<float> metricValueGetterFunction)
    {
        return MetricsHelper.CreateGauge("shelly_voltage_volts", "Voltage (V)", targetName, deviceModel,
            () => metricValueGetterFunction().ToString("0.00", CultureInfo.InvariantCulture), phaseLabel, [phase.ToString()]);
    }
    
    public static IMetric CreatePhaseCurrentMetric(string targetName, string deviceModel, int phase, Func<float> metricValueGetterFunction)
    {
        return MetricsHelper.CreateGauge("shelly_current_amps", "Current (A)", targetName, deviceModel,
            () => metricValueGetterFunction().ToString("0.000", CultureInfo.InvariantCulture), phaseLabel, [phase.ToString()]);
    }
    
    public static IMetric CreatePhasePowerMetric(string targetName, string deviceModel, int phase, Func<float> metricValueGetterFunction)
    {
        return MetricsHelper.CreateGauge("shelly_power_watts", "Power (W)", targetName, deviceModel,
            () => metricValueGetterFunction().ToString("0.00", CultureInfo.InvariantCulture), phaseLabel, [phase.ToString()]);
    }
    
    public static IMetric CreatePhaseActivePowerMetric(string targetName, string deviceModel, int phase, Func<float> metricValueGetterFunction)
    {
        return MetricsHelper.CreateGauge("shelly_power_active_watts", "Active Power (W)", targetName, deviceModel,
            () => metricValueGetterFunction().ToString("0.00", CultureInfo.InvariantCulture), phaseLabel, [phase.ToString()]);
    }
    
    public static IMetric CreatePhaseApparentPowerMetric(string targetName, string deviceModel, int phase, Func<float> metricValueGetterFunction)
    {
        return MetricsHelper.CreateGauge("shelly_power_apparent_va", "Apparent Power (VA)", targetName, deviceModel,
            () => metricValueGetterFunction().ToString("0.00", CultureInfo.InvariantCulture), phaseLabel, [phase.ToString()]);
    }

    public static IMetric CreatePhaseReactivePowerMetric(string targetName, string deviceModel, int phase, Func<float> metricValueGetterFunction)
    {
        return MetricsHelper.CreateGauge("shelly_power_reactive_watts", "Reactive Power (W)", targetName, deviceModel,
            () => metricValueGetterFunction().ToString("0.00", CultureInfo.InvariantCulture), phaseLabel, [phase.ToString()]);
    }
    
    public static IMetric CreatePhasePowerFactorMetric(string targetName, string deviceModel, int phase, Func<float> metricValueGetterFunction)
    {
        return MetricsHelper.CreateGauge("shelly_power_factor", "Power Factor", targetName, deviceModel,
            () => metricValueGetterFunction().ToString("0.00", CultureInfo.InvariantCulture), phaseLabel, [phase.ToString()]);
    }

    public static IMetric CreatePhaseEnergyTotalMetric(string targetName, string deviceModel, int phase, Func<float> metricValueGetterFunction)
    {
        return MetricsHelper.CreateGauge("shelly_energy_total_wh", "Total Energy (Wh)", targetName, deviceModel,
            () => metricValueGetterFunction().ToString("0.00", CultureInfo.InvariantCulture), phaseLabel, [phase.ToString()]);
    }
    
    public static IMetric CreatePhaseEnergyReturnedMetric(string targetName, string deviceModel, int phase, Func<float> metricValueGetterFunction)
    {
        return MetricsHelper.CreateGauge("shelly_energy_returned_total_wh", "Total Energy Returned to the grid (Wh)", targetName, deviceModel,
            () => metricValueGetterFunction().ToString("0.00", CultureInfo.InvariantCulture), phaseLabel, [phase.ToString()]);
    }
    
    public static IMetric CreatePhaseTotalActiveEnergyMetric(string targetName, string deviceModel, int phase, Func<float> metricValueGetterFunction)
    {
        return MetricsHelper.CreateGauge("shelly_energy_active_total_wh", "Total Active Energy (Wh)", targetName, deviceModel,
            () => metricValueGetterFunction().ToString("0.00", CultureInfo.InvariantCulture), phaseLabel, [phase.ToString()]);
    }
    
    public static IMetric CreatePhaseTotalActiveEnergyReturnedMetric(string targetName, string deviceModel, int phase, Func<float> metricValueGetterFunction)
    {
        return MetricsHelper.CreateGauge("shelly_energy_active_returned_total_wh", "Total Active Energy Returned to the grid (Wh)", targetName, deviceModel,
            () => metricValueGetterFunction().ToString("0.00", CultureInfo.InvariantCulture), phaseLabel, [phase.ToString()]);
    }
    
    public static IMetric CreatePhaseTemperatureMetric(string targetName, string deviceModel, int phase, Func<float> metricValueGetterFunction)
    {
        return MetricsHelper.CreateGauge("shelly_temperature_celsius", "Temperature (°C)", targetName, deviceModel,
            () => metricValueGetterFunction().ToString("0.00", CultureInfo.InvariantCulture), phaseLabel, [phase.ToString()]);
    }
    
    public static IMetric CreatePhaseRelayStateMetric(string targetName, string deviceModel, int phase, Func<bool> metricValueGetterFunction)
    {
        return MetricsHelper.CreateGauge("shelly_relay_state", "The state of the relay", targetName, deviceModel,
            () => metricValueGetterFunction() ? "1" : "0", phaseLabel, [phase.ToString()]);
    }

    public static IMetric CreatePhaseFrequencyMetric(string targetName, string deviceModel, int phase, Func<float> metricValueGetterFunction)
    {
        return MetricsHelper.CreateGauge("shelly_frequency_hz", "Frequency (Hz)", targetName, deviceModel,
            () => metricValueGetterFunction().ToString("0.00", CultureInfo.InvariantCulture), phaseLabel, [phase.ToString()]);
    }
    
    
    public static IMetric CreateCurrentMetric(string targetName, string deviceModel, Func<float> metricValueGetterFunction)
    {
        return MetricsHelper.CreateGauge("shelly_current_amps", "Current (A)", targetName, deviceModel,
            () => metricValueGetterFunction().ToString("0.000", CultureInfo.InvariantCulture));
    }

    public static IMetric CreateTotalCurrentMetric(string targetName, string deviceModel, Func<float> metricValueGetterFunction)
    {
        return MetricsHelper.CreateGauge("shelly_current_total_amps", "Total Current (A)", targetName, deviceModel,
            () => metricValueGetterFunction().ToString("0.000", CultureInfo.InvariantCulture));
    }
    
    public static IMetric CreateVoltageMetric(string targetName, string deviceModel, Func<float> metricValueGetterFunction)
    {
        return MetricsHelper.CreateGauge("shelly_voltage_volts", "Voltage (V)", targetName, deviceModel,
            () => metricValueGetterFunction().ToString("0.00", CultureInfo.InvariantCulture));
    }
    
    public static IMetric CreatePowerMetric(string targetName, string deviceModel, Func<float> metricValueGetterFunction)
    {
        return MetricsHelper.CreateGauge("shelly_power_watts", "Power (W)", targetName, deviceModel, 
            () => metricValueGetterFunction().ToString("0.00", CultureInfo.InvariantCulture));
    }

    public static IMetric CreateTotalActivePowerMetric(string targetName, string deviceModel, Func<float> metricValueGetterFunction)
    {
        return MetricsHelper.CreateGauge("shelly_power_active_total_watts", "Total Active Power (W)", targetName, deviceModel,
            () => metricValueGetterFunction().ToString("0.00", CultureInfo.InvariantCulture));
    }

    public static IMetric CreateTotalApparentPowerMetric(string targetName, string deviceModel, Func<float> metricValueGetterFunction)
    {
        return MetricsHelper.CreateGauge("shelly_power_apparent_total_va", "Total Apparent Power (VA)", targetName, deviceModel,
            () => metricValueGetterFunction().ToString("0.00", CultureInfo.InvariantCulture));
    }
    
    public static IMetric CreateTotalEnergyMetric(string targetName, string deviceModel, Func<float> metricValueGetterFunction)
    {
        return MetricsHelper.CreateGauge("shelly_energy_total_wh", "Total Energy (Wh)", targetName, deviceModel,
            () => metricValueGetterFunction().ToString("0.00", CultureInfo.InvariantCulture));
    }
    
    public static IMetric CreateTotalActiveEnergyMetric(string targetName, string deviceModel, Func<float> metricValueGetterFunction)
    {
        return MetricsHelper.CreateGauge("shelly_energy_active_total_wh", "Total Active Energy (Wh)", targetName, deviceModel,
            () => metricValueGetterFunction().ToString("0.00", CultureInfo.InvariantCulture));
    }

    public static IMetric CreateTotalActiveEnergyReturnedMetric(string targetName, string deviceModel, Func<float> metricValueGetterFunction)
    {
        return MetricsHelper.CreateGauge("shelly_energy_active_returned_total_wh", "Total Active Energy returned to the grid (Wh)", targetName, deviceModel,
            () => metricValueGetterFunction().ToString("0.00", CultureInfo.InvariantCulture));
    }
    
    public static IMetric CreateTemperatureMetric(string targetName, string deviceModel, Func<float> metricValueGetterFunction)
    {
        return MetricsHelper.CreateGauge("shelly_temperature_celsius", "Temperature (°C)", targetName, deviceModel,
            () => metricValueGetterFunction().ToString("0.00", CultureInfo.InvariantCulture));
    }
    
    public static IMetric CreateRelayStateMetric(string targetName, string deviceModel, Func<bool> metricValueGetterFunction)
    {
        return MetricsHelper.CreateGauge("shelly_relay_state", "The state of the relay", targetName, deviceModel,
            () => metricValueGetterFunction() ? "1" : "0");
    }

    public static IMetric CreateInputStateMetric(string targetName, string deviceModel, Func<bool> metricValueGetterFunction)
    {
        return MetricsHelper.CreateGauge("shelly_input_state", "The state of the input", targetName, deviceModel,
            () => metricValueGetterFunction() ? "1" : "0");
    }
    
    public static IMetric CreateInputPercentMetric(string targetName, string deviceModel, Func<float> metricValueGetterFunction)
    {
        return MetricsHelper.CreateGauge("shelly_input_percent", "Input analog value in percent", targetName, deviceModel,
            () => metricValueGetterFunction().ToString("0.00", CultureInfo.InvariantCulture));
    }
    
    public static IMetric CreateInputCountTotalMetric(string targetName, string deviceModel, Func<float> metricValueGetterFunction)
    {
        return MetricsHelper.CreateGauge("shelly_input_count", "Total pulses counted on the input", targetName, deviceModel,
            () => metricValueGetterFunction().ToString("D", CultureInfo.InvariantCulture));
    }
    
    public static IMetric CreateInputFrequencyMetric(string targetName, string deviceModel, Func<float> metricValueGetterFunction)
    {
        return MetricsHelper.CreateGauge("shelly_input_frequency_hz", "Network frequency on the input in hertz", targetName, deviceModel,
            () => metricValueGetterFunction().ToString("0.00", CultureInfo.InvariantCulture));
    }
}