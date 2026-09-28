using System.Collections.Concurrent;
using System.Net;
using System.Net.Http;
using System.Text.Json;

namespace Livia.Utils;

internal static class RobloxRequestService
{
    private const string CsrfTokenHeader = "X-Csrf-Token";

    internal static readonly HttpClient SharedClient = new();

    private static readonly ConcurrentDictionary<string, string> CsrfTokens = new();

    internal static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    internal static void ConfigureRobloxHeaders(
        HttpRequestMessage request,
        string robloxSecurityToken)
    {
        request.Headers.Add(
            "Cookie",
            $".ROBLOSECURITY={robloxSecurityToken}");

        request.Headers.Add(
            "Origin",
            "https://www.roblox.com");

        request.Headers.Add(
            "Referer",
            "https://www.roblox.com/");

        request.Headers.UserAgent.ParseAdd(
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) " +
            "AppleWebKit/537.36 (KHTML, like Gecko) " +
            "Chrome/152.0.0.0 Safari/537.36");
    }

    internal static void ConfigureRobloxAuthenticationTicketHeaders(
        HttpRequestMessage request,
        string robloxSecurityToken)
    {
        request.Headers.Add(
            "Cookie",
            $".ROBLOSECURITY={robloxSecurityToken}");

        request.Headers.Add(
            "Origin",
            "https://www.roblox.com");

        request.Headers.Add(
            "Referer",
            "https://www.roblox.com/");

        request.Headers.UserAgent.ParseAdd(
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) " +
            "AppleWebKit/537.36 (KHTML, like Gecko) " +
            "Chrome/152.0.0.0 Safari/537.36");

        request.Headers.TryAddWithoutValidation(
            "Accept",
            "application/json, text/plain, */*");
    }

    internal static async Task<HttpResponseMessage> SendAsync(
        HttpClient client,
        Func<HttpRequestMessage> requestFactory,
        string robloxSecurityToken,
        Action<HttpRequestMessage, string> configureHeaders,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentNullException.ThrowIfNull(requestFactory);
        ArgumentException.ThrowIfNullOrWhiteSpace(robloxSecurityToken);
        ArgumentNullException.ThrowIfNull(configureHeaders);

        robloxSecurityToken = robloxSecurityToken.Trim();

        string? csrfToken =
            GetCachedCsrfToken(robloxSecurityToken);

        using HttpRequestMessage request = requestFactory();

        configureHeaders(
            request,
            robloxSecurityToken);

        if (csrfToken != null)
        {
            request.Headers.TryAddWithoutValidation(
                CsrfTokenHeader,
                csrfToken);
        }

        HttpResponseMessage response =
            await client.SendAsync(
                request,
                cancellationToken).ConfigureAwait(false);

        if (response.StatusCode != HttpStatusCode.Forbidden ||
            !response.Headers.TryGetValues(
                CsrfTokenHeader,
                out IEnumerable<string>? values))
        {
            return response;
        }

        string? newCsrfToken = values.FirstOrDefault();

        if (string.IsNullOrWhiteSpace(newCsrfToken))
        {
            return response;
        }

        newCsrfToken = newCsrfToken.Trim();

        CsrfTokens[robloxSecurityToken] = newCsrfToken;

        response.Dispose();

        using HttpRequestMessage retryRequest =
            requestFactory();

        configureHeaders(
            retryRequest,
            robloxSecurityToken);

        retryRequest.Headers.TryAddWithoutValidation(
            CsrfTokenHeader,
            newCsrfToken);

        return await client.SendAsync(
            retryRequest,
            cancellationToken).ConfigureAwait(false);
    }

    private static string? GetCachedCsrfToken(
        string robloxSecurityToken)
    {
        return CsrfTokens.TryGetValue(
            robloxSecurityToken,
            out string? csrfToken)
                ? csrfToken
                : null;
    }
}