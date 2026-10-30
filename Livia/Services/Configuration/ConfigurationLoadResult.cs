using System.Diagnostics.CodeAnalysis;

namespace Livia.Services.Configuration;

/// <summary>Contains the result of a configuration load operation.</summary>
[Experimental("LIVIA002")]
public sealed class ConfigurationLoadResult
{
    /// <summary>Gets whether a document was produced. Field-level errors may still be present.</summary>
    public bool Success { get; }
    /// <summary>Gets the loaded document, or null when the input document could not be parsed.</summary>
    public ConfigurationDocument? Document { get; }
    /// <summary>Gets diagnostics generated during loading.</summary>
    public IReadOnlyList<ConfigurationDiagnostic> Diagnostics { get; }

    internal ConfigurationLoadResult(bool success, ConfigurationDocument? document, IEnumerable<ConfigurationDiagnostic> diagnostics)
    {
        Success = success;
        Document = document;
        Diagnostics = diagnostics.ToArray();
    }
}
