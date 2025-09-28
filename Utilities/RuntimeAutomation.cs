using NuGet.Versioning;
using Serilog;
using Utilities.Configs;
using Utilities.Logging;

namespace Utilities;

public static class RuntimeAutomation
{
    static ILogger log = null!;
    
    static readonly TaskCompletionSource shutdownCompletionSource = new();

    public static bool ShuttingDown { get; private set; }
    
    public static void Init<T>(Config<T> config, SemanticVersion currentVersion, DateTime buildTime)
    {
        LogSystem.Init(config.logToFile, config.logLevel);
        log = Log.Logger.ForContext(typeof(RuntimeAutomation));
        
        log.Information("------------- Starting -------------");
        log.Information("Version: {version}", currentVersion.ToString());
        log.Information("Build time: {buildTime}", buildTime.ToString("yyyy-MM-dd HH:mm:ss") + " UTC");
        
        Console.CancelKeyPress += (_, args) => { args.Cancel = true; Shutdown("Process cancelled"); };
        AppDomain.CurrentDomain.ProcessExit += (_, _) => { Shutdown("Received process exit"); };
    }

    public static Task WaitForShutdown()
    {
        return shutdownCompletionSource.Task;
    }

    public static void Shutdown(string? reason = null)
    {
        if (ShuttingDown) return;
        ShuttingDown = true;
        
        log.Information("------------- Shutdown -------------");
        
        if (!string.IsNullOrEmpty(reason))
        {
            log.Information("Shutdown reason: {reason}", reason);
        }
        
        Log.CloseAndFlush();
        shutdownCompletionSource.TrySetResult();
    }
}