﻿using System.Globalization;
using Serilog;
using Utilities;
using Utilities.Configs;
using Utilities.Metrics;
using Utilities.Networking;

namespace ShellyPlus1PmExporter;

internal static class Program
{
    static ILogger log = null!;
    
    const string configName = "shellyPlus1PmExporter";
    const int port = 10022;
    
    static readonly Dictionary<ShellyPlus1PmConnection, List<GaugeMetric>> deviceToMetricsDictionary = new(1);
    
    static async Task Main()
    {
        try
        {
            if (!ConfigHelper.LoadAndUpdateConfig(configName, WriteExampleConfig, out Config<TargetDevice>? config))
            {
                Console.WriteLine("[ERROR] Could not load config - returning");
                return;
            }
            
            RuntimeAutomation.Init(config);
            log = Log.ForContext(typeof(Program));
            
            SetupDevicesFromConfig(config);
            SetupMetrics();
            StartMetricsServer();
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
            Config<TargetDevice> config = new();

            config.targets.Add(new TargetDevice("Your Name for the device",
                "Address (usually 192.168.X.X - the IP of your device)",
                "Password (leave empty if not used)"));

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
        log.Information("Setting up Shelly Plus 1 PM Connections from Config...");

        foreach (TargetDevice target in config.targets)
        {
            log.Information("Setting up: {targetName} at: {url} requires auth: {requiresAuth}", target.name, target.url, target.RequiresAuthentication());
            deviceToMetricsDictionary.Add(new ShellyPlus1PmConnection(target), []);
        }
    }

    static void SetupMetrics()
    {
        log.Information("Setting up metrics");

        foreach ((ShellyPlus1PmConnection device, List<GaugeMetric> deviceMetrics) in deviceToMetricsDictionary)
        {
            if (!device.IgnoreTotalPower)
            {
                deviceMetrics.Add(new GaugeMetric("shellyplus1pm_" + device.GetTargetName() + "_total_power", 
                                                "The total power/energy consumed in Watt-hours",
                                                () => device.TotalPower.ToString("0.00", CultureInfo.InvariantCulture)));
            }
            
            if (!device.IgnoreTotalPowerReturned)
            {
                deviceMetrics.Add(new GaugeMetric("shellyplus1pm_" + device.GetTargetName() + "_total_power_returned", 
                    "The total power/energy returned in Watt-hours",
                    () => device.TotalPowerReturned.ToString("0.00", CultureInfo.InvariantCulture)));
            }
            
            if (!device.IgnoreCurrentPower)
            {
                deviceMetrics.Add(new GaugeMetric("shellyplus1pm_" + device.GetTargetName() + "_currently_used_power", 
                                                "The amount of power currently flowing in watts",
                                                () => device.CurrentlyUsedPower.ToString("0.00", CultureInfo.InvariantCulture)));
            }

            if (!device.IgnoreVoltage)
            {
                deviceMetrics.Add(new GaugeMetric("shellyplus1pm_" + device.GetTargetName() + "_voltage",
                                                "The current voltage in volts",
                                                () => device.Voltage.ToString("0.00", CultureInfo.InvariantCulture)));
            }
            
            if (!device.IgnoreCurrent)
            {
                deviceMetrics.Add(new GaugeMetric("shellyplus1pm_" + device.GetTargetName() + "_current",
                                                "The currently flowing current in amperes",
                                                () => device.Current.ToString("0.00", CultureInfo.InvariantCulture)));
            }
            
            if (!device.IgnoreTemperature)
            {
                deviceMetrics.Add(new GaugeMetric("shellyplus1pm_" + device.GetTargetName() + "_temperature",
                                                "The internal device temperature in Celsius",
                                                () => device.Temperature.ToString("0.00", CultureInfo.InvariantCulture)));
            }

            if (!device.IgnorePowerFactor)
            {
                deviceMetrics.Add(new GaugeMetric("shellyplus1pm_" + device.GetTargetName() + "_power_factor",
                                                "The current power factor",
                                                () => device.PowerFactor.ToString("0.00", CultureInfo.InvariantCulture)));
            }

            if (!device.IgnoreFrequency)
            {
                deviceMetrics.Add(new GaugeMetric("shellyplus1pm_" + device.GetTargetName() + "_frequency",
                                                "The current network frequency",
                                                () => device.Frequency.ToString("0.00", CultureInfo.InvariantCulture)));
            }

            if (!device.IgnoreOutputState)
            {
                deviceMetrics.Add(new GaugeMetric("shellyplus1pm_" + device.GetTargetName() + "_relay_state",
                                                "The state of the output",
                                                () => device.OutputState ? "1" : "0"));
            }

            if (!device.IgnoreInputState)
            {
                deviceMetrics.Add(new GaugeMetric("shellyplus1pm_" + device.GetTargetName() + "_input_state",
                                                "The state of the input",
                                                () => device.InputState ? "1" : "0"));
            }
            
            if (!device.IgnoreInputPercent)
            {
                deviceMetrics.Add(new GaugeMetric("shellyplus1pm_" + device.GetTargetName() + "_input_percent",
                                                "Input analog value in percent",
                                                () => device.InputPercent.ToString("0.00", CultureInfo.InvariantCulture)));
            }
            
            if (!device.IgnoreInputCountTotal)
            {
                deviceMetrics.Add(new GaugeMetric("shellyplus1pm_" + device.GetTargetName() + "_input_count",
                    "Total pulses counted on the input",
                    () => device.InputCountTotal.ToString("D", CultureInfo.InvariantCulture)));
            }
            
            if (!device.IgnoreInputFrequency)
            {
                deviceMetrics.Add(new GaugeMetric("shellyplus1pm_" + device.GetTargetName() + "_input_frequency",
                    "Network frequency on the input",
                    () => device.InputFrequency.ToString("0.00", CultureInfo.InvariantCulture)));
            }
        }
    }

    static void StartMetricsServer()
    {
        log.Information("Starting metrics server on port: {port}", port);

        HttpServer.SetResponseFunction(CollectAllMetrics);

        try
        {
            HttpServer.ListenOnPort(port);
        }
        catch (Exception exception)
        {
            log.Information("");
            log.Information("If the exception below is related to access denied or something else with permissions - " +
                            "you are probably trying to start this on a Windows machine.\n" +
                            "It won't let you do it without some special permission as this program will try to listen for all requests.\n" +
                            "This program was designed to run as a Docker container where this problem does not occur\n" +
                            "If you really want to launch it anyways but only for local testing you can launch with the \"localhost\" argument" + 
                            "(Make a shortcut to this program, open its properties window and in the \"Target\" section add \"localhost\" after a space at the end)");
            log.Information("");
            log.Information("The exception: " + exception.Message);
            throw;
        }

        log.Information("Server started");
    }

    static async Task<string> CollectAllMetrics()
    {
        string allMetrics = "";

        foreach ((ShellyPlus1PmConnection device, List<GaugeMetric> deviceMetrics) in deviceToMetricsDictionary)
        {
            if (!await device.UpdateMetricsIfNecessary())
            {
                log.Error("Failed to update metrics for target device: {targetName}", device.GetTargetName());
                continue;
            }
            
            foreach (GaugeMetric metric in deviceMetrics)
            {
                allMetrics += metric.GetMetric();
            }
        }

        return allMetrics;
    }
}