using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Livia.Dtos.Roblox;

public class VipServerPermissions
{
    [JsonPropertyName("clanAllowed")]
    public bool ClanAllowed
    {
        get; set;
    }

    [JsonPropertyName("enemyClanId")]
    public long? EnemyClanId
    {
        get; set;
    }

    [JsonPropertyName("friendsAllowed")]
    public bool FriendsAllowed
    {
        get; set;
    }

    [JsonPropertyName("users")]
    public List<object> Users { get; set; } = new();
}