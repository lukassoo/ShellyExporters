using System.Net;

namespace Utilities;

public static class HttpClient
{
    static CredentialCache credentialCache = new CredentialCache();

    static System.Net.Http.HttpClient httpClient;

    static HttpClient()
    {
        HttpClientHandler handler = new HttpClientHandler();
        handler.Credentials = credentialCache;

        httpClient = new System.Net.Http.HttpClient(handler);
    }

    public static void AddCredentials(string url, string username, string password)
    {
        credentialCache.Add(new Uri(GetFinalUrl(url)), "Basic", new NetworkCredential(username, password));
    }

    public static void RemoveCredentials(string url)
    {
        credentialCache.Remove(new Uri(GetFinalUrl(url)), "Basic");
    }

    public static async Task<string> GetRequestString(string url)
    {
        return await httpClient.GetStringAsync(GetFinalUrl(url));
    }

    static string GetFinalUrl(string url)
    {
        return url.Contains("http") ? url : ("http://" + url);
    }
}