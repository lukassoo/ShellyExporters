using System.Net;

namespace Utilities.Networking.RequestHandling;

public class HttpRequestHandler
{
    HttpClient httpClient;
    CredentialCache? credentialCache;

    string targetUrl;
    
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
            Console.WriteLine("[ERR] Exception during http request: " + exception.Message);
            return null;
        }
    }
}