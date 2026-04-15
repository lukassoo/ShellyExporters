using System.Globalization;
using NuGet.Versioning;
using Serilog;
using Utilities;
using Utilities.Configs;
using Utilities.Logging;
using Utilities.Metrics;

namespace ShellyPro4PmExporter;

internal static class Program
{
    static ILogger log = null!;

    public static SemanticVersion CurrentVersion { get; } = SemanticVersion.Parse("1.0.0");
    public static DateTime BuildTime { get; } = DateTime.UtcNow;
    
    const string configName = "shellyPro4PmExporter";
    const int defaultPort = 10037;
    static int listenPort = defaultPort;

    static readonly List<Device> devices = new(1);

    static async Task Main()
    {
        try
        {
            if (!ConfigHelper.LoadAndUpdateConfig(configName, defaultPort, WriteExampleConfig, out Config<TargetDevice>? config))
            {
                Console.WriteLine("[ERROR] Could not load config - returning");
                return;
            }

            RuntimeAutomation.Init(config, CurrentVersion, BuildTime);
            log = Log.ForContext(typeof(Program));

            listenPort = config.listenPort;

            SetupDevicesFromConfig(config);
            SetupMetrics(config.useOldIncorrectMetricNames);
            
            if (!MetricsServer.Start((ushort)listenPort, _ => MetricsHelper.UpdateDeviceMetrics(devices)))
            {
                RuntimeAutomation.Shutdown("Failed to start metrics server");
            }
        }
        catch (Exception exception)
        {
            log.Error(exception, "Exception in Main()");
            
            LogAdditions.CheckExceptionLogIndexError(exception, log);
            RuntimeAutomation.Shutdown("Exception in Main()");
        }

        await RuntimeAutomation.WaitForShutdown();
    }

