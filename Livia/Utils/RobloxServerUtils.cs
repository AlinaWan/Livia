using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Livia.Dtos.Roblox;

namespace Livia.Utils;

/// <summary>
/// High-performance utility for discovering Roblox private servers, fetching join links, 
/// and managing server configurations via direct HTTP APIs.
/// </summary>
public static class RobloxServerUtils
{
    private static readonly HttpClient SharedClient = new();
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    /// <summary>
    /// Asynchronously retrieves detailed information and join links for all private servers 
    /// owned by or accessible to the authenticated user for a given place.
    /// </summary>
    /// <param name="rootPlaceId">The root place ID of the Roblox experience.</param>
    /// <param name="robloxSecurityToken">The valid <c>.ROBLOSECURITY</c> authentication cookie token.</param>
    /// <returns>
    /// A list of detailed private server response objects, or an empty list if none are found or the request fails.
    /// </returns>
    public static async Task<List<VipServerResponseDto>> GetAllPrivateServerDetailsAsync(string rootPlaceId, string robloxSecurityToken)
    {
        if (string.IsNullOrWhiteSpace(robloxSecurityToken))
        {
            throw new ArgumentException("The .ROBLOSECURITY token cannot be null or empty.", nameof(robloxSecurityToken));
        }

        // 1. Fetch the private server list to discover all server IDs
        string listUrl = $"https://games.roblox.com/v1/games/{rootPlaceId}/private-servers?cursor=&sortOrder=Desc&excludeFullGames=false";

        using var listRequest = new HttpRequestMessage(HttpMethod.Get, listUrl);
        RobloxRequestUtils.ConfigureRobloxHeaders(listRequest, robloxSecurityToken);

        using var listResponse = await SharedClient.SendAsync(listRequest).ConfigureAwait(false);
        if (!listResponse.IsSuccessStatusCode)
        {
            return new List<VipServerResponseDto>();
        }

        using var listStream = await listResponse.Content.ReadAsStreamAsync().ConfigureAwait(false);
        var listResult = await JsonSerializer.DeserializeAsync<VipServerListResponseDto>(listStream, JsonOptions).ConfigureAwait(false);

        if (listResult?.Data == null || listResult.Data.Count == 0)
        {
            return new List<VipServerResponseDto>();
        }

        var detailedServers = new List<VipServerResponseDto>();

        // 2. Iterate through each discovered server and fetch its complete details
        foreach (var serverInstance in listResult.Data)
        {
            if (serverInstance.VipServerId == null)
            {
                continue;
            }

            string detailUrl = $"https://games.roblox.com/v1/vip-servers/{serverInstance.VipServerId.Value}";
            using var detailRequest = new HttpRequestMessage(HttpMethod.Get, detailUrl);
            RobloxRequestUtils.ConfigureRobloxHeaders(detailRequest, robloxSecurityToken);

            using var detailResponse = await SharedClient.SendAsync(detailRequest).ConfigureAwait(false);
            if (detailResponse.IsSuccessStatusCode)
            {
                using var detailStream = await detailResponse.Content.ReadAsStreamAsync().ConfigureAwait(false);
                var detailResult = await JsonSerializer.DeserializeAsync<VipServerResponseDto>(detailStream, JsonOptions).ConfigureAwait(false);

                if (detailResult != null)
                {
                    detailedServers.Add(detailResult);
                }
            }
        }

        return detailedServers;
    }

    /// <summary>
    /// Asynchronously retrieves the unique VIP server ID for a private server 
    /// matching the specified name, or the first available server if no name is provided.
    /// </summary>
    /// <param name="rootPlaceId">The root place ID of the Roblox experience.</param>
    /// <param name="serverName">
    /// The display name of the target private server, or <c>null</c> or empty
    /// to select the first available private server.
    /// </param>
    /// <param name="robloxSecurityToken">The valid <c>.ROBLOSECURITY</c> authentication cookie token.</param>
    /// <returns>
    /// The unique <c>vipServerId</c> of the server, or <c>null</c> if no matching server is found.
    /// </returns>
    public static async Task<long?> GetPrivateServerIdAsync(string rootPlaceId, string? serverName, string robloxSecurityToken)
    {
        if (string.IsNullOrWhiteSpace(robloxSecurityToken))
        {
            throw new ArgumentException("The .ROBLOSECURITY token cannot be null or empty.", nameof(robloxSecurityToken));
        }

        string listUrl = $"https://games.roblox.com/v1/games/{rootPlaceId}/private-servers?cursor=&sortOrder=Desc&excludeFullGames=false";

        using var listRequest = new HttpRequestMessage(HttpMethod.Get, listUrl);
        RobloxRequestUtils.ConfigureRobloxHeaders(listRequest, robloxSecurityToken);

        using var listResponse = await SharedClient.SendAsync(listRequest).ConfigureAwait(false);
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

        if (string.IsNullOrWhiteSpace(serverName))
        {
            return listResult.Data[0].VipServerId;
        }

        foreach (var server in listResult.Data)
        {
            if (string.Equals(server.Name, serverName.Trim(), StringComparison.OrdinalIgnoreCase))
            {
                return server.VipServerId;
            }
        }

        return null;
    }

