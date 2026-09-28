namespace Livia.Dtos.Roblox;

/// <summary>
/// Represents the result of attempting to launch a Roblox account into an experience.
/// </summary>
public sealed class RobloxAccountLaunchResultDto
{
    /// <summary>
    /// Gets the Roblox user ID associated with the account, if it could be determined.
    /// </summary>
    public long? UserId
    {
        get; init;
    }

    /// <summary>
    /// Gets a value indicating whether the account was successfully launched.
    /// </summary>
    public bool Success
    {
        get; init;
    }

    /// <summary>
    /// Gets the reason the account could not be launched, if applicable.
    /// </summary>
    public string? Error
    {
        get; init;
    }
}