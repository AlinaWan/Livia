using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Livia.Dtos.Roblox;

public class VipServerRootPlace
{
    [JsonPropertyName("id")]
    public long Id
    {
        get; set;
    }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
}