﻿using System.Globalization;
using Serilog;
using Utilities;
using Utilities.Configs;
using Utilities.Metrics;
using Utilities.Networking;

namespace ShellyPlusPlugExporter;

internal static class Program
{
    static ILogger log = null!;
    
    const string configName = "shellyPlusPlugExporter";
    const int port = 10009;
    
    static readonly Dictionary<ShellyPlusPlugConnection, List<GaugeMetric>> deviceToMetricsDictionary = new(1);
    
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
        log.Information("Setting up Shelly Plus Plug Connections from Config...");

        foreach (TargetDevice target in config.targets)
        {
            log.Information("Setting up: {targetName} at: {url} requires auth: {requiresAuth}", target.name, target.url, target.RequiresAuthentication());
            deviceToMetricsDictionary.Add(new ShellyPlusPlugConnection(target), []);
        }
    }

    static void SetupMetrics()
    {
        log.Information("Setting up metrics");

        foreach ((ShellyPlusPlugConnection device, List<GaugeMetric> deviceMetrics) in deviceToMetricsDictionary)
        {
            if (!device.IgnoreTotalPower)
            {
                deviceMetrics.Add(new GaugeMetric("shellyplusplug_" + device.GetTargetName() + "_total_power", 
                                                "The total power/energy consumed through the plug in Watt-hours",
                                                () => device.TotalPower.ToString("0.00", CultureInfo.InvariantCulture)));
            }
            
            if (!device.IgnoreCurrentPower)
            {
                deviceMetrics.Add(new GaugeMetric("shellyplusplug_" + device.GetTargetName() + "_currently_used_power", 
                                                "The amount of power currently flowing through the plug in watts",
                                                () => device.CurrentlyUsedPower.ToString("0.00", CultureInfo.InvariantCulture)));
            }

            if (!device.IgnoreVoltage)
            {
                deviceMetrics.Add(new GaugeMetric("shellyplusplug_" + device.GetTargetName() + "_voltage",
                                                "The current voltage at the plug in volts",
                                                () => device.Voltage.ToString("0.00", CultureInfo.InvariantCulture)));
            }
            
            if (!device.IgnoreCurrent)
            {
                deviceMetrics.Add(new GaugeMetric("shellyplusplug_" + device.GetTargetName() + "_current",
                                                "The current flowing through the plug in amperes",
                                                () => device.Current.ToString("0.00", CultureInfo.InvariantCulture)));
            }
            
            if (!device.IgnoreTemperature)
            {
                deviceMetrics.Add(new GaugeMetric("shellyplusplug_" + device.GetTargetName() + "_temperature",
                                                "The internal device temperature in Celsius",
                                                () => device.Temperature.ToString("0.00", CultureInfo.InvariantCulture)));
            }

            if (!device.IgnoreRelayState)
            {
                deviceMetrics.Add(new GaugeMetric("shellyplusplug_" + device.GetTargetName() + "_relay_state",
                                                "The state of the relay",
                                                () => device.RelayStatus ? "1" : "0"));
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

        foreach ((ShellyPlusPlugConnection device, List<GaugeMetric> deviceMetrics) in deviceToMetricsDictionary)
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