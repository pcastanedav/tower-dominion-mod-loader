namespace TDModLoader.Handlers.Http;

using System.Net;
using System.Text;
using System.Text.Json;

public abstract class Base
{
    public abstract Task HandleRequest(HttpListenerContext context);
    protected static async Task SendJsonResponse(HttpListenerResponse response, object data, int statusCode)
    {
        var json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
        var buffer = Encoding.UTF8.GetBytes(json);

        response.ContentType = "application/json";
        response.StatusCode = statusCode;
        response.ContentLength64 = buffer.Length;

        await response.OutputStream.WriteAsync(buffer);
        response.OutputStream.Close();
    }
    
    protected static async Task SendHtmlResponse(HttpListenerContext context, string html)
    {
        var response = context.Response;
        var buffer = Encoding.UTF8.GetBytes(html);
        response.ContentType = "text/html";
        response.StatusCode = 200;
        response.ContentLength64 = buffer.Length;
        await response.OutputStream.WriteAsync(buffer);
        response.OutputStream.Close();
    }

}