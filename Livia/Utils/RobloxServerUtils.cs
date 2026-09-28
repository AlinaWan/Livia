using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using Livia.Dtos.Roblox;

namespace Livia.Utils;

/// <summary>
/// Provides utilities for discovering Roblox private servers, fetching join links,
/// and managing server configurations via direct HTTP APIs.
/// </summary>
public static class RobloxServerUtils
{
    /// <summary>
    /// Asynchronously retrieves detailed information and join links for all private servers
    /// owned by or accessible to the authenticated user for a given place.
    /// </summary>
    /// <param name="rootPlaceId">
    /// The root place ID of the Roblox experience.
    /// </param>
    /// <param name="robloxSecurityToken">
    /// The valid <c>.ROBLOSECURITY</c> authentication cookie token.
    /// </param>
    /// <returns>
    /// A list of detailed private server response objects, or an empty list if none are found
    /// or the request fails.
    /// </returns>
    public static async Task<List<VipServerResponseDto>> GetAllPrivateServerDetailsAsync(
        string rootPlaceId,
        string robloxSecurityToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(
            rootPlaceId);

        ArgumentException.ThrowIfNullOrWhiteSpace(
            robloxSecurityToken);

        string listUrl =
            $"https://games.roblox.com/v1/games/{rootPlaceId}" +
            "/private-servers?cursor=&sortOrder=Desc&excludeFullGames=false";

        using HttpResponseMessage listResponse =
            await RobloxRequestService.SendAsync(
                RobloxRequestService.SharedClient,
                () => new HttpRequestMessage(
                    HttpMethod.Get,
                    listUrl),
                robloxSecurityToken,
                RobloxRequestService.ConfigureRobloxHeaders)
            .ConfigureAwait(false);

        if (!listResponse.IsSuccessStatusCode)
        {
            return [];
        }

        VipServerListResponseDto? listResult =
            await listResponse.Content.ReadFromJsonAsync<VipServerListResponseDto>(
                RobloxRequestService.JsonOptions)
            .ConfigureAwait(false);

        if (listResult?.Data == null ||
            listResult.Data.Count == 0)
        {
            return [];
        }

        List<VipServerResponseDto> detailedServers = [];

        foreach (VipServerInstanceDto serverInstance in listResult.Data)
        {
            if (serverInstance.VipServerId == null)
            {
                continue;
            }

            string detailUrl =
                $"https://games.roblox.com/v1/vip-servers/" +
                $"{serverInstance.VipServerId.Value}";

            using HttpResponseMessage detailResponse =
                await RobloxRequestService.SendAsync(
                    RobloxRequestService.SharedClient,
                    () => new HttpRequestMessage(
                        HttpMethod.Get,
                        detailUrl),
                    robloxSecurityToken,
                    RobloxRequestService.ConfigureRobloxHeaders)
                .ConfigureAwait(false);

            if (!detailResponse.IsSuccessStatusCode)
            {
                continue;
            }

            VipServerResponseDto? detailResult =
                await detailResponse.Content
                    .ReadFromJsonAsync<VipServerResponseDto>(
                        RobloxRequestService.JsonOptions)
                    .ConfigureAwait(false);

            if (detailResult != null)
            {
                detailedServers.Add(detailResult);
            }
        }

        return detailedServers;
    }

    /// <summary>
    /// Asynchronously retrieves the unique VIP server ID for a private server
    /// matching the specified name, or the first available server if no name is provided.
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
    /// The unique <c>vipServerId</c> of the server, or <c>null</c> if no matching
    /// server is found.
    /// </returns>
    public static async Task<long?> GetPrivateServerIdAsync(
        string rootPlaceId,
        string? serverName,
        string robloxSecurityToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(
            rootPlaceId);

        ArgumentException.ThrowIfNullOrWhiteSpace(
            robloxSecurityToken);

        string listUrl =
            $"https://games.roblox.com/v1/games/{rootPlaceId}" +
            "/private-servers?cursor=&sortOrder=Desc&excludeFullGames=false";

        using HttpResponseMessage listResponse =
            await RobloxRequestService.SendAsync(
                RobloxRequestService.SharedClient,
                () => new HttpRequestMessage(
                    HttpMethod.Get,
                    listUrl),
                robloxSecurityToken,
                RobloxRequestService.ConfigureRobloxHeaders)
            .ConfigureAwait(false);

        if (!listResponse.IsSuccessStatusCode)
        {
            return null;
        }

        VipServerListResponseDto? listResult =
            await listResponse.Content.ReadFromJsonAsync<VipServerListResponseDto>(
                RobloxRequestService.JsonOptions)
            .ConfigureAwait(false);

        if (listResult?.Data == null ||
            listResult.Data.Count == 0)
        {
            return null;
        }

        if (string.IsNullOrWhiteSpace(serverName))
        {
            return listResult.Data[0].VipServerId;
        }

        string targetName = serverName.Trim();

        foreach (VipServerInstanceDto server in listResult.Data)
        {
            if (string.Equals(
                    server.Name,
                    targetName,
                    StringComparison.OrdinalIgnoreCase))
            {
                return server.VipServerId;
            }
        }

        return null;
    }

