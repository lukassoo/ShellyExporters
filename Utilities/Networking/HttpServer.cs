using System.Net;
using System.Text;

namespace Utilities.Networking;

public static class HttpServer
{
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
            Console.WriteLine("Using localhost");
            targetHost = "localhost";
        }
        
        string prefix = "http://" + targetHost + ":" + port + "/";

        httpListener.Prefixes.Add(prefix);
        httpListener.Start();
        StartRequestProcessing();
    }

    static void StartRequestProcessing()
    {
        Task.Run(async () =>
        {
            if (responseFunction == null)
            {
                throw new Exception("No response function set, this would result in an empty response, set one before starting to listen for requests with SetResponseFunction()");
            }

            while (true)
            {

                HttpListenerContext httpListenerContext = await httpListener.GetContextAsync();
                HttpListenerResponse response = httpListenerContext.Response;

                try
                {
                    response.OutputStream.Write(Encoding.UTF8.GetBytes(await responseFunction()));
                    response.StatusCode = (int)HttpStatusCode.OK;
                }
                catch (Exception exception)
                {
                    Console.WriteLine(DateTime.UtcNow + " Exception while processing request: " + exception.Message);
                    response.StatusCode = (int)HttpStatusCode.InternalServerError;
                    response.StatusDescription = exception.Message;
                }

                response.Close();
            }
        });
    }
}
