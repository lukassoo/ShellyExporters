using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using Newtonsoft.Json;
using Uri = System.Uri;

namespace Utilities.Networking.RequestHandling.WebSockets;

public class WebSocketHandler
{
    ClientWebSocket webSocket;
    string targetUrl;

    CancellationTokenSource cancellationTokenSource = new();
    byte[] responseBuffer = new byte[1024 * 10];

    readonly RequestObject requestObject;
    string requestJson = null!;
    
    AuthObject? authObject;
    string? password;
    
    public WebSocketHandler(string targetUrl, RequestObject requestObject)
    {
        if (targetUrl.Contains("https"))
        {
            targetUrl = targetUrl.Replace("https", "ws");
        }

        if (targetUrl.Contains("http"))
        {
            targetUrl = targetUrl.Replace("http", "ws");
        }

        if (!targetUrl.Contains("ws"))
        {
            targetUrl = "ws://" + targetUrl;
        }

        this.targetUrl = targetUrl;
        this.requestObject = requestObject;

        UpdateRequestJson();
        
        _ = Connect();
    }

    public void SetAuth(string authPassword)
    {
        password = authPassword;
    }

    public async Task<string?> Request()
    {
        try
        {
            bool isRetrying = false;
            
            start:
            
            bool sendSuccessful = await Send(requestJson);

            if (!sendSuccessful)
            {
                Console.WriteLine("[WRN] Send failed, failing request");
                return null;
            }
            
            WebSocketReceiveResult result = await webSocket.ReceiveAsync(responseBuffer, cancellationTokenSource.Token);

            string responseString = Encoding.UTF8.GetString(responseBuffer, 0, result.Count);

            try
            {
                JsonDocument? jsonDocument = JsonDocument.Parse(responseString);

                if (jsonDocument.RootElement.TryGetProperty("error", out JsonElement errorElement))
                {
                    if (isRetrying)
                    {
                        Console.WriteLine("[ERR] Request error after authentication update");
                        return null;
                    }
                    
                    if (!errorElement.TryGetProperty("code", out JsonElement codeElement))
                    {
                        return responseString;
                    }

                    int errorCode = codeElement.GetInt32();
                    
                    // Only 401 is handled here
                    if (errorCode != 401)
                    {
                        return responseString;
                    }
                    
                    if (!UpdateAuthentication(responseString))
                    {
                        Console.WriteLine("[ERR] Failed to update authentication");
                        return null;
                    }
                    
                    // Retry sending the request
                    isRetrying = true;
                    goto start;
                }
            }
            catch (Exception)
            {
                return responseString;
            }

            return responseString;
        }
        catch (Exception exception)
        {
            Console.WriteLine("[ERR] Exception during web socket request: \n" + exception.Message);
            return null;
        }
    }

    async Task<bool> Connect()
    {
        try
        {
            webSocket = new ClientWebSocket();
            await webSocket.ConnectAsync(new Uri(targetUrl), cancellationTokenSource.Token);

            if (webSocket.State == WebSocketState.Open)
            {
                Console.WriteLine("[INF] Connected to web socket");
                return true;
            }

            Console.WriteLine("[WRN] Failed to connect to web socket");
            return false;
        }
        catch (Exception exception)
        {
            Console.WriteLine("[WRN] Failed to connect to web socket, exception: \n" + exception.Message);
            return false;
        }
    }

    async Task<bool> Send(string message)
    {
        int attempts = 0;
        
        do
        {
            if (attempts >= 3)
            {
                Console.WriteLine("[ERR] Send attempts exhausted, failing send");
                return false;
            }
            attempts += 1;
            
            try
            {
                await webSocket.SendAsync(Encoding.UTF8.GetBytes(message), WebSocketMessageType.Text, true, cancellationTokenSource.Token);
                return true;
            }
            catch (Exception exception)
            {
                Console.WriteLine("[WRN] Request send exception: \n" + exception.Message);
                Console.WriteLine("[INF] Attempting reconnect");
                
                bool connected = await Connect();

                if (!connected)
                {
                    Console.WriteLine("[ERR] Failed to reconnect, failing send");
                    return false;
                }
            }
        } 
        while (true);
    }
    
    bool UpdateAuthentication(string response)
    {
        if (string.IsNullOrEmpty(password))
        {
            Console.WriteLine("[ERR] Cannot authenticate without password");
            return false;
        }
        
        try
        {
            JsonDocument json = JsonDocument.Parse(response);
            JsonElement errorElement = json.RootElement.GetProperty("error");
            JsonDocument messageJson = JsonDocument.Parse(errorElement.GetProperty("message").GetString()!);

            string? realm = messageJson.RootElement.GetProperty("realm").GetString();
            
            authObject = new AuthObject(password!, realm!)
            {
                nonce = messageJson.RootElement.GetProperty("nonce").GetInt32(),
                cnonce = 0,
                nc = messageJson.RootElement.GetProperty("nc").GetInt32()
            };

            UpdateRequestJson();
            return true;
        }
        catch (Exception exception)
        {
            Console.WriteLine("[ERR] Failed to authenticate, exception: " + exception.Message);
            return false;
        }
    }
    
    void UpdateRequestJson()
    {
        if (authObject == null)
        {
            requestJson = JsonConvert.SerializeObject(requestObject);
            return;
        }
        
        requestObject.AuthObject = authObject;
        requestJson = JsonConvert.SerializeObject(requestObject);
    }
}