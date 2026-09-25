using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Livia.Dtos.Roblox;

public class VipServerSubscription
{
    [JsonPropertyName("active")]
    public bool Active
    {
        get; set;
    }

    [JsonPropertyName("expired")]
    public bool Expired
    {
        get; set;
    }

    [JsonPropertyName("expirationDate")]
    public string? ExpirationDate
    {
        get; set;
    }

    [JsonPropertyName("price")]
    public int Price
    {
        get; set;
    }

    [JsonPropertyName("canRenew")]
    public bool CanRenew
    {
        get; set;
    }
}