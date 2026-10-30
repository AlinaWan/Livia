using System.Diagnostics.CodeAnalysis;

namespace Livia.Services.Configuration;

/// <summary>Identifies the severity of a configuration diagnostic.</summary>
[Experimental("LIVIA002")]
public enum ConfigurationDiagnosticSeverity
{
    /// <summary>Informational message.</summary>
    Info,
    /// <summary>Recoverable issue; loading may continue.</summary>
    Warning,
    /// <summary>Error affecting a value or the complete document.</summary>
    Error
}