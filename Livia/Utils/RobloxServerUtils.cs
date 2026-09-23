using System;
using System.Threading.Tasks;
using PuppeteerSharp;
using Windows.System;

namespace Livia.Utils;

public static class RobloxServerUtils
{
    /// <summary>
    /// Asynchronously authenticates with Roblox using a .ROBLOSECURITY cookie,
    /// navigates to a game page, locates a private server by name (or the first
    /// available if no name is specified), and retrieves its existing join link.
    /// </summary>
    /// <param name="gameUrl">The full URL of the Roblox game.</param>
    /// <param name="serverName">
    /// The display name of the target private server, or <c>null> to select the
    /// first available configurable server.
    /// </param>
    /// <param name="robloxSecurityToken">
    /// The valid .ROBLOSECURITY authentication cookie token.
    /// </param>
    /// <returns>
    /// The existing private server join link, or <c>null> if no matching server
    /// is found or a join link has not been generated for that server.
    /// </returns>
    /// <remarks>
    /// This method does not generate a join link if one has not already been
    /// generated.
    /// </remarks>
    public static async Task<string?> GetPrivateServerJoinLinkAsync(string gameUrl, string? serverName, string robloxSecurityToken)
    {
        await new BrowserFetcher().DownloadAsync();

        using var browser = await Puppeteer.LaunchAsync(new LaunchOptions
        {
            Headless = true, // Set to false to debug
            Args = new[] { "--disable-setuid-sandbox" }
        });

        using var page = await browser.NewPageAsync();
        await page.SetViewportAsync(new ViewPortOptions { Width = 1280, Height = 900 });

        // 1. Inject cookie
        await page.SetCookieAsync(new CookieParam
        {
            Name = ".ROBLOSECURITY",
            Value = robloxSecurityToken,
            Domain = ".roblox.com",
            Path = "/",
            HttpOnly = true,
            Secure = true
        });

        // 2. Validate session via /home
        await page.GoToAsync("https://www.roblox.com/home", WaitUntilNavigation.Networkidle2);
        if (page.Url.Contains("/Login", StringComparison.OrdinalIgnoreCase))
        {
            throw new UnauthorizedAccessException("Invalid or expired .ROBLOSECURITY token (redirected to login).");
        }

        // 3. Go to game page
        await page.GoToAsync(gameUrl, WaitUntilNavigation.Networkidle2);

        // 4. Click the "Servers" tab/button on the Roblox game page to trigger rendering of instances
        try
        {
            await page.EvaluateExpressionAsync(@"
                const tabs = Array.from(document.querySelectorAll('button, a, span'));
                const serversTab = tabs.find(el => el.textContent.trim() === 'Servers' || el.textContent.trim() === 'Private Servers');
                if (serversTab) {
                    serversTab.click();
                }
            ");

            // Wait for the running instances container to populate after clicking the tab
            await page.WaitForSelectorAsync("#running-game-instances-container", new WaitForSelectorOptions { Timeout = 10000 });
            await Task.Delay(3000); // Extra buffer for React hydration
        }
        catch
        {
            throw new Exception("Could not find or click the Servers tab on the Roblox game page.");
        }

        // 5. Scan elements either by name or grab the first available configure link if serverName is null/empty
        string? configureHref = await page.EvaluateFunctionAsync<string?>(@"
            (targetName) => {
                const container = document.querySelector('#running-game-instances-container');
                if (!container) return null;
                
                // If no name was provided, just return the very first configure link found in the container
                if (!targetName || targetName.trim() === '') {
                    const firstConfigLink = container.querySelector('a[aria-label=""Configure""], a[href*=""private-server/configure""]');
                    return firstConfigLink ? firstConfigLink.getAttribute('href') : null;
                }

                // Otherwise, search by the specified server name
                const spans = container.querySelectorAll('span');
                for (const span of spans) {
                    if (span.textContent.trim().toLowerCase() === targetName.toLowerCase()) {
                        let parent = span.parentElement;
                        while (parent && parent !== container) {
                            const configLink = parent.querySelector('a[aria-label=""Configure""], a[href*=""private-server/configure""]');
                            if (configLink) {
                                return configLink.getAttribute('href');
                            }
                            parent = parent.parentElement;
                        }
                    }
                }
                return null;
            }
        ", serverName);

        if (string.IsNullOrEmpty(configureHref))
        {
            return null;
        }

        if (configureHref.StartsWith("/"))
        {
            var uri = new Uri(gameUrl);
            configureHref = $"{uri.Scheme}://{uri.Host}{configureHref}";
        }

        // 6. Navigate to configuration page
        await page.GoToAsync(configureHref, WaitUntilNavigation.Networkidle2);
        await page.WaitForSelectorAsync("input#join-link", new WaitForSelectorOptions { Timeout = 10000 });

        // 7. Pull the join link value
        string? joinLink = await page.EvaluateFunctionAsync<string?>(@"() => {
            const input = document.querySelector('input#join-link');
            if (!input || !input.value || input.value.trim() === '') {
                return null;
            }
            return input.value;
        }");

        return joinLink;
    }
}