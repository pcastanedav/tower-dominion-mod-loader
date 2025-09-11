
namespace TDModLoader.Handlers.Http;

using TDModLoader.Utils;

using System.Net;

public class Manifest: Base
{

    protected override Task HandleGet(HttpListenerContext context)
    {
        var url = context.Request.Url!.AbsolutePath == "/" ? new Uri("/Resources/runner.html") : context.Request.Url!;
        try
        {
            var manifest = TDModLoader.Utils.Manifest.GetResource(url);
            return SendStream(
                context.Response,
                manifest.Stream,
                new BaseResponseHeaders() { ContentType = manifest.ContentType }
            );           
        } catch (FileNotFoundException ex)
        {
            return SendJsonResponse(context.Response, new { error = ex.Message }, HttpStatusCode.NotFound);
        }

    }
    
}