    static bool WriteExampleConfig()
    {
        try
        {
            Config<TargetDevice> config = new()
            {
                listenPort = defaultPort,
                useOldIncorrectMetricNames = false
            };

            TargetMeter[] targetMeters =
            [
                new(0),
                new(1),
                new(2),
                new(3)
            ];

            config.targets.Add(new TargetDevice("Your Name for the device - like \"power_sockets\" - keep it formatted like that, lowercase with underscores",
                "Address (usually 192.168.X.X - the IP of your device)",
                "Password (leave empty if not used)",
                targetMeters));

            Configuration.WriteConfig(configName, config);
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    static void SetupDevicesFromConfig(Config<TargetDevice> config)
    {
        log.Information("Setting up connections from config");

        foreach (TargetDevice target in config.targets)
        {
            log.Information("Setting up: {targetName} at: {url} requires auth: {requiresAuth}", target.name, target.url, target.RequiresAuthentication());
            devices.Add(new ShellyPro4Pm(target));
        }
    }

    static void SetupMetrics(bool oldIncorrectMetricNames)
    {
        log.Information("Setting up metrics");
        
        if (oldIncorrectMetricNames)
        {
            SetupDevicesWithOldNaming();
            return;
        }
        
        foreach (Device baseDevice in devices)
        {
            ShellyPro4Pm device = (ShellyPro4Pm)baseDevice;
            List<IMetric> deviceMetrics = device.Metrics;
            
            string targetName = device.TargetName;
            const string deviceModel = "Pro4Pm";

            MeterReading[] meterReadings = device.GetCurrentMeterReadings();

            foreach (MeterReading meterReading in meterReadings)
            {
                if (!meterReading.currentIgnored)
                {
                    IMetric currentMetric = PredefinedMetrics.CreatePhaseCurrentMetric(targetName, deviceModel, meterReading.meterIndex, () => meterReading.current);
                    deviceMetrics.Add(currentMetric);
                }

                if (!meterReading.voltageIgnored)
                {
                    IMetric voltageMetric = PredefinedMetrics.CreatePhaseVoltageMetric(targetName, deviceModel, meterReading.meterIndex, () => meterReading.voltage);
                    deviceMetrics.Add(voltageMetric);
                }

                if (!meterReading.activePowerIgnored)
                {
                    IMetric powerMetric = PredefinedMetrics.CreatePhaseActivePowerMetric(targetName, deviceModel, meterReading.meterIndex, () => meterReading.activePower);
                    deviceMetrics.Add(powerMetric);
                }

                if (!meterReading.powerFactorIgnored)
                {
                    IMetric powerFactorMetric = PredefinedMetrics.CreatePhasePowerFactorMetric(targetName, deviceModel, meterReading.meterIndex, () => meterReading.powerFactor);
                    deviceMetrics.Add(powerFactorMetric);
                }

                if (!meterReading.frequencyIgnored)
                {
                    IMetric frequencyMetric = PredefinedMetrics.CreatePhaseFrequencyMetric(targetName, deviceModel, meterReading.meterIndex, () => meterReading.frequency);
                    deviceMetrics.Add(frequencyMetric);
                }

                if (!meterReading.totalActiveEnergyIgnored)
                {
                    IMetric totalMetric = PredefinedMetrics.CreatePhaseTotalActiveEnergyMetric(targetName, deviceModel, meterReading.meterIndex, () => meterReading.totalActiveEnergy);
                    deviceMetrics.Add(totalMetric);
                }

                if (!meterReading.totalReturnedActiveEnergyIgnored)
                {
                    IMetric totalReturnedMetric = PredefinedMetrics.CreatePhaseTotalActiveEnergyReturnedMetric(targetName, deviceModel, meterReading.meterIndex, () => meterReading.totalReturnedActiveEnergy);
                    deviceMetrics.Add(totalReturnedMetric);
                }

                if (!meterReading.temperatureIgnored)
                {
                    IMetric temperatureMetric = PredefinedMetrics.CreatePhaseTemperatureMetric(targetName, deviceModel, meterReading.meterIndex, () => meterReading.temperature);
                    deviceMetrics.Add(temperatureMetric);
                }

                if (!meterReading.outputIgnored)
                {
                    IMetric outputMetric = PredefinedMetrics.CreatePhaseRelayStateMetric(targetName, deviceModel, meterReading.meterIndex, () => meterReading.output);
                    deviceMetrics.Add(outputMetric);
                }
            }
        }
    }

    static void SetupDevicesWithOldNaming()
    {
        foreach (Device baseDevice in devices)
        {
            ShellyPro4Pm device = (ShellyPro4Pm)baseDevice;
            List<IMetric> deviceMetrics = device.Metrics;
            
            string deviceName = device.TargetName;
            string metricPrefix = "shellyPro4Pm_" + deviceName + "_";

            MeterReading[] meterReadings = device.GetCurrentMeterReadings();

            foreach (MeterReading meterReading in meterReadings)
            {
                if (!meterReading.currentIgnored)
                {
                    IMetric currentMetric = MetricsHelper.CreateGauge(metricPrefix + meterReading.meterIndex + "_current", "Current (A)", 
                        () => meterReading.current.ToString("0.000", CultureInfo.InvariantCulture));
                    
                    deviceMetrics.Add(currentMetric);
                }

                if (!meterReading.voltageIgnored)
                {
                    IMetric voltageMetric = MetricsHelper.CreateGauge(metricPrefix + meterReading.meterIndex + "_voltage", "Voltage (V)", 
                        () => meterReading.voltage.ToString("0.00", CultureInfo.InvariantCulture));
                    
                    deviceMetrics.Add(voltageMetric);
                }

                if (!meterReading.activePowerIgnored)
                {
                    IMetric powerMetric = MetricsHelper.CreateGauge(metricPrefix + meterReading.meterIndex + "_active_power", "Active Power (W)", 
                        () => meterReading.activePower.ToString("0.00", CultureInfo.InvariantCulture));
                    
                    deviceMetrics.Add(powerMetric);
                }

                if (!meterReading.powerFactorIgnored)
                {
                    IMetric powerFactorMetric = MetricsHelper.CreateGauge(metricPrefix + meterReading.meterIndex + "_power_factor", "Power Factor", 
                        () => meterReading.powerFactor.ToString("0.00", CultureInfo.InvariantCulture));
                    
                    deviceMetrics.Add(powerFactorMetric);
                }

                if (!meterReading.frequencyIgnored)
                {
                    IMetric frequencyMetric = MetricsHelper.CreateGauge(metricPrefix + meterReading.meterIndex + "_frequency", "Frequency (Hz)", 
                        () => meterReading.frequency.ToString("0.00", CultureInfo.InvariantCulture));
                    
                    deviceMetrics.Add(frequencyMetric);
                }

                if (!meterReading.totalActiveEnergyIgnored)
                {
                    IMetric totalMetric = MetricsHelper.CreateGauge(metricPrefix + meterReading.meterIndex + "_total_energy", "Total Energy (Wh)", 
                        () => meterReading.totalActiveEnergy.ToString("0.000", CultureInfo.InvariantCulture));
                    
                    deviceMetrics.Add(totalMetric);
                }

                if (!meterReading.totalReturnedActiveEnergyIgnored)
                {
                    IMetric totalReturnedMetric = MetricsHelper.CreateGauge(metricPrefix + meterReading.meterIndex + "_total_returned_energy", "Total Returned Energy (Wh)",
                        () => meterReading.totalReturnedActiveEnergy.ToString("0.000", CultureInfo.InvariantCulture));
                    
                    deviceMetrics.Add(totalReturnedMetric);
                }

                if (!meterReading.temperatureIgnored)
                {
                    IMetric temperatureMetric = MetricsHelper.CreateGauge(metricPrefix + meterReading.meterIndex + "_temperature", "Temperature (°C)", 
                        () => meterReading.temperature.ToString("0.0", CultureInfo.InvariantCulture));
                    
                    deviceMetrics.Add(temperatureMetric);
                }

                if (!meterReading.outputIgnored)
                {
                    IMetric outputMetric = MetricsHelper.CreateGauge(metricPrefix + meterReading.meterIndex + "_output", "Output State", 
                        () => meterReading.output ? "1" : "0");
                    
                    deviceMetrics.Add(outputMetric);
                }
            }
        }
    }
}