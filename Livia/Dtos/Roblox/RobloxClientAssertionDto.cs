namespace Livia.Dtos.Roblox;

/// <summary>
/// Represents the response returned by the Roblox client assertion endpoint.
/// </summary>
internal sealed class RobloxClientAssertionDto
{
    /// <summary>
    /// Gets or sets the client assertion used when requesting an authentication ticket.
    /// </summary>
    public string? ClientAssertion
    {
        get; set;
    }
}