namespace Livia.Dtos.Roblox;

/// <summary>
/// Represents the authenticated Roblox user associated with an authentication token.
/// </summary>
public sealed class RobloxAuthenticatedUserDto
{
    /// <summary>
    /// Gets the Roblox user ID.
    /// </summary>
    public long Id
    {
        get; init;
    }

    /// <summary>
    /// Gets the Roblox username.
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// Gets the Roblox display name.
    /// </summary>
    public string DisplayName { get; init; } = string.Empty;
}