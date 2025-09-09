namespace TDModLoader.Handlers.Http;

using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;

using Utils;
public class Code : Base
{
    public override async Task HandleRequest(HttpListenerContext context)
    {
        await (context.Request.HttpMethod switch
        {
            "GET" => HandleGet(context),
            "POST" => HandlePost(context),
            _ => SendJsonResponse(context.Response, new { error = "Only GET and POST methods supported" },
                (int)HttpStatusCode.MethodNotAllowed)
        });
    }

    private static Task HandleGet(HttpListenerContext context)
    {
        return SendJsonResponse(context.Response, new { success = true, message = "Soon" }, (int) HttpStatusCode.OK);
    }
    private static async Task HandlePost(HttpListenerContext context)
    {
        string requestBody;
        using (var reader = new StreamReader(context.Request.InputStream)) { requestBody = await reader.ReadToEndAsync(); }

        var requestData = JsonSerializer.Deserialize<CodeRequest>(requestBody, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        if (string.IsNullOrEmpty(requestData?.Code))
        {
            await SendJsonResponse(context.Response, new { success = false, error = "Code field is required" }, (int) HttpStatusCode.BadRequest);
            return;
        }

        var result = CodeEvaluator.Execute(requestData.Code);
        await SendJsonResponse(context.Response, result, (int) HttpStatusCode.OK);
    }

    internal class CodeRequest
    {
        [JsonPropertyName("code")]
        public string Code { get; init; } = "";
    }
}
