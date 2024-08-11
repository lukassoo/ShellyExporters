using Serilog;
using Serilog.Core;
using Serilog.Events;

namespace Utilities.Logging;

public static class LogSystem
{
    const string logTemplate = "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz}][{Level:u3}][{Properties:j}] {Message:lj}{NewLine}{Exception}";
    const string logPath = "../Logs/log.txt";
    
    public static void Init(bool logToFile, string logLevel)
    {
        LoggerConfiguration loggerConfig = new();
        loggerConfig.MinimumLevel.Verbose();
        
        if (!Enum.TryParse(logLevel, out LogEventLevel logLevelEnum))
        {
            Console.WriteLine("WARNING: Failed to parse log level, setting to \"Information\"");
            logLevelEnum = LogEventLevel.Information;
        }
        
        LoggingLevelSwitch levelSwitch = new()
        {
            MinimumLevel = logLevelEnum
        };

        loggerConfig.WriteTo.Console(levelSwitch: levelSwitch, outputTemplate: logTemplate);
        
        if (logToFile)
        {
            loggerConfig.WriteTo.File(logPath, levelSwitch: levelSwitch, rollingInterval: RollingInterval.Day, outputTemplate: logTemplate);
        }
        
        Log.Logger = loggerConfig.CreateLogger();
    }
}