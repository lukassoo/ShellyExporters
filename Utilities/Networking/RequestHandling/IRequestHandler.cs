namespace Utilities.Networking.RequestHandling;

public interface IRequestHandler
{
    public Task<string?> Request();
}