    /// <summary>
    /// Asynchronously retrieves the unique VIP server access code for a private server
    /// matching the specified name, or the first available server if no name is provided.
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
    /// The unique <c>accessCode</c> of the server, or <c>null</c> if no matching
    /// server is found.
    /// </returns>
    public static async Task<string?> GetPrivateServerAccessCodeAsync(
        string rootPlaceId,
        string? serverName,
        string robloxSecurityToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(
            rootPlaceId);

        ArgumentException.ThrowIfNullOrWhiteSpace(
            robloxSecurityToken);

        string listUrl =
            $"https://games.roblox.com/v1/games/{rootPlaceId}" +
            "/private-servers?cursor=&sortOrder=Desc&excludeFullGames=false";

        using HttpResponseMessage listResponse =
            await RobloxRequestService.SendAsync(
                RobloxRequestService.SharedClient,
                () => new HttpRequestMessage(
                    HttpMethod.Get,
                    listUrl),
                robloxSecurityToken,
                RobloxRequestService.ConfigureRobloxHeaders)
            .ConfigureAwait(false);

        if (!listResponse.IsSuccessStatusCode)
        {
            return null;
        }

        VipServerListResponseDto? listResult =
            await listResponse.Content.ReadFromJsonAsync<VipServerListResponseDto>(
                RobloxRequestService.JsonOptions)
            .ConfigureAwait(false);

        if (listResult?.Data == null ||
            listResult.Data.Count == 0)
        {
            return null;
        }

        if (string.IsNullOrWhiteSpace(serverName))
        {
            return listResult.Data[0].AccessCode;
        }

        string targetName = serverName.Trim();

        foreach (VipServerInstanceDto server in listResult.Data)
        {
            if (string.Equals(
                    server.Name,
                    targetName,
                    StringComparison.OrdinalIgnoreCase))
            {
                return server.AccessCode;
            }
        }

        return null;
    }

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
    /// This method does not generate a join link if one has not been generated.
    /// </remarks>
    public static async Task<string?> GetPrivateServerJoinLinkAsync(
        string rootPlaceId,
        string? serverName,
        string robloxSecurityToken)
    {
        long? targetServerId =
            await GetPrivateServerIdAsync(
                rootPlaceId,
                serverName,
                robloxSecurityToken)
            .ConfigureAwait(false);

        if (targetServerId == null)
        {
            return null;
        }

        string detailUrl =
            $"https://games.roblox.com/v1/vip-servers/" +
            $"{targetServerId.Value}";

        using HttpResponseMessage detailResponse =
            await RobloxRequestService.SendAsync(
                RobloxRequestService.SharedClient,
                () => new HttpRequestMessage(
                    HttpMethod.Get,
                    detailUrl),
                robloxSecurityToken,
                RobloxRequestService.ConfigureRobloxHeaders)
            .ConfigureAwait(false);

        if (!detailResponse.IsSuccessStatusCode)
        {
            return null;
        }

        VipServerResponseDto? detailResult =
            await detailResponse.Content
                .ReadFromJsonAsync<VipServerResponseDto>(
                    RobloxRequestService.JsonOptions)
                .ConfigureAwait(false);

        return detailResult?.Link;
    }

    /// <summary>
    /// Asynchronously regenerates the join link for a private server by sending a PATCH request
    /// to the Roblox VIP servers endpoint, forcing a new join code and link to be created.
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
    /// The newly generated private server join link, or <c>null</c> if the request
    /// fails or the server is not found.
    /// </returns>
    public static async Task<string?> GeneratePrivateServerLinkAsync(
        string rootPlaceId,
        string? serverName,
        string robloxSecurityToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(
            rootPlaceId);

        ArgumentException.ThrowIfNullOrWhiteSpace(
            robloxSecurityToken);

        long? targetServerId =
            await GetPrivateServerIdAsync(
                rootPlaceId,
                serverName,
                robloxSecurityToken)
            .ConfigureAwait(false);

        if (targetServerId == null)
        {
            return null;
        }

        string patchUrl =
            $"https://games.roblox.com/v1/vip-servers/" +
            $"{targetServerId.Value}";

        using HttpResponseMessage patchResponse =
            await RobloxRequestService.SendAsync(
                RobloxRequestService.SharedClient,
                () => new HttpRequestMessage(
                    HttpMethod.Patch,
                    patchUrl)
                {
                    Content = JsonContent.Create(new
                    {
                        newJoinCode = true
                    })
                },
                robloxSecurityToken,
                RobloxRequestService.ConfigureRobloxHeaders)
            .ConfigureAwait(false);

        if (!patchResponse.IsSuccessStatusCode)
        {
            return null;
        }

        VipServerResponseDto? patchResult =
            await patchResponse.Content
                .ReadFromJsonAsync<VipServerResponseDto>(
                    RobloxRequestService.JsonOptions)
                .ConfigureAwait(false);

        return patchResult?.Link;
    }
}