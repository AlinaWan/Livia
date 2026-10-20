using System.Text.Json.Nodes;

namespace Livia.Services.Configuration;

/// <summary>Describes one field in a runtime configuration schema.</summary>
public sealed class ConfigurationField
{
    private readonly List<ConfigurationField> _children = [];

    /// <summary>Gets the JSON property name for this field.</summary>
    public string Name { get; }
    /// <summary>Gets the expected JSON value kind.</summary>
    public ConfigurationValueKind Kind { get; }
    /// <summary>Gets a copy of the default JSON value, if any.</summary>
    public JsonNode? DefaultValue { get; }
    /// <summary>Gets whether a numeric string may be evaluated as an expression.</summary>
    public bool AllowExpression { get; }
    /// <summary>Gets an optional description for generated documentation.</summary>
    public string? Description { get; }
    /// <summary>Gets a read-only view of child fields.</summary>
    public IReadOnlyList<ConfigurationField> Children => _children.AsReadOnly();
    /// <summary>Gets the optional custom validator. Return null for a valid value, otherwise an error message.</summary>
    public Func<JsonNode?, string?>? Validator { get; }

    /// <summary>Creates a scalar or array field.</summary>
    public ConfigurationField(
        string name,
        ConfigurationValueKind kind,
        object? defaultValue = null,
        bool allowExpression = false,
        string? description = null,
        Func<JsonNode?, string?>? validator = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        if (name.Contains('.'))
            throw new ArgumentException("Field names are individual JSON property names; define nesting with Object().", nameof(name));
        Name = name;
        Kind = kind;
        DefaultValue = ToNode(defaultValue);
        AllowExpression = allowExpression;
        Description = description;
        Validator = validator;
        if (kind == ConfigurationValueKind.Object)
            throw new ArgumentException("Use ConfigurationField.Object() to define an object field.", nameof(kind));
    }

    private ConfigurationField(string name, IEnumerable<ConfigurationField> children, object? defaultValue, string? description)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        if (name.Contains('.'))
            throw new ArgumentException("Field names are individual JSON property names; define nesting with Object().", nameof(name));
        Name = name;
        Kind = ConfigurationValueKind.Object;
        DefaultValue = ToNode(defaultValue);
        Description = description;
        foreach (ConfigurationField child in children)
            AddChild(child);
    }

    /// <summary>Creates a nested object field with its own schema.</summary>
    public static ConfigurationField Object(string name, params ConfigurationField[] children) =>
        new(name, children ?? throw new ArgumentNullException(nameof(children)), null, null);

    /// <summary>Adds a child field to an object field.</summary>
    public ConfigurationField AddChild(ConfigurationField child)
    {
        ArgumentNullException.ThrowIfNull(child);
        if (Kind != ConfigurationValueKind.Object)
            throw new InvalidOperationException("Only object fields can contain child fields.");
        if (_children.Any(existing => string.Equals(existing.Name, child.Name, StringComparison.Ordinal)))
            throw new ArgumentException($"A field named '{child.Name}' already exists in '{Name}'.", nameof(child));
        _children.Add(child);
        return this;
    }

    internal static JsonNode? ToNode(object? value) => value switch
    {
        null => null,
        JsonNode node => node.DeepClone(),
        _ => System.Text.Json.JsonSerializer.SerializeToNode(value)
    };
}
