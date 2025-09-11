namespace TDModLoader.Handlers.Http;

using System.Net;
using Debug = System.Diagnostics.Debug;
public class Server: Base
{
    private readonly int _port;
    private readonly HttpListener _listener;
    private readonly Dictionary<string, Base> _handlers;
    public Server(int port = 8080)
    {
        _port = port;
        _listener = new HttpListener();
        _listener.Prefixes.Add($"http://localhost:{_port}/");
        _handlers = new Dictionary<string, Base>
        {
            { "code", new Code() },
            { "files", new Manifest() }
        };
    }

    public async Task StartAsync()
    {
        _listener.Start();
        Console.WriteLine($"JSON RPC Code Evaluator started at http://localhost:{_port}/code/");
        Console.WriteLine($"GET http://localhost:{_port}/ for web interface");

        while (true)
        {
            var context = await _listener.GetContextAsync();
            await HandleRequest(context);
        }
        // ReSharper disable once FunctionNeverReturns
    }

    public override async Task HandleRequest(HttpListenerContext context)
    {
        var request = context.Request;
        var response = context.Response;

        try
        {
            Console.WriteLine($"Request: {request.HttpMethod} {request.Url}");
            var segments = request.Url!.Segments;
            var handlerId = segments.Length < 2
                ? "files"
                : segments[1].Trim('/');
            var handler = _handlers.ContainsKey(handlerId) ? _handlers[handlerId] : _handlers["files"];
            await handler.HandleRequest(context);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error handling request: {ex.Message}");
            await SendJsonResponse(response, new { error = ex.Message }, HttpStatusCode.InternalServerError);
        }
    }

    public void Stop() => _listener.Stop();
    
}
