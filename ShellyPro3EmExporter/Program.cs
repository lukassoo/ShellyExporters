using System.Globalization;
using Serilog;
using Utilities;
using Utilities.Configs;
using Utilities.Metrics;
using Utilities.Networking;

namespace ShellyPro3EmExporter;

internal static class Program
{
    static ILogger log = null!;
    
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
            
            RuntimeAutomation.Init(config);
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
        if (oldIncorrectMetricNames)
        {
            SetupDevicesWithOldNaming();
            return;
        }
        
        foreach ((IDeviceConnection deviceConnection, List<IMetric> deviceMetrics) in deviceToMetricsDictionary)
        {
            ShellyPro3EmConnection device = (ShellyPro3EmConnection)deviceConnection;
            
            string targetName = device.GetTargetName();
            
            const string metricPrefix = "shelly_";
            const string deviceModel = "Pro3Em";
            string[] phaseLabel = ["phase"];
            
            MeterReading[] meterReadings = device.GetCurrentMeterReadings();

            foreach (MeterReading meterReading in meterReadings)
            {
                if (!meterReading.currentIgnored)
                {
                    const string metricName = metricPrefix + "current_amps";
                    
                    IMetric currentMetric = MetricsHelper.CreateGauge(metricName, "Current (A)", targetName, deviceModel,
                        () => meterReading.current.ToString("0.000", CultureInfo.InvariantCulture), phaseLabel, [meterReading.meterIndex.ToString()]);
                    
                    deviceMetrics.Add(currentMetric);
                }
                
                if (!meterReading.voltageIgnored)
                {
                    const string metricName = metricPrefix + "voltage_volts";
                    
                    IMetric voltageMetric = MetricsHelper.CreateGauge(metricName, "Voltage (V)", targetName, deviceModel,
                        () => meterReading.voltage.ToString("0.00", CultureInfo.InvariantCulture), phaseLabel, [meterReading.meterIndex.ToString()]);
                    
                    deviceMetrics.Add(voltageMetric);
                }
                
                if (!meterReading.activePowerIgnored)
                {
                    const string metricName = metricPrefix + "power_active_watts";   
                    
                    IMetric powerMetric = MetricsHelper.CreateGauge(metricName, "Active Power (W)", targetName, deviceModel,
                        () => meterReading.activePower.ToString("0.00", CultureInfo.InvariantCulture), phaseLabel, [meterReading.meterIndex.ToString()]);
                    
                    deviceMetrics.Add(powerMetric);
                }
                
                if (!meterReading.apparentPowerIgnored)
                {
                    const string metricName = metricPrefix + "power_apparent_va";
                    
                    IMetric apparentPowerMetric = MetricsHelper.CreateGauge(metricName, "Apparent Power (VA)", targetName, deviceModel,
                        () => meterReading.apparentPower.ToString("0.00", CultureInfo.InvariantCulture), phaseLabel, [meterReading.meterIndex.ToString()]);
                    
                    deviceMetrics.Add(apparentPowerMetric);
                }
                
                if (!meterReading.powerFactorIgnored)
                {
                    const string metricName = metricPrefix + "power_factor";
                    
                    IMetric powerFactorMetric = MetricsHelper.CreateGauge(metricName, "Power Factor", targetName, deviceModel,
                        () => meterReading.powerFactor.ToString("0.00", CultureInfo.InvariantCulture), phaseLabel, [meterReading.meterIndex.ToString()]);
                    
                    deviceMetrics.Add(powerFactorMetric);
                }
            }
            
            if (!device.IsTotalCurrentIgnored)
            {
                const string metricName = metricPrefix + "current_total_amps";
                
                IMetric totalCurrentMetric = MetricsHelper.CreateGauge(metricName, "Total Current (A)", targetName, deviceModel,
                    () => device.TotalCurrent.ToString("0.000", CultureInfo.InvariantCulture));
                
                deviceMetrics.Add(totalCurrentMetric);
            }
                            
            if (!device.IsTotalActivePowerIgnored)
            {
                const string metricName = metricPrefix + "power_active_total_watts";   
                
                IMetric totalActivePowerMetric = MetricsHelper.CreateGauge(metricName, "Total Active Power (W)", targetName, deviceModel,
                    () => device.TotalActivePower.ToString("0.00", CultureInfo.InvariantCulture));
                
                deviceMetrics.Add(totalActivePowerMetric);
            }
                
            if (!device.IsTotalApparentPowerIgnored)
            {
                const string metricName = metricPrefix + "power_apparent_total_va";
                
                IMetric totalApparentPowerMetric = MetricsHelper.CreateGauge(metricName, "Total Apparent Power (VA)", targetName, deviceModel,
                    () => device.TotalApparentPower.ToString("0.00", CultureInfo.InvariantCulture));
                
                deviceMetrics.Add(totalApparentPowerMetric);
            }

            if (!device.IsTotalActiveEnergyIgnored)
            {
                const string metricName = metricPrefix + "energy_active_total_wh";   
                
                IMetric totalActiveEnergyMetric = MetricsHelper.CreateGauge(metricName, "Total Active Energy (Wh)", targetName, deviceModel,
                    () => device.TotalActiveEnergy.ToString("0.00", CultureInfo.InvariantCulture));
                
                deviceMetrics.Add(totalActiveEnergyMetric);
            }
            
            if (!device.IsTotalActiveEnergyReturnedIgnored)
            {
                const string metricName = metricPrefix + "energy_active_returned_total_wh";
                
                IMetric totalActiveEnergyReturnedMetric = MetricsHelper.CreateGauge(metricName, "Total Active Energy Returned to the grid (Wh)", targetName, deviceModel,
                    () => device.TotalActiveEnergyReturned.ToString("0.00", CultureInfo.InvariantCulture));
                
                deviceMetrics.Add(totalActiveEnergyReturnedMetric);
            }

            if (!device.IsTotalActiveEnergyPhase1Ignored)
            {
                const string metricName = metricPrefix + "energy_active_total_wh";   
                
                IMetric totalActiveEnergyPhase1Metric = MetricsHelper.CreateGauge(metricName, "Total Phase 1 Active Energy (Wh)", targetName, deviceModel,
                    () => device.TotalActiveEnergyPhase1.ToString("0.00", CultureInfo.InvariantCulture), phaseLabel, ["1"]);
                
                deviceMetrics.Add(totalActiveEnergyPhase1Metric);
            }
            
            if (!device.IsTotalActiveEnergyPhase2Ignored)
            {
                const string metricName = metricPrefix + "energy_active_total_wh";
                
                IMetric totalActiveEnergyPhase2Metric = MetricsHelper.CreateGauge(metricName, "Total Phase 2 Active Energy (Wh)", targetName, deviceModel,
                    () => device.TotalActiveEnergyPhase2.ToString("0.00", CultureInfo.InvariantCulture), phaseLabel, ["2"]);
                
                deviceMetrics.Add(totalActiveEnergyPhase2Metric);
            }
            
            if (!device.IsTotalActiveEnergyPhase3Ignored)
            {
                const string metricName = metricPrefix + "energy_active_total_wh";
                
                IMetric totalActiveEnergyPhase3Metric = MetricsHelper.CreateGauge(metricName, "Total Phase 3 Active Energy (Wh)", targetName, deviceModel,
                    () => device.TotalActiveEnergyPhase3.ToString("0.00", CultureInfo.InvariantCulture), phaseLabel, ["3"]);
                
                deviceMetrics.Add(totalActiveEnergyPhase3Metric);
            }
            
            if (!device.IsTotalActiveEnergyReturnedPhase1Ignored)
            {
                const string metricName = metricPrefix + "energy_active_returned_total_wh";  
                
                IMetric totalActiveEnergyReturnedPhase1Metric = MetricsHelper.CreateGauge(metricName, "Total Phase 1 Active Energy Returned to the grid (Wh)", targetName, deviceModel,
                    () => device.TotalActiveEnergyReturnedPhase1.ToString("0.00", CultureInfo.InvariantCulture), phaseLabel, ["1"]);
                
                deviceMetrics.Add(totalActiveEnergyReturnedPhase1Metric);
            }
            
            if (!device.IsTotalActiveEnergyReturnedPhase2Ignored)
            {
                const string metricName = metricPrefix + "energy_active_returned_total_wh"; 
                
                IMetric totalActiveEnergyReturnedPhase2Metric = MetricsHelper.CreateGauge(metricName, "Total Phase 2 Active Energy Returned to the grid (Wh)", targetName, deviceModel,
                    () => device.TotalActiveEnergyReturnedPhase2.ToString("0.00", CultureInfo.InvariantCulture), phaseLabel, ["2"]);
                
                deviceMetrics.Add(totalActiveEnergyReturnedPhase2Metric);
            }
            
            if (!device.IsTotalActiveEnergyReturnedPhase3Ignored)
            {
                const string metricName = metricPrefix + "energy_active_returned_total_wh";
                
                IMetric totalActiveEnergyReturnedPhase3Metric = MetricsHelper.CreateGauge(metricName, "Total Phase 3 Active Energy Returned to the grid (Wh)", targetName, deviceModel,
                    () => device.TotalActiveEnergyReturnedPhase3.ToString("0.00", CultureInfo.InvariantCulture), phaseLabel, ["3"]);
                
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