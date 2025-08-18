namespace Utilities.Networking;

public interface IDeviceConnection
{
    public string GetTargetName();
    public Task<bool> UpdateMetricsIfNecessary();
}