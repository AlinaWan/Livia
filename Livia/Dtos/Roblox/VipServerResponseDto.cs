using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Livia.Dtos.Roblox;

public class VipServerResponseDto
{
    [JsonPropertyName("id")]
    public long Id
    {
        get; set;
    }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("joinCode")]
    public string? JoinCode
    {
        get; set;
    }

    [JsonPropertyName("link")]
    public string? Link
    {
        get; set;
    }

    [JsonPropertyName("active")]
    public bool Active
    {
        get; set;
    }

    [JsonExtensionData]
    public Dictionary<string, object> Headers { get; set; } = new();
}