using Prometheus;
using Serilog;

namespace Utilities.Metrics;

public static class MetricsServer
{
    static readonly ILogger log = Log.Logger.ForContext(typeof(MetricsServer));
    
    static MetricServer? metricServer;
    
    public static bool Start(ushort port, Func<CancellationToken, Task> scrapeCallback)
    {
        // Suppress all default metrics, we only want exporter-specific device metrics
        SuppressDefaultMetricOptions suppressOptions = new()
        {
            SuppressEventCounters = true,
            SuppressDebugMetrics = true,
            SuppressProcessMetrics = true,
            SuppressMeters = true
        };

        Prometheus.Metrics.SuppressDefaultMetrics(suppressOptions);
        Prometheus.Metrics.DefaultRegistry.AddBeforeCollectCallback(scrapeCallback);
        
        string targetHost = "+";

        string[] commandLineArgs = Environment.GetCommandLineArgs();
        if (commandLineArgs.Length > 1 && commandLineArgs[1] == "localhost")
        {
            log.Information("Using localhost");
            targetHost = "localhost";
        }
        
        metricServer = new MetricServer(targetHost, port, url: "metrics/");

        try
        {
            metricServer.Start();
            log.Information("Started metric server on port {port}, path: {path}", port, "/metrics");
            return true;
        }
        catch (Exception exception)
        {
            log.Error(exception, "Failed to start metric server");
            log.Information("If the exception above is related to access denied or something else with permissions - " +
                            "you are probably trying to start this on a Windows machine.\n" +
                            "It won't let you do it without some special permission as this program will try to listen for all requests.\n" +
                            "This program was designed to run as a Docker container where this problem does not occur\n" +
                            "If you really want to launch it anyways but only for local testing you can launch with the \"localhost\" argument\n" + 
                            "(Make a shortcut to this program, open its properties window and in the \"Target\" section add \"localhost\" after a space at the end)");

            return false;
        }
    }
}