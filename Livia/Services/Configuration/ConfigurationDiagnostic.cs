using System.Diagnostics.CodeAnalysis;

namespace Livia.Services.Configuration;

/// <summary>Describes a configuration loading or validation issue.</summary>
[Experimental("LIVIA002")]
public sealed class ConfigurationDiagnostic
{
    /// <summary>Gets the diagnostic severity.</summary>
    public ConfigurationDiagnosticSeverity Severity { get; }
    /// <summary>Gets a stable machine-readable diagnostic code.</summary>
    public string Code { get; }
    /// <summary>Gets the human-readable diagnostic message.</summary>
    public string Message { get; }
    /// <summary>Gets the dot-separated configuration field path, if applicable.</summary>
    public string? Path { get; }

    /// <summary>Creates a diagnostic.</summary>
    public ConfigurationDiagnostic(ConfigurationDiagnosticSeverity severity, string code, string message, string? path = null)
    {
        Severity = severity;
        Code = code ?? throw new ArgumentNullException(nameof(code));
        Message = message ?? throw new ArgumentNullException(nameof(message));
        Path = path;
    }
}
