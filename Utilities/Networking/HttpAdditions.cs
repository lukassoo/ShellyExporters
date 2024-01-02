using System.Net;

namespace Utilities.Networking;

public static class HttpAdditions
{
    public static void GetClient(out HttpClient httpClient)
    {
        httpClient = new HttpClient();
        httpClient.Timeout = TimeSpan.FromSeconds(15);
    }
    
    public static void GetClientWithCredentialCache(out HttpClient httpClient, out CredentialCache credentialCache)
    {
        credentialCache = new();
        
        HttpClientHandler handler = new();
        handler.Credentials = credentialCache;

        httpClient = new HttpClient(handler);
        httpClient.Timeout = TimeSpan.FromSeconds(15);
    }

    public static async Task<string> GetRequestString(this HttpClient httpClient, string url)
    {
        return await httpClient.GetStringAsync(GetFinalUrl(url));
    }
    
    public static string GetFinalUrl(string url)
    {
        return url.Contains("http") ? url : "http://" + url;
    }

    public static void AddCredentials(this CredentialCache credentialCache, string url, string username, string password)
    {
        credentialCache.Add(new Uri(GetFinalUrl(url)), "Basic", new NetworkCredential(username, password));
    }
    
    public static void RemoveCredentials(this CredentialCache credentialCache, string url)
    {
        credentialCache.Remove(new Uri(GetFinalUrl(url)), "Basic");
    }
}