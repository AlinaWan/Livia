using System.Net.Http;

namespace Livia.Utils
{
    internal static class RobloxRequestUtils
    {
        internal static void ConfigureRobloxHeaders(HttpRequestMessage request, string token)
        {
            request.Headers.Add("Cookie", $".ROBLOSECURITY={token.Trim()}");
            request.Headers.Add("Origin", "https://www.roblox.com");
            request.Headers.Add("Referer", "https://www.roblox.com/");
            request.Headers.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/152.0.0.0 Safari/537.36");
        }
    }
}
