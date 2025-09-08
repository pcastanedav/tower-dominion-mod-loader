using System.Text.Json.Serialization;

namespace TDModLoader.Handlers.Http;

public class CodeRequest
{
    [JsonPropertyName("code")]
    public string Code { get; init; } = "";
}