    /// <summary>
    /// Asynchronously retrieves the existing join link for a private server
    /// accessible to the authenticated Roblox user.
    /// </summary>
    /// <param name="rootPlaceId">The root place ID of the Roblox experience.</param>
    /// <param name="serverName">
    /// The display name of the target private server, or <c>null</c> or empty
    /// to select the first available private server.
    /// </param>
    /// <param name="robloxSecurityToken">The valid <c>.ROBLOSECURITY</c> authentication cookie token.</param>
    /// <returns>
    /// The existing private server join link, or <c>null</c> if no matching
    /// server is found or the server does not have a join link.
    /// </returns>
    /// <remarks>
    /// This method does not generate a join link if one has not been generated.
    /// </remarks>
    public static async Task<string?> GetPrivateServerJoinLinkAsync(string rootPlaceId, string? serverName, string robloxSecurityToken)
    {
        long? targetServerId = await GetPrivateServerIdAsync(rootPlaceId, serverName, robloxSecurityToken).ConfigureAwait(false);
        if (targetServerId == null)
        {
            return null;
        }

        string detailUrl = $"https://games.roblox.com/v1/vip-servers/{targetServerId.Value}";
        using var detailRequest = new HttpRequestMessage(HttpMethod.Get, detailUrl);
        RobloxRequestUtils.ConfigureRobloxHeaders(detailRequest, robloxSecurityToken);

        using var detailResponse = await SharedClient.SendAsync(detailRequest).ConfigureAwait(false);
        if (!detailResponse.IsSuccessStatusCode)
        {
            return null;
        }

        using var detailStream = await detailResponse.Content.ReadAsStreamAsync().ConfigureAwait(false);
        var detailResult = await JsonSerializer.DeserializeAsync<VipServerResponseDto>(detailStream, JsonOptions).ConfigureAwait(false);

        return detailResult?.Link;
    }

    /// <summary>
    /// Asynchronously regenerates the join link for a private server by sending a PATCH request 
    /// to the Roblox VIP servers endpoint, forcing a new join code and link to be created.
    /// </summary>
    /// <param name="rootPlaceId">The root place ID of the Roblox experience.</param>
    /// <param name="serverName">
    /// The display name of the target private server, or <c>null</c> or empty
    /// to select the first available private server.
    /// </param>
    /// <param name="robloxSecurityToken">The valid <c>.ROBLOSECURITY</c> authentication cookie token.</param>
    /// <returns>
    /// The newly generated private server join link, or <c>null</c> if the request fails or the server is not found.
    /// </returns>
    private static async Task<string?> GeneratePrivateServerLinkAsync(string rootPlaceId, string? serverName, string robloxSecurityToken)
    {
        long ? targetServerId = await GetPrivateServerIdAsync(rootPlaceId, serverName, robloxSecurityToken).ConfigureAwait(false);
        if (targetServerId == null)
        {
            return null;
        }

        string patchUrl = $"https://games.roblox.com/v1/vip-servers/{targetServerId.Value}";

        using var patchRequest = new HttpRequestMessage(HttpMethod.Patch, patchUrl)
        {
            Content = JsonContent.Create(new { newJoinCode = true })
        };

        // TODO: 2026-09-25: Implement X-Csrf-Token and attach it to the request headers, then change method visibility to public
        RobloxRequestUtils.ConfigureRobloxHeaders(patchRequest, robloxSecurityToken);

        using var patchResponse = await SharedClient.SendAsync(patchRequest).ConfigureAwait(false);
        if (!patchResponse.IsSuccessStatusCode)
        {
            return null;
        }

        using var patchStream = await patchResponse.Content.ReadAsStreamAsync().ConfigureAwait(false);
        var patchResult = await JsonSerializer.DeserializeAsync<VipServerResponseDto>(patchStream, JsonOptions).ConfigureAwait(false);

        return patchResult?.Link;
    }
}