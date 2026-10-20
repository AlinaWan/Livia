namespace Livia.Services.Configuration;

/// <summary>Provides a configuration document to reload event subscribers.</summary>
public sealed class ConfigurationDocumentEventArgs(ConfigurationDocument document) : EventArgs
{
    /// <summary>Gets the successfully loaded configuration document.</summary>
    public ConfigurationDocument Document { get; } = document ?? throw new ArgumentNullException(nameof(document));
}

/// <summary>Provides a configuration load result to failure event subscribers.</summary>
public sealed class ConfigurationLoadResultEventArgs(ConfigurationLoadResult result) : EventArgs
{
    /// <summary>Gets the failed load result and its diagnostics.</summary>
    public ConfigurationLoadResult Result { get; } = result ?? throw new ArgumentNullException(nameof(result));
}
