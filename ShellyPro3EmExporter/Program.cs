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
        foreach ((IDeviceConnection deviceConnection, List<IMetric> deviceMetrics) in deviceToMetricsDictionary)
        {
            ShellyPro3EmConnection device = (ShellyPro3EmConnection)deviceConnection;
            
            string deviceName = device.GetTargetName();
            
            string oldMetricPrefix = "shellyPro3Em_" + deviceName + "_";
            const string newMetricPrefix = "shellyPro3Em_";
            
            string metricPrefix = oldIncorrectMetricNames ? oldMetricPrefix : newMetricPrefix;
            
            MeterReading[] meterReadings = device.GetCurrentMeterReadings();

            foreach (MeterReading meterReading in meterReadings)
            {
                if (!meterReading.currentIgnored)
                {
                    IMetric currentMetric = MetricsHelper.CreateGauge(metricPrefix + meterReading.meterIndex + "_current", "Current (A)", deviceName, 
                        () => meterReading.current.ToString("0.000", CultureInfo.InvariantCulture));
                    
                    deviceMetrics.Add(currentMetric);
                }
                
                if (!meterReading.voltageIgnored)
                {
                    IMetric voltageMetric = MetricsHelper.CreateGauge(metricPrefix + meterReading.meterIndex + "_voltage", "Voltage (V)", deviceName, 
                        () => meterReading.voltage.ToString("0.00", CultureInfo.InvariantCulture));
                    
                    deviceMetrics.Add(voltageMetric);
                }
                
                if (!meterReading.activePowerIgnored)
                {
                    IMetric powerMetric = MetricsHelper.CreateGauge(metricPrefix + meterReading.meterIndex + "_active_power", "Active Power (W)", deviceName, 
                        () => meterReading.activePower.ToString("0.00", CultureInfo.InvariantCulture));
                    
                    deviceMetrics.Add(powerMetric);
                }
                
                if (!meterReading.apparentPowerIgnored)
                {
                    IMetric apparentPowerMetric = MetricsHelper.CreateGauge(metricPrefix + meterReading.meterIndex + "_apparent_power", "Apparent Power (VA)", deviceName, 
                        () => meterReading.apparentPower.ToString("0.00", CultureInfo.InvariantCulture));
                    
                    deviceMetrics.Add(apparentPowerMetric);
                }
                
                if (!meterReading.powerFactorIgnored)
                {
                    IMetric powerFactorMetric = MetricsHelper.CreateGauge(metricPrefix + meterReading.meterIndex + "_power_factor", "Power Factor", deviceName, 
                        () => meterReading.powerFactor.ToString("0.00", CultureInfo.InvariantCulture));
                    
                    deviceMetrics.Add(powerFactorMetric);
                }
            }
            
            if (!device.IsTotalCurrentIgnored)
            {
                IMetric totalCurrentMetric = MetricsHelper.CreateGauge(metricPrefix + "total_current", "Total Current (A)", deviceName, 
                    () => device.TotalCurrent.ToString("0.000", CultureInfo.InvariantCulture));
                
                deviceMetrics.Add(totalCurrentMetric);
            }
                            
            if (!device.IsTotalActivePowerIgnored)
            {
                IMetric totalActivePowerMetric = MetricsHelper.CreateGauge(metricPrefix + "total_active_power", "Total Active Power (W)", deviceName, 
                    () => device.TotalActivePower.ToString("0.00", CultureInfo.InvariantCulture));
                
                deviceMetrics.Add(totalActivePowerMetric);
            }
                
            if (!device.IsTotalApparentPowerIgnored)
            {
                IMetric totalApparentPowerMetric = MetricsHelper.CreateGauge(metricPrefix + "total_apparent_power", "Total Apparent Power (VA)", deviceName, 
                    () => device.TotalApparentPower.ToString("0.00", CultureInfo.InvariantCulture));
                
                deviceMetrics.Add(totalApparentPowerMetric);
            }

            if (!device.IsTotalActiveEnergyIgnored)
            {
                IMetric totalActiveEnergyMetric = MetricsHelper.CreateGauge(metricPrefix + "total_active_energy", "Total Active Energy (Wh)", deviceName, 
                    () => device.TotalActiveEnergy.ToString("0.00", CultureInfo.InvariantCulture));
                
                deviceMetrics.Add(totalActiveEnergyMetric);
            }
            
            if (!device.IsTotalActiveEnergyReturnedIgnored)
            {
                IMetric totalActiveEnergyReturnedMetric = MetricsHelper.CreateGauge(metricPrefix + "total_active_energy_returned", "Total Active Energy Returned to the grid (Wh)", deviceName, 
                    () => device.TotalActiveEnergyReturned.ToString("0.00", CultureInfo.InvariantCulture));
                
                deviceMetrics.Add(totalActiveEnergyReturnedMetric);
            }

            if (!device.IsTotalActiveEnergyPhase1Ignored)
            {
                IMetric totalActiveEnergyPhase1Metric = MetricsHelper.CreateGauge(metricPrefix + "total_active_energy_phase_1", "Total Phase 1 Active Energy (Wh)", deviceName, 
                    () => device.TotalActiveEnergyPhase1.ToString("0.00", CultureInfo.InvariantCulture));
                
                deviceMetrics.Add(totalActiveEnergyPhase1Metric);
            }
            
            if (!device.IsTotalActiveEnergyPhase2Ignored)
            {
                IMetric totalActiveEnergyPhase2Metric = MetricsHelper.CreateGauge(metricPrefix + "total_active_energy_phase_2", "Total Phase 2 Active Energy (Wh)", deviceName, 
                    () => device.TotalActiveEnergyPhase2.ToString("0.00", CultureInfo.InvariantCulture));
                
                deviceMetrics.Add(totalActiveEnergyPhase2Metric);
            }
            
            if (!device.IsTotalActiveEnergyPhase3Ignored)
            {
                IMetric totalActiveEnergyPhase3Metric = MetricsHelper.CreateGauge(metricPrefix + "total_active_energy_phase_3", "Total Phase 3 Active Energy (Wh)", deviceName, 
                    () => device.TotalActiveEnergyPhase3.ToString("0.00", CultureInfo.InvariantCulture));
                
                deviceMetrics.Add(totalActiveEnergyPhase3Metric);
            }
            
            if (!device.IsTotalActiveEnergyReturnedPhase1Ignored)
            {
                IMetric totalActiveEnergyReturnedPhase1Metric = MetricsHelper.CreateGauge(metricPrefix + "total_active_energy_returned_phase_1", "Total Phase 1 Active Energy Returned to the grid (Wh)", deviceName, 
                    () => device.TotalActiveEnergyReturnedPhase1.ToString("0.00", CultureInfo.InvariantCulture));
                
                deviceMetrics.Add(totalActiveEnergyReturnedPhase1Metric);
            }
            
            if (!device.IsTotalActiveEnergyReturnedPhase2Ignored)
            {
                IMetric totalActiveEnergyReturnedPhase2Metric = MetricsHelper.CreateGauge(metricPrefix + "total_active_energy_returned_phase_2", "Total Phase 2 Active Energy Returned to the grid (Wh)", deviceName,
                    () => device.TotalActiveEnergyReturnedPhase2.ToString("0.00", CultureInfo.InvariantCulture));
                
                deviceMetrics.Add(totalActiveEnergyReturnedPhase2Metric);
            }
            
            if (!device.IsTotalActiveEnergyReturnedPhase3Ignored)
            {
                IMetric totalActiveEnergyReturnedPhase3Metric = MetricsHelper.CreateGauge(metricPrefix + "total_active_energy_returned_phase_3", "Total Phase 3 Active Energy Returned to the grid (Wh)", deviceName,
                    () => device.TotalActiveEnergyReturnedPhase3.ToString("0.00", CultureInfo.InvariantCulture));
                
                deviceMetrics.Add(totalActiveEnergyReturnedPhase3Metric);
            }
        }
    }
}