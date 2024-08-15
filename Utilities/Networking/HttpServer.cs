using System.Net;
using System.Text;
using Serilog;

namespace Utilities.Networking;

public static class HttpServer
{
    static ILogger log = Log.ForContext(typeof(HttpServer));
    
    static HttpListener httpListener;
    static Func<Task<string>>? responseFunction;

    static HttpServer()
    {
        if (!HttpListener.IsSupported)
        {
            throw new Exception("The HttpAdditions Server is somehow unsupported, read up what C# HttpListener supports");
        }

        httpListener = new HttpListener();
    }

    public static void SetResponseFunction(Func<Task<string>> responseFunctionRef)
    {
        responseFunction = responseFunctionRef;
    }

    public static void ListenOnPort(int port)
    {
        string targetHost = "+";

        string[] commandLineArgs = Environment.GetCommandLineArgs();
        if (commandLineArgs.Length > 1 && commandLineArgs[1] == "localhost")
        {
            log.Information("Using localhost");
            targetHost = "localhost";
        }
        
        string prefix = "http://" + targetHost + ":" + port + "/";

        log.Information("Will be listening on: {url}", prefix);
        
        httpListener.Prefixes.Add(prefix);
        httpListener.Start();
        
        log.Information("Listening started");
        
        StartRequestProcessing();
    }

    static async void StartRequestProcessing()
    {
        if (responseFunction == null)
        {
            throw new Exception("No response function set, this would result in an empty response, set one before starting to listen for requests with SetResponseFunction()");
        }

        while (true)
        {
            HttpListenerResponse? response = null;
            
            try
            {
                HttpListenerContext httpListenerContext = await httpListener.GetContextAsync();
                response = httpListenerContext.Response;
                log.Debug("Starting request handling");
                
                log.Debug("Getting response");
                string responseString = await responseFunction();
                log.Debug("Response received");

                if (string.IsNullOrEmpty(responseString))
                {
                    log.Error("Detected empty response - responding with error 500");
                    response.StatusCode = (int)HttpStatusCode.InternalServerError;
                }
                else
                {
                    log.Debug("Writing response");
                    await response.OutputStream.WriteAsync(Encoding.UTF8.GetBytes(responseString));
                    log.Debug("Response written");
                    
                    log.Debug("Setting OK status (200)");
                    response.StatusCode = (int)HttpStatusCode.OK;
                }
            }
            catch (Exception exception)
            {
                log.Error(exception, "Exception while handling request");

                if (response != null)
                {
                    log.Debug("Setting error status code and description");

                    try
                    {
                        response.StatusCode = (int)HttpStatusCode.InternalServerError;
                        response.StatusDescription = exception.Message;
                    }
                    catch (Exception exception2)
                    {
                        log.Error(exception2, "Failed to set status code or description");
                    }
                }
                else
                {
                    log.Debug("Response is null - cannot set error status code and description");
                }
            }

            if (response != null)
            {
                log.Debug("Closing response");
                response.Close();
            }
            else
            {
                log.Debug("No response to close");
            }
            
            log.Debug("Request handling ended");
        }
    }
}
