using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Livia.Dtos.Roblox;

public class VipServerVoiceSettings
{
    [JsonPropertyName("enabled")]
    public bool Enabled
    {
        get; set;
    }
}