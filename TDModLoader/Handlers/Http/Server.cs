using System.Net;
using System.Text;
using MelonLoader;

namespace TDModLoader.Handlers.Http;

public class Server
{
    private readonly HttpListener _listener;
    private readonly int _port;
    
    public Server(int port = 8080)
    {
        _port = port;
        _listener = new HttpListener();
        _listener.Prefixes.Add($"http://localhost:{_port}/");
    }
    
    public async Task StartAsync()
    {
        _listener.Start();
        MelonLogger.Msg($"Server started at http://localhost:{_port}/");
        
        while (true)
        {
            var context = await _listener.GetContextAsync();
            var response = context.Response;
            
            var responseString = "<html><body><h1>Hello World!</h1></body></html>";
            var buffer = Encoding.UTF8.GetBytes(responseString);
            
            response.ContentLength64 = buffer.Length;
            response.ContentType = "text/html";
            response.StatusCode = 200;
            
            await response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
            response.OutputStream.Close();
        }
    }
    
    public void Stop()
    {
        _listener.Stop();
        MelonLogger.Msg($"Server stopped on port {_port}");
    }
}