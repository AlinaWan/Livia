using System.Collections.Concurrent;
using System.Net;
using System.Net.Http;

namespace Livia.Utils;

internal static class RobloxRequestUtils
{
    private const string CsrfTokenHeader = "X-Csrf-Token";

    private static readonly ConcurrentDictionary<string, string> CsrfTokens = new();

    internal static void ConfigureRobloxHeaders(
        HttpRequestMessage request,
        string robloxSecurityToken)
    {
        request.Headers.Add(
            "Cookie",
            $".ROBLOSECURITY={robloxSecurityToken.Trim()}");

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

    internal static async Task<HttpResponseMessage> SendAsync(
        HttpClient client,
        Func<HttpRequestMessage> requestFactory,
        string robloxSecurityToken,
        CancellationToken cancellationToken = default)
    {
        string? csrfToken = GetCachedCsrfToken(robloxSecurityToken);

        using HttpRequestMessage request = requestFactory();

        ConfigureRobloxHeaders(request, robloxSecurityToken);

        if (csrfToken != null)
        {
            request.Headers.TryAddWithoutValidation(
                CsrfTokenHeader,
                csrfToken);
        }

        HttpResponseMessage response = await client.SendAsync(
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

        using HttpRequestMessage retryRequest = requestFactory();

        ConfigureRobloxHeaders(
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
