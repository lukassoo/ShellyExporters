using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using Newtonsoft.Json;
using Serilog;
using Uri = System.Uri;

namespace Utilities.Networking.RequestHandling.WebSockets;

public class WebSocketHandler
{
    static ILogger log = Log.ForContext(typeof(WebSocketHandler));
    
    ClientWebSocket? webSocket;
    string targetUrl;

    CancellationTokenSource cancellationTokenSource = new();
    byte[] responseBuffer = new byte[1024 * 10];

    readonly TimeSpan requestTimeoutTime;
    readonly RequestObject requestObject;
    string requestJson = null!;
    
    AuthObject? authObject;
    string? password;

    bool isConnecting;
    
    public WebSocketHandler(string targetUrl, RequestObject requestObject, TimeSpan requestTimeoutTime)
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
        this.requestTimeoutTime = requestTimeoutTime;

        UpdateRequestJson();
        
        Connect().Wait();
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
                log.Warning("Send failed, failing request" + requestJson);
                return null;
            }

            CancellationTokenSource tempCancellationSource = new();
            tempCancellationSource.CancelAfter(requestTimeoutTime);

            WebSocketReceiveResult result;
            
            try
            {
                log.Verbose("Receiving");
                result = await webSocket!.ReceiveAsync(responseBuffer, tempCancellationSource.Token);
                log.Verbose("Receiving completed");
            }
            catch (TaskCanceledException)
            {
                log.Warning("Request receive timeout");
                return null;
            }

            string responseString = Encoding.UTF8.GetString(responseBuffer, 0, result.Count);

            try
            {
                JsonDocument jsonDocument = JsonDocument.Parse(responseString);

                if (jsonDocument.RootElement.TryGetProperty("error", out JsonElement errorElement))
                {
                    if (isRetrying)
                    {
                        log.Error("Response contains error right after authentication update - something went wrong, response:\n{response}", responseString);
                        return null;
                    }
                    
                    log.Information("Response contains \"error\" property");
                    
                    if (!errorElement.TryGetProperty("code", out JsonElement codeElement))
                    {
                        log.Error("Response error does not contain \"code\" property - unknown error, response:\n{response}", responseString);
                        return null;
                    }

                    int errorCode = codeElement.GetInt32();
                    
                    // Only 401 is handled here
                    if (errorCode != 401)
                    {
                        log.Error("Unhandled error code: \"{errorCode}\" in response:\n{response}", errorCode, responseString);
                        return null;
                    }
                    
                    log.Information("Updating authentication");
                    
                    if (!UpdateAuthentication(responseString))
                    {
                        log.Error("Failed to update authentication");
                        return null;
                    }
                    
                    log.Information("Authentication updated, retrying request");
                    
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
            log.Error(exception, "Exception during web socket request");
            return null;
        }
    }

    async Task<bool> Connect()
    {
        try
        {
            if (isConnecting)
            {
                log.Warning("Already trying to connect - ignoring connect call");
                return false;
            }
            isConnecting = true;
            
            log.Information("Trying to connect web socket...");

            webSocket?.Dispose();
            webSocket = null;
            
            ClientWebSocket tempWebSocket = new();
            
            // TODO: Set a shorter web socket timeout using the builtin way once it is possible in .NET 9: https://github.com/dotnet/runtime/issues/48729

            CancellationTokenSource tempCancellationSource = new();
            tempCancellationSource.CancelAfter(TimeSpan.FromSeconds(3));
            
            await tempWebSocket.ConnectAsync(new Uri(targetUrl), tempCancellationSource.Token);

            if (tempWebSocket.State == WebSocketState.Open)
            {
                log.Information("Connected web socket");

                webSocket = tempWebSocket;
                isConnecting = false;
                return true;
            }

            log.Warning("Failed to connect web socket");
            tempWebSocket.Dispose();
            isConnecting = false;
            return false;
        }
        catch (Exception exception)
        {
            if (exception is TaskCanceledException)
            {
                log.Error("Web socket connection timeout");
            }
            else
            {
                log.Error(exception, "Failed to connect to web socket at " + targetUrl);
            }
            
            isConnecting = false;
            return false;
        }
    }

    async Task<bool> Send(string message)
    {
        int attempts = 0;

        if (webSocket == null)
        {
            log.Error("Can not send on null web socket");

            if (!isConnecting)
            {
                log.Information("Triggering reconnection attempt");
                _ = Connect();
            }
            
            return false;
        }
        
        do
        {
            if (attempts >= 3)
            {
                log.Error("Send attempts exhausted, failing send");
                return false;
            }
            attempts += 1;
            
            try
            {
                try
                {
                    CancellationTokenSource tempCancellationSource = new();
                    tempCancellationSource.CancelAfter(requestTimeoutTime);
                    
                    log.Verbose("Sending");
                    await webSocket.SendAsync(Encoding.UTF8.GetBytes(message), WebSocketMessageType.Text, true, cancellationTokenSource.Token);
                    log.Verbose("Sending completed");
                    
                    return true;
                }
                catch (TaskCanceledException)
                {
                    log.Warning("Send timeout");
                    return false;
                }
            }
            catch (Exception exception)
            {
                log.Warning(exception, "Send exception");
                
                log.Information("Attempting reconnect");
                bool connected = await Connect();

                if (!connected)
                {
                    log.Error("Failed to reconnect, failing send");
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
            log.Error("Cannot authenticate without password");
            return false;
        }
        
        try
        {
            log.Debug("Updating authentication form response:\n{response}", response);
            
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
            
            log.Debug("Updated serialized request:\n{request}", requestJson);
            return true;
        }
        catch (Exception exception)
        {
            log.Error(exception, "Failed to update authentication");
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