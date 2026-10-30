using System.Diagnostics.CodeAnalysis;

namespace Livia.Services.Configuration;

/// <summary>Provides a configuration document to reload event subscribers.</summary>
[Experimental("LIVIA002")]
public sealed class ConfigurationDocumentEventArgs(ConfigurationDocument document) : EventArgs
{
    /// <summary>Gets the successfully loaded configuration document.</summary>
    public ConfigurationDocument Document { get; } = document ?? throw new ArgumentNullException(nameof(document));
}