using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Livia.Dtos.Roblox;

public class VipServerInstanceDto
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("vipServerId")]
    public long? VipServerId
    {
        get; set;
    }

    [JsonPropertyName("accessCode")]
    public string? AccessCode
    {
        get; set;
    }

    [JsonPropertyName("owner")]
    public VipOwnerDto? Owner
    {
        get; set;
    }
}