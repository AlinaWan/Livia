using System.Diagnostics;
using System.IO;
using System.Text;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using Livia.Dtos.Roblox;

namespace Livia.Utils;

/// <summary>
/// Provides utilities for authenticating Roblox accounts and launching Roblox experiences.
/// </summary>
public static class RobloxPlayerUtils
{
    private const string AuthenticationTicketUrl =
        "https://auth.roblox.com/v1/authentication-ticket/";

    private const string ClientAssertionUrl =
        "https://auth.roblox.com/v1/client-assertion";

    private const string AuthenticatedUserUrl =
        "https://users.roblox.com/v1/users/authenticated";

    private const string AuthenticationTicketHeader =
        "Rbx-Authentication-Ticket";

    private const string PlaceLauncherUrl =
        "https://www.roblox.com/Game/PlaceLauncher.ashx";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    /// <summary>
    /// Asynchronously obtains a short-lived authentication ticket for a Roblox account.
    /// </summary>
    /// <param name="robloxSecurityToken">
    /// The valid <c>.ROBLOSECURITY</c> authentication cookie token.
    /// </param>
    /// <param name="cancellationToken">
    /// A token that can be used to cancel the operation.
    /// </param>
    /// <returns>
    /// The authentication ticket returned by Roblox, or <c>null</c> if the request fails
    /// or Roblox does not return an authentication ticket.
    /// </returns>
    public static async Task<string?> GetAuthenticationTicketAsync(
        string robloxSecurityToken,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(
            robloxSecurityToken);

        string? clientAssertion =
            await GetClientAssertionAsync(
                robloxSecurityToken,
                cancellationToken).ConfigureAwait(false);

        if (clientAssertion == null)
        {
            return null;
        }

        using HttpResponseMessage response =
            await RobloxRequestService.SendAsync(
                RobloxRequestService.SharedClient,
                () =>
                {
                    HttpRequestMessage request = new(
                        HttpMethod.Post,
                        AuthenticationTicketUrl);

                    request.Headers.Accept.ParseAdd(
                        "application/json");

                    request.Content = new StringContent(
                        JsonSerializer.Serialize(
                            new RobloxClientAssertionDto
                            {
                                ClientAssertion = clientAssertion
                            }),
                        Encoding.UTF8,
                        "application/json");

                    return request;
                },
                robloxSecurityToken,
                RobloxRequestService.ConfigureRobloxAuthenticationTicketHeaders,
                cancellationToken).ConfigureAwait(false);

        if (response.StatusCode != HttpStatusCode.OK)
        {
            return null;
        }

        if (!response.Headers.TryGetValues(
                AuthenticationTicketHeader,
                out IEnumerable<string>? values))
        {
            return null;
        }

        string? ticket = values.FirstOrDefault();

        return string.IsNullOrWhiteSpace(ticket)
            ? null
            : ticket.Trim();
    }

    /// <summary>
    /// Creates a Roblox Player launch URI for the specified place and authentication ticket.
    /// </summary>
    /// <param name="placeId">
    /// The ID of the place to launch.
    /// </param>
    /// <param name="authenticationTicket">
    /// The authentication ticket returned by
    /// <see cref="GetAuthenticationTicketAsync"/>.
    /// </param>
    /// <returns>
    /// A <c>roblox-player:</c> URI for launching the Roblox client.
    /// </returns>
    public static string CreateLaunchUri(
        long placeId,
        string authenticationTicket,
        string? privateServerAccessCode = null)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(placeId);

        ArgumentException.ThrowIfNullOrWhiteSpace(
            authenticationTicket);

        authenticationTicket = authenticationTicket.Trim();

        long browserTrackerId =
            Random.Shared.NextInt64(
                1_000_000_000_000_000,
                9_999_999_999_999_999);

        Guid joinAttemptId = Guid.NewGuid();

        long launchTime =
            DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        bool privateServer =
            !string.IsNullOrWhiteSpace(privateServerAccessCode);

        string request =
            privateServer
                ? "RequestPrivateGame"
                : "RequestGame";

        string placeLauncherUrl =
            $"{PlaceLauncherUrl}" +
            $"?request={request}" +
            $"&browserTrackerId={browserTrackerId}" +
            $"&placeId={placeId}" +
            (privateServer
                ? $"&accessCode={Uri.EscapeDataString(privateServerAccessCode!.Trim())}" +
                  $"&linkCode="
                : "&isPlayTogetherGame=false") +
            $"&joinAttemptId={joinAttemptId}" +
            (privateServer
                ? "&joinAttemptOrigin=privateServerListJoin"
                : "&joinAttemptOrigin=PlayButton");

        string encodedPlaceLauncherUrl =
            Uri.EscapeDataString(placeLauncherUrl);

        return
            $"roblox-player:1" +
            $"+launchmode:play" +
            $"+gameinfo:{authenticationTicket}" +
            $"+launchtime:{launchTime}" +
            $"+placelauncherurl:{encodedPlaceLauncherUrl}" +
            $"+browsertrackerid:{browserTrackerId}" +
            $"+robloxLocale:en_us" +
            $"+gameLocale:en_us" +
            $"+channel:" +
            $"+LaunchExp:InApp";
    }

    /// <summary>
    /// Asynchronously obtains authentication tickets for the specified Roblox accounts
    /// and launches the specified place for each unique account.
    /// </summary>
    /// <param name="robloxSecurityTokens">
    /// The <c>.ROBLOSECURITY</c> authentication cookie tokens of the accounts to launch.
    /// </param>
    /// <param name="placeId">
    /// The ID of the place to launch.
    /// </param>
    /// <param name="cancellationToken">
    /// A token that can be used to cancel the operation.
    /// </param>
    /// <returns>
    /// The result of each account launch attempt.
    /// </returns>
    public static async Task<IReadOnlyList<RobloxAccountLaunchResultDto>> JoinAccountsAsync(
        IEnumerable<string> robloxSecurityTokens,
        long placeId,
        string? privateServerAccessCode = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(
            robloxSecurityTokens);

        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(
            placeId);

        HashSet<long> processedUserIds = [];
        List<RobloxAccountLaunchResultDto> results = [];

        foreach (string? token in robloxSecurityTokens)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (string.IsNullOrWhiteSpace(token))
            {
                results.Add(new RobloxAccountLaunchResultDto
                {
                    Success = false,
                    Error =
                        "The .ROBLOSECURITY token cannot be null or empty."
                });

                continue;
            }

            string normalizedToken = token.Trim();

            RobloxAuthenticatedUserDto? user =
                await GetAuthenticatedUserAsync(
                    normalizedToken,
                    cancellationToken).ConfigureAwait(false);

            if (user == null)
            {
                results.Add(new RobloxAccountLaunchResultDto
                {
                    Success = false,
                    Error =
                        "The Roblox account could not be authenticated."
                });

                continue;
            }

            if (!processedUserIds.Add(user.Id))
            {
                results.Add(new RobloxAccountLaunchResultDto
                {
                    UserId = user.Id,
                    Success = false,
                    Error =
                        "The Roblox account was already included in the operation."
                });

                continue;
            }

            string? authenticationTicket =
                await GetAuthenticationTicketAsync(
                    normalizedToken,
                    cancellationToken).ConfigureAwait(false);

            if (authenticationTicket == null)
            {
                results.Add(new RobloxAccountLaunchResultDto
                {
                    UserId = user.Id,
                    Success = false,
                    Error =
                        "Roblox did not return an authentication ticket."
                });

                continue;
            }

            string launchUri =
                CreateLaunchUri(
                    placeId,
                    authenticationTicket,
                    privateServerAccessCode);

            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = launchUri,
                    UseShellExecute = true
                });

                results.Add(new RobloxAccountLaunchResultDto
                {
                    UserId = user.Id,
                    Success = true
                });
            }
            catch (Exception ex) when (
                ex is InvalidOperationException ||
                ex is System.ComponentModel.Win32Exception)
            {
                results.Add(new RobloxAccountLaunchResultDto
                {
                    UserId = user.Id,
                    Success = false,
                    Error = ex.Message
                });
            }
        }

        return results;
    }

    private static async Task<RobloxAuthenticatedUserDto?> GetAuthenticatedUserAsync(
        string robloxSecurityToken,
        CancellationToken cancellationToken)
    {
        using HttpResponseMessage response =
            await RobloxRequestService.SendAsync(
                RobloxRequestService.SharedClient,
                () => new HttpRequestMessage(
                    HttpMethod.Get,
                    AuthenticatedUserUrl),
                robloxSecurityToken,
                RobloxRequestService.ConfigureRobloxHeaders,
                cancellationToken).ConfigureAwait(false);

        if (response.StatusCode != HttpStatusCode.OK)
        {
            return null;
        }

        await using Stream stream =
            await response.Content.ReadAsStreamAsync(
                cancellationToken).ConfigureAwait(false);

        return await JsonSerializer.DeserializeAsync<RobloxAuthenticatedUserDto>(
            stream,
            JsonOptions,
            cancellationToken).ConfigureAwait(false);
    }

    private static async Task<string?> GetClientAssertionAsync(
        string robloxSecurityToken,
        CancellationToken cancellationToken)
    {
        using HttpResponseMessage response =
            await RobloxRequestService.SendAsync(
                RobloxRequestService.SharedClient,
                () => new HttpRequestMessage(
                    HttpMethod.Get,
                    ClientAssertionUrl),
                robloxSecurityToken,
                RobloxRequestService.ConfigureRobloxHeaders,
                cancellationToken).ConfigureAwait(false);

        if (response.StatusCode != HttpStatusCode.OK)
        {
            return null;
        }

        await using Stream stream =
            await response.Content.ReadAsStreamAsync(
                cancellationToken).ConfigureAwait(false);

        RobloxClientAssertionDto? result =
            await JsonSerializer.DeserializeAsync<RobloxClientAssertionDto>(
                stream,
                JsonOptions,
                cancellationToken).ConfigureAwait(false);

        return result?.ClientAssertion;
    }
}