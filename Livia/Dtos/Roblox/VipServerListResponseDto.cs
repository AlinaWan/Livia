using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Livia.Dtos.Roblox;

public class VipServerListResponseDto
{
    [JsonPropertyName("data")]
    public List<VipServerInstanceDto> Data { get; set; } = new();
}