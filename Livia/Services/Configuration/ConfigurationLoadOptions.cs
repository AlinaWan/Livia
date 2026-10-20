namespace Livia.Services.Configuration;

/// <summary>Controls configuration loading, expression evaluation, and live reload behavior.</summary>
public sealed class ConfigurationLoadOptions
{
    /// <summary>Gets or sets whether fields marked as expression-capable may evaluate numeric strings. Defaults to false.</summary>
    public bool EvaluateExpressions { get; set; }
    /// <summary>Gets the caller-provided variables available to expressions.</summary>
    public IDictionary<string, double> ExpressionVariables { get; } = new Dictionary<string, double>(StringComparer.Ordinal);
    /// <summary>Gets or sets the debounce interval used by live reload. Defaults to 250 milliseconds.</summary>
    public TimeSpan DebounceInterval { get; set; } = TimeSpan.FromMilliseconds(250);
    /// <summary>Gets or sets how many times a reload retries after a transient file read or parse failure.</summary>
    public int ReloadRetryCount { get; set; } = 3;
    /// <summary>Gets or sets the delay between reload retries.</summary>
    public TimeSpan ReloadRetryDelay { get; set; } = TimeSpan.FromMilliseconds(150);
}
