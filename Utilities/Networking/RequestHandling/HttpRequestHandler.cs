using System.Net;
using Serilog;

namespace Utilities.Networking.RequestHandling;

public class HttpRequestHandler
{
    static readonly ILogger log = Log.ForContext<HttpRequestHandler>();

    readonly HttpClient httpClient;
    readonly CredentialCache? credentialCache;
    
    readonly string targetUrl;
    
    public HttpRequestHandler(string targetUrl, bool useAuthentication)
    {
        this.targetUrl = HttpAdditions.GetFinalUrl(targetUrl);
        
        if (useAuthentication)
        {
            HttpAdditions.GetClientWithCredentialCache(out httpClient, out credentialCache);
        }
        else
        {
            HttpAdditions.GetClient(out httpClient);
        }
    }

    public void SetAuth(string username, string password)
    {
        credentialCache?.AddCredentials(targetUrl,username, password);
    }
    
    public async Task<string?> Request()
    {
        try
        {
            return await httpClient.GetStringAsync(targetUrl);
        }
        catch (Exception exception)
        {
            log.Error(exception, "Exception during http request");
            return null;
        }
    }
}