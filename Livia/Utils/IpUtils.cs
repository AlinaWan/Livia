using System.Net.Http;
using System.Text.Json;
using Livia.Dtos;

namespace Livia.Utils;

public static class IpUtils
{
    private static readonly HttpClient SharedClient = new HttpClient();
    private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
    {
        PropertyNameCaseInsensitive = true
    };

    public static async Task<IpInfoResponseDto?> GetIpInfoAsync(
        string? ipAddress = null,
        HttpClient? httpClient = null,
        CancellationToken cancellationToken = default)
    {
        var client = httpClient ?? SharedClient;

        string url = string.IsNullOrWhiteSpace(ipAddress)
            ? "https://ipinfo.io/json"
            : $"https://ipinfo.io/{ipAddress.Trim()}/json";

        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.UserAgent.ParseAdd("Livia/1.0");

        using var response = await client.SendAsync(request, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        using var contentStream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);

        var result = await JsonSerializer.DeserializeAsync<IpInfoResponseDto>(contentStream, JsonOptions, cancellationToken).ConfigureAwait(false);

        if (result != null)
        {
            foreach (var header in response.Headers)
            {
                result.Headers[header.Key] = string.Join(", ", header.Value);
            }

            foreach (var header in response.Content.Headers)
            {
                result.Headers[header.Key] = string.Join(", ", header.Value);
            }
        }

        return result;
    }
}