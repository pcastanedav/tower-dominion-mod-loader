namespace TDModLoader.Handlers.Http;

using System.Net;
using System.Text;
using System.Text.Json;

public abstract class Base
{
    public virtual async Task HandleRequest(HttpListenerContext context)
    {
        await (context.Request.HttpMethod switch
        {
            "GET" => HandleGet(context),
            "POST" => HandlePost(context),
            _ => SendJsonResponse(context.Response, new { error = "Only GET and POST methods supported" },
                HttpStatusCode.MethodNotAllowed)
        });
    }
    protected virtual Task HandlePost(HttpListenerContext context)
    {
        throw new NotImplementedException();
    }

    protected virtual Task HandleGet(HttpListenerContext context)
    {
        throw new NotImplementedException();
    }

    protected static Task SendJsonResponse(HttpListenerResponse response, object data, HttpStatusCode statusCode = HttpStatusCode.OK)
    {
        var options = new JsonSerializerOptions { WriteIndented = true };
        var content = JsonSerializer.SerializeToUtf8Bytes(data, options);
        return SendStream(
            response,
            new MemoryStream(content),
            new BaseResponseHeaders() { ContentType = "application/json", StatusCode = statusCode }
        );
    }
    protected static Task SendHtmlResponse(HttpListenerResponse response, string html)
    {
        return SendStream(
            response,
            new MemoryStream(Encoding.UTF8.GetBytes((html))),
            new BaseResponseHeaders() { ContentType = "text/html" }
        );
    }
    protected static async Task SendStream(HttpListenerResponse response, Stream stream, BaseResponseHeaders headers)
    {
        response.ContentType = headers.ContentType;
        if (stream.CanSeek)
        {
            response.ContentLength64 = stream.Length;
        }
        response.StatusCode = (int) headers.StatusCode;
        await stream.CopyToAsync(response.OutputStream);
        response.OutputStream.Close();
    }
    protected record struct BaseResponseHeaders()
    {
        public string ContentType = "application/octet-stream";
        public HttpStatusCode StatusCode  = HttpStatusCode.OK;
    }

}