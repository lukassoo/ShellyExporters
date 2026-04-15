using Serilog;

namespace Utilities.Logging;

public static class LogAdditions
{
    public static void CheckExceptionLogIndexError(Exception exception, ILogger log)
    {
        if (exception is KeyNotFoundException or IndexOutOfRangeException)
        {
            log.Error("\n--------\n" +
                      "Detected key not found or index out of range exception\n" +
                      "\n" +
                      "Meter indexing was incorrect and inconsistent for some time/versions of exporters.\n" +
                      "Please make sure your config is indexing meters from 0 and doesn't go above the number of meters available.\n" +
                      "(For example, with 3 meters, this means indexes 0, 1, 2)\n" +
                      "--------"
            );
        }
    }

    public static void LogEnergyMeterMetricChanges(ILogger log)
    {
        log.Warning("\n--------\n" +
                    "WARNING! Some metric names have changed, and some may no longer exist! (Change date: 2026-04-15)\n" +
                    "\n" +
                    "Earlier versions of this exporter had some metrics named incorrectly and exposed with the wrong type, and some metrics should not have been exposed at all.\n" +
                    "The change affects \"total\" metrics such as \"shelly_energy_total_wh\" now being called \"shelly_energy_wh_total\".\n" +
                    "Totals, sums, and counts have suffixes that must be at the end of the name, right after the unit.\n" +
                    "These are also called \"Counters\" which can only increment and reset down to 0. (Unlike Gauges which can change arbitrarily)\n" +
                    "Some previously existing total metrics were not valid \"total\" metrics, as they were not Counters but temporary sums of other metrics.\n" +
                    "These metrics have been removed. (These were things like a sum of current or power across all phases)\n" +
                    "If you were using these metrics, please calculate them using the individual phase metrics.\n" +
                    "\n" +
                    "These changes affect all EM (Energy Meter) exporters, but some don't have multiple phases and thus don't have totals to remove.\n" +
                    "If you just updated and this is the first time you are seeing this, please check your dashboards and update the metric names\n" +
                    "I'm sorry for the inconvenience.\n" +
                    "I will try to be more mindful of correct metric names in the future.\n" +
                    "--------"
                    );
    }
}