using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace Livia.Dtos;

public class IpInfoResponseDto
{
    // Body Properties
    [JsonPropertyName("ip")]
    public string? Ip
    {
        get; set;
    }

    [JsonPropertyName("hostname")]
    public string? Hostname
    {
        get; set;
    }

    [JsonPropertyName("city")]
    public string? City
    {
        get; set;
    }

    [JsonPropertyName("region")]
    public string? Region
    {
        get; set;
    }

    [JsonPropertyName("country")]
    public string? Country
    {
        get; set;
    }

    [JsonPropertyName("loc")]
    public string? Loc
    {
        get; set;
    }

    [JsonPropertyName("org")]
    public string? Org
    {
        get; set;
    }

    [JsonPropertyName("postal")]
    public string? Postal
    {
        get; set;
    }

    [JsonPropertyName("timezone")]
    public string? Timezone
    {
        get; set;
    }

    [JsonPropertyName("readme")]
    public string? Readme
    {
        get; set;
    }

    [JsonPropertyName("anycast")]
    public bool? Anycast
    {
        get; set;
    }

    // Header Metadata
    public Dictionary<string, string> Headers { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}