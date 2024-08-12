using Serilog;
using Utilities.Configs;
using Utilities.Logging;

namespace Utilities;

public static class RuntimeAutomation
{
    static ILogger log = null!;
    
    static readonly TaskCompletionSource shutdownCompletionSource = new();

    static bool shuttingDown;
    
    public static void Init<T>(Config<T> config)
    {
        LogSystem.Init(config.logToFile, config.logLevel);
        log = Log.Logger.ForContext(typeof(RuntimeAutomation));
        
        log.Information("------------- Start -------------");
        
        Console.CancelKeyPress += (_, args) => { args.Cancel = true; Shutdown("Process cancelled"); };
        AppDomain.CurrentDomain.ProcessExit += (_, _) => { Shutdown("Received process exit"); };
    }

    public static Task WaitForShutdown()
    {
        return shutdownCompletionSource.Task;
    }

    public static void Shutdown(string? reason = null)
    {
        if (shuttingDown) return;
        shuttingDown = true;
        
        log.Information("------------- Shutdown -------------");
        
        if (!string.IsNullOrEmpty(reason))
        {
            log.Information("Shutdown reason: {reason}", reason);
        }
        
        Log.CloseAndFlush();
        shutdownCompletionSource.TrySetResult();
    }
}