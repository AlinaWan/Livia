using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Livia.Dtos.Roblox;

namespace Livia.Utils;

public static class RobloxServerUtils
{
    private static readonly HttpClient SharedClient = new();
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    /// <summary>
    /// Asynchronously retrieves the existing join link for a private server
    /// accessible to the authenticated Roblox user.
    /// </summary>
    /// <param name="rootPlaceId">
    /// The root place ID of the Roblox experience.
    /// </param>
    /// <param name="serverName">
    /// The display name of the target private server, or <c>null</c> or empty
    /// to select the first available private server.
    /// </param>
    /// <param name="robloxSecurityToken">
    /// The valid <c>.ROBLOSECURITY</c> authentication cookie token.
    /// </param>
    /// <returns>
    /// The existing private server join link, or <c>null</c> if no matching
    /// server is found or the server does not have a join link.
    /// </returns>
    /// <remarks>
    /// This method uses Roblox's legacy web APIs and does not generate a new
    /// join link if one has not already been generated.
    /// </remarks>
    public static async Task<string?> GetPrivateServerJoinLinkAsync(string rootPlaceId, string? serverName, string robloxSecurityToken)
    {
        if (string.IsNullOrWhiteSpace(robloxSecurityToken))
        {
            throw new ArgumentException("The .ROBLOSECURITY token cannot be null or empty.", nameof(robloxSecurityToken));
        }

        var client = SharedClient;

        // 2. Fetch the private server list endpoint for the place
        string listUrl = $"https://games.roblox.com/v1/games/{rootPlaceId}/private-servers?cursor=&sortOrder=Desc&excludeFullGames=false";

        using var listRequest = new HttpRequestMessage(HttpMethod.Get, listUrl);
        ConfigureRobloxHeaders(listRequest, robloxSecurityToken);

        using var listResponse = await client.SendAsync(listRequest).ConfigureAwait(false);
        if (!listResponse.IsSuccessStatusCode)
        {
            return null;
        }

        using var listStream = await listResponse.Content.ReadAsStreamAsync().ConfigureAwait(false);
        var listResult = await JsonSerializer.DeserializeAsync<VipServerListResponseDto>(listStream, JsonOptions).ConfigureAwait(false);

        if (listResult?.Data == null || listResult.Data.Count == 0)
        {
            return null;
        }

        long? targetServerId = null;

        // 3. Scan list either by name or grab the first available server if serverName is null/empty
        if (string.IsNullOrWhiteSpace(serverName))
        {
            targetServerId = listResult.Data[0].VipServerId;
        }
        else
        {
            foreach (var server in listResult.Data)
            {
                if (string.Equals(server.Name, serverName.Trim(), StringComparison.OrdinalIgnoreCase))
                {
                    targetServerId = server.VipServerId;
                    break;
                }
            }
        }

        if (targetServerId == null)
        {
            return null;
        }

        // 4. Query the specific VIP server details endpoint using its ID to retrieve the join link
        string detailUrl = $"https://games.roblox.com/v1/vip-servers/{targetServerId.Value}";
        using var detailRequest = new HttpRequestMessage(HttpMethod.Get, detailUrl);
        ConfigureRobloxHeaders(detailRequest, robloxSecurityToken);

        using var detailResponse = await client.SendAsync(detailRequest).ConfigureAwait(false);
        if (!detailResponse.IsSuccessStatusCode)
        {
            return null;
        }

        using var detailStream = await detailResponse.Content.ReadAsStreamAsync().ConfigureAwait(false);
        var detailResult = await JsonSerializer.DeserializeAsync<VipServerResponseDto>(detailStream, JsonOptions).ConfigureAwait(false);

        return detailResult?.Link;
    }

    private static void ConfigureRobloxHeaders(HttpRequestMessage request, string token)
    {
        request.Headers.Add("Cookie", $".ROBLOSECURITY={token.Trim()}");
        request.Headers.Add("Origin", "https://www.roblox.com");
        request.Headers.Add("Referer", "https://www.roblox.com/");
        request.Headers.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/152.0.0.0 Safari/537.36");
    }
}