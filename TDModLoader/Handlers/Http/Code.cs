namespace TDModLoader.Handlers.Http;

using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using TDModLoader.Utils;

using Utils;
public class Code : Base
{
    protected override Task HandleGet(HttpListenerContext context)
    {
        return SendJsonResponse(context.Response, new { success = true, message = "Soon" });
    }
    protected override async Task HandlePost(HttpListenerContext context)
    {
        string requestBody;
        using (var reader = new StreamReader(context.Request.InputStream)) { requestBody = await reader.ReadToEndAsync(); }

        var requestData = JsonSerializer.Deserialize<CodeRequest>(requestBody, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        if (string.IsNullOrEmpty(requestData?.Code))
        {
            await SendJsonResponse(context.Response, new { success = false, error = "Code field is required" }, HttpStatusCode.BadRequest);
            return;
        }

        var result = CodeEvaluator.Execute(requestData.Code);
        await SendJsonResponse(context.Response, result);
    }

    internal class CodeRequest
    {
        [JsonPropertyName("code")]
        public string Code { get; init; } = "";
    }
}
