using System.Diagnostics.CodeAnalysis;

namespace Livia.Services.Configuration;

/// <summary>Provides a configuration load result to failure event subscribers.</summary>
[Experimental("LIVIA002")]
public sealed class ConfigurationLoadResultEventArgs(ConfigurationLoadResult result) : EventArgs
{
    /// <summary>Gets the failed load result and its diagnostics.</summary>
    public ConfigurationLoadResult Result { get; } = result ?? throw new ArgumentNullException(nameof(result));
}