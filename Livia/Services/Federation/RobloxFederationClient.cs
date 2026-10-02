using System.Diagnostics.CodeAnalysis;

namespace Livia.Services.Federation;

[Experimental("LIVIA002")]
public sealed class RobloxFederationClient
{
    public RobloxFederationClient(long identityProviderId)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(
            identityProviderId);

        IdentityProviderId = identityProviderId;
    }

    public long IdentityProviderId
    {
        get;
    }

    public Uri CreateOAuthAuthorizationUri(
        string? postAuthenticationIntentId = null)
    {
        var builder = new UriBuilder(
            "https://auth.roblox.com")
        {
            Path =
                $"/v1/external/{IdentityProviderId}/sso/oauth/init"
        };

        if (!string.IsNullOrWhiteSpace(postAuthenticationIntentId))
        {
            builder.Query =
                $"postAuthenticationIntentId=" +
                Uri.EscapeDataString(postAuthenticationIntentId);
        }

        return builder.Uri;
    }
}