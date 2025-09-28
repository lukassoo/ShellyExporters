using System.Globalization;
using NuGet.Versioning;
using Serilog;
using Utilities;
using Utilities.Configs;
using Utilities.Metrics;
using Utilities.Networking;

namespace ShellyPro3EmExporter;

internal static class Program
{
    static ILogger log = null!;
    
    public static SemanticVersion CurrentVersion { get; } = SemanticVersion.Parse("1.0.0");
    public static DateTime BuildTime { get; } = DateTime.UtcNow;
    
    const string configName = "shellyPro3EmExporter";
    const int defaultPort = 10011;
    static int listenPort = defaultPort;
    
    static readonly Dictionary<IDeviceConnection, List<IMetric>> deviceToMetricsDictionary = new(1);

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
            
            if (!MetricsServer.Start((ushort)listenPort, _ => MetricsHelper.UpdateDeviceMetrics(deviceToMetricsDictionary)))
            {
                RuntimeAutomation.Shutdown("Failed to start metrics server");
            }
        }
        catch (Exception exception)
        {
            log.Error(exception, "Exception in Main()");
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
                new(1),
                new(2),
                new(3)
            ];
                
            config.targets.Add(new TargetDevice("Your Name for the device - like \"solar_power\" - keep it formatted like that, lowercase with underscores", 
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
            deviceToMetricsDictionary.Add(new ShellyPro3EmConnection(target), []);
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
        
        foreach ((IDeviceConnection deviceConnection, List<IMetric> deviceMetrics) in deviceToMetricsDictionary)
        {
            ShellyPro3EmConnection device = (ShellyPro3EmConnection)deviceConnection;
            
            string targetName = device.GetTargetName();
            const string deviceModel = "Pro3Em";
            
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
                
                if (!meterReading.apparentPowerIgnored)
                {
                    IMetric apparentPowerMetric = PredefinedMetrics.CreatePhaseApparentPowerMetric(targetName, deviceModel, meterReading.meterIndex, () => meterReading.apparentPower);
                    deviceMetrics.Add(apparentPowerMetric);
                }
                
                if (!meterReading.powerFactorIgnored)
                {
                    IMetric powerFactorMetric = PredefinedMetrics.CreatePhasePowerFactorMetric(targetName, deviceModel, meterReading.meterIndex, () => meterReading.powerFactor);
                    deviceMetrics.Add(powerFactorMetric);
                }
            }
            
            if (!device.IsTotalCurrentIgnored)
            {
                IMetric totalCurrentMetric = PredefinedMetrics.CreateTotalCurrentMetric(targetName, deviceModel, () => device.TotalCurrent);
                deviceMetrics.Add(totalCurrentMetric);
            }
                            
            if (!device.IsTotalActivePowerIgnored)
            {
                IMetric totalActivePowerMetric = PredefinedMetrics.CreateTotalActivePowerMetric(targetName, deviceModel, () => device.TotalActivePower);
                deviceMetrics.Add(totalActivePowerMetric);
            }
                
            if (!device.IsTotalApparentPowerIgnored)
            {
                IMetric totalApparentPowerMetric = PredefinedMetrics.CreateTotalApparentPowerMetric(targetName, deviceModel, () => device.TotalApparentPower);
                deviceMetrics.Add(totalApparentPowerMetric);
            }

            if (!device.IsTotalActiveEnergyIgnored)
            {
                IMetric totalActiveEnergyMetric = PredefinedMetrics.CreateTotalActiveEnergyMetric(targetName, deviceModel, () => device.TotalActiveEnergy);
                deviceMetrics.Add(totalActiveEnergyMetric);
            }
            
            if (!device.IsTotalActiveEnergyReturnedIgnored)
            {
                IMetric totalActiveEnergyReturnedMetric = PredefinedMetrics.CreateTotalActiveEnergyReturnedMetric(targetName, deviceModel, () => device.TotalActiveEnergyReturned);
                deviceMetrics.Add(totalActiveEnergyReturnedMetric);
            }

            if (!device.IsTotalActiveEnergyPhase1Ignored)
            {
                IMetric totalActiveEnergyPhase1Metric = PredefinedMetrics.CreatePhaseTotalActiveEnergyMetric(targetName, deviceModel, 1, () => device.TotalActiveEnergyPhase1);
                deviceMetrics.Add(totalActiveEnergyPhase1Metric);
            }
            
            if (!device.IsTotalActiveEnergyPhase2Ignored)
            {
                IMetric totalActiveEnergyPhase2Metric = PredefinedMetrics.CreatePhaseTotalActiveEnergyMetric(targetName, deviceModel, 2, () => device.TotalActiveEnergyPhase2);
                deviceMetrics.Add(totalActiveEnergyPhase2Metric);
            }
            
            if (!device.IsTotalActiveEnergyPhase3Ignored)
            {
                IMetric totalActiveEnergyPhase3Metric = PredefinedMetrics.CreatePhaseTotalActiveEnergyMetric(targetName, deviceModel, 3, () => device.TotalActiveEnergyPhase3);
                deviceMetrics.Add(totalActiveEnergyPhase3Metric);
            }
            
            if (!device.IsTotalActiveEnergyReturnedPhase1Ignored)
            {
                IMetric totalActiveEnergyReturnedPhase1Metric = PredefinedMetrics.CreatePhaseTotalActiveEnergyReturnedMetric(targetName, deviceModel, 1, () => device.TotalActiveEnergyReturnedPhase1);
                deviceMetrics.Add(totalActiveEnergyReturnedPhase1Metric);
            }
            
            if (!device.IsTotalActiveEnergyReturnedPhase2Ignored)
            {
                IMetric totalActiveEnergyReturnedPhase2Metric = PredefinedMetrics.CreatePhaseTotalActiveEnergyReturnedMetric(targetName, deviceModel, 2, () => device.TotalActiveEnergyReturnedPhase2);
                deviceMetrics.Add(totalActiveEnergyReturnedPhase2Metric);
            }
            
            if (!device.IsTotalActiveEnergyReturnedPhase3Ignored)
            {
                IMetric totalActiveEnergyReturnedPhase3Metric = PredefinedMetrics.CreatePhaseTotalActiveEnergyReturnedMetric(targetName, deviceModel, 3, () => device.TotalActiveEnergyReturnedPhase3);
                deviceMetrics.Add(totalActiveEnergyReturnedPhase3Metric);
            }
        }
    }

    static void SetupDevicesWithOldNaming()
    {
        foreach ((IDeviceConnection deviceConnection, List<IMetric> deviceMetrics) in deviceToMetricsDictionary)
        {
            ShellyPro3EmConnection device = (ShellyPro3EmConnection)deviceConnection;
            
            string deviceName = device.GetTargetName();
            
            string oldMetricPrefix = "shellyPro3Em_" + deviceName + "_";

            MeterReading[] meterReadings = device.GetCurrentMeterReadings();

            foreach (MeterReading meterReading in meterReadings)
            {
                if (!meterReading.currentIgnored)
                {
                    string metricName = oldMetricPrefix + meterReading.meterIndex + "_current";
                    
                    IMetric currentMetric = MetricsHelper.CreateGauge(metricName, "Current (A)", deviceName, 
                        () => meterReading.current.ToString("0.000", CultureInfo.InvariantCulture));
                    
                    deviceMetrics.Add(currentMetric);
                }
                
                if (!meterReading.voltageIgnored)
                {
                    string metricName = oldMetricPrefix + meterReading.meterIndex + "_voltage";
                    
                    IMetric voltageMetric = MetricsHelper.CreateGauge(metricName, "Voltage (V)", deviceName, 
                        () => meterReading.voltage.ToString("0.00", CultureInfo.InvariantCulture));
                    
                    deviceMetrics.Add(voltageMetric);
                }
                
                if (!meterReading.activePowerIgnored)
                {
                    string metricName = oldMetricPrefix + meterReading.meterIndex + "_active_power";   
                    
                    IMetric powerMetric = MetricsHelper.CreateGauge(metricName, "Active Power (W)", deviceName, 
                        () => meterReading.activePower.ToString("0.00", CultureInfo.InvariantCulture));
                    
                    deviceMetrics.Add(powerMetric);
                }
                
                if (!meterReading.apparentPowerIgnored)
                {
                    string metricName = oldMetricPrefix + meterReading.meterIndex + "_apparent_power";
                    
                    IMetric apparentPowerMetric = MetricsHelper.CreateGauge(metricName, "Apparent Power (VA)", deviceName, 
                        () => meterReading.apparentPower.ToString("0.00", CultureInfo.InvariantCulture));
                    
                    deviceMetrics.Add(apparentPowerMetric);
                }
                
                if (!meterReading.powerFactorIgnored)
                {
                    IMetric powerFactorMetric = MetricsHelper.CreateGauge(oldMetricPrefix + meterReading.meterIndex + "_power_factor", "Power Factor", deviceName, 
                        () => meterReading.powerFactor.ToString("0.00", CultureInfo.InvariantCulture));
                    
                    deviceMetrics.Add(powerFactorMetric);
                }
            }
            
            if (!device.IsTotalCurrentIgnored)
            {
                string metricName = oldMetricPrefix + "total_current";
                
                IMetric totalCurrentMetric = MetricsHelper.CreateGauge(metricName, "Total Current (A)", deviceName, 
                    () => device.TotalCurrent.ToString("0.000", CultureInfo.InvariantCulture));
                
                deviceMetrics.Add(totalCurrentMetric);
            }
                            
            if (!device.IsTotalActivePowerIgnored)
            {
                string metricName = oldMetricPrefix + "total_active_power";
                
                IMetric totalActivePowerMetric = MetricsHelper.CreateGauge(metricName, "Total Active Power (W)", deviceName, 
                    () => device.TotalActivePower.ToString("0.00", CultureInfo.InvariantCulture));
                
                deviceMetrics.Add(totalActivePowerMetric);
            }
                
            if (!device.IsTotalApparentPowerIgnored)
            {
                string metricName = oldMetricPrefix + "total_apparent_power";
                
                IMetric totalApparentPowerMetric = MetricsHelper.CreateGauge(metricName, "Total Apparent Power (VA)", deviceName, 
                    () => device.TotalApparentPower.ToString("0.00", CultureInfo.InvariantCulture));
                
                deviceMetrics.Add(totalApparentPowerMetric);
            }

            if (!device.IsTotalActiveEnergyIgnored)
            {
                string metricName = oldMetricPrefix + "total_active_energy";   
                
                IMetric totalActiveEnergyMetric = MetricsHelper.CreateGauge(metricName, "Total Active Energy (Wh)", deviceName, 
                    () => device.TotalActiveEnergy.ToString("0.00", CultureInfo.InvariantCulture));
                
                deviceMetrics.Add(totalActiveEnergyMetric);
            }
            
            if (!device.IsTotalActiveEnergyReturnedIgnored)
            {
                string metricName = oldMetricPrefix + "total_active_energy_returned";
                
                IMetric totalActiveEnergyReturnedMetric = MetricsHelper.CreateGauge(metricName, "Total Active Energy Returned to the grid (Wh)", deviceName, 
                    () => device.TotalActiveEnergyReturned.ToString("0.00", CultureInfo.InvariantCulture));
                
                deviceMetrics.Add(totalActiveEnergyReturnedMetric);
            }

            if (!device.IsTotalActiveEnergyPhase1Ignored)
            {
                string metricName = oldMetricPrefix + "total_active_energy_phase_1";   
                
                IMetric totalActiveEnergyPhase1Metric = MetricsHelper.CreateGauge(metricName, "Total Phase 1 Active Energy (Wh)", deviceName, 
                    () => device.TotalActiveEnergyPhase1.ToString("0.00", CultureInfo.InvariantCulture));
                
                deviceMetrics.Add(totalActiveEnergyPhase1Metric);
            }
            
            if (!device.IsTotalActiveEnergyPhase2Ignored)
            {
                string metricName = oldMetricPrefix + "total_active_energy_phase_2";
                
                IMetric totalActiveEnergyPhase2Metric = MetricsHelper.CreateGauge(metricName, "Total Phase 2 Active Energy (Wh)", deviceName, 
                    () => device.TotalActiveEnergyPhase2.ToString("0.00", CultureInfo.InvariantCulture));
                
                deviceMetrics.Add(totalActiveEnergyPhase2Metric);
            }
            
            if (!device.IsTotalActiveEnergyPhase3Ignored)
            {
                string metricName = oldMetricPrefix + "total_active_energy_phase_3";
                
                IMetric totalActiveEnergyPhase3Metric = MetricsHelper.CreateGauge(metricName, "Total Phase 3 Active Energy (Wh)", deviceName, 
                    () => device.TotalActiveEnergyPhase3.ToString("0.00", CultureInfo.InvariantCulture));
                
                deviceMetrics.Add(totalActiveEnergyPhase3Metric);
            }
            
            if (!device.IsTotalActiveEnergyReturnedPhase1Ignored)
            {
                string metricName = oldMetricPrefix + "total_active_energy_returned_phase_1";  
                
                IMetric totalActiveEnergyReturnedPhase1Metric = MetricsHelper.CreateGauge(metricName, "Total Phase 1 Active Energy Returned to the grid (Wh)", deviceName, 
                    () => device.TotalActiveEnergyReturnedPhase1.ToString("0.00", CultureInfo.InvariantCulture));
                
                deviceMetrics.Add(totalActiveEnergyReturnedPhase1Metric);
            }
            
            if (!device.IsTotalActiveEnergyReturnedPhase2Ignored)
            {
                string metricName = oldMetricPrefix + "total_active_energy_returned_phase_2"; 
                
                IMetric totalActiveEnergyReturnedPhase2Metric = MetricsHelper.CreateGauge(metricName, "Total Phase 2 Active Energy Returned to the grid (Wh)", deviceName,
                    () => device.TotalActiveEnergyReturnedPhase2.ToString("0.00", CultureInfo.InvariantCulture));
                
                deviceMetrics.Add(totalActiveEnergyReturnedPhase2Metric);
            }
            
            if (!device.IsTotalActiveEnergyReturnedPhase3Ignored)
            {
                string metricName = oldMetricPrefix + "total_active_energy_returned_phase_3";
                
                IMetric totalActiveEnergyReturnedPhase3Metric = MetricsHelper.CreateGauge(metricName, "Total Phase 3 Active Energy Returned to the grid (Wh)", deviceName,
                    () => device.TotalActiveEnergyReturnedPhase3.ToString("0.00", CultureInfo.InvariantCulture));
                
                deviceMetrics.Add(totalActiveEnergyReturnedPhase3Metric);
            }
        }
    }
}