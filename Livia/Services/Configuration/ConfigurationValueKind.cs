namespace Livia.Services.Configuration;

/// <summary>Describes the expected JSON value type for a configuration field.</summary>
public enum ConfigurationValueKind
{
    /// <summary>Any JSON value type.</summary>
    Any,
    /// <summary>A JSON string.</summary>
    String,
    /// <summary>A JSON Boolean.</summary>
    Boolean,
    /// <summary>An integral numeric value.</summary>
    Integer,
    /// <summary>A numeric value, including fractional values.</summary>
    Number,
    /// <summary>A nested JSON object.</summary>
    Object,
    /// <summary>A JSON array.</summary>
    Array
}
