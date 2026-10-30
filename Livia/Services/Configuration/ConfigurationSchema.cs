using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Diagnostics.CodeAnalysis;

namespace Livia.Services.Configuration;

/// <summary>Defines the fields and defaults accepted by a configuration document.</summary>
[Experimental("LIVIA002")]
public sealed class ConfigurationSchema
{
    private readonly List<ConfigurationField> _fields = [];

    /// <summary>Gets the optional application or schema name.</summary>
    public string? Name { get; }
    /// <summary>Gets a read-only view of top-level fields.</summary>
    public IReadOnlyList<ConfigurationField> Fields => _fields.AsReadOnly();

    /// <summary>Creates an empty schema.</summary>
    public ConfigurationSchema(string? name = null) => Name = name;

    /// <summary>Adds a top-level field.</summary>
    public ConfigurationSchema Add(ConfigurationField field)
    {
        ArgumentNullException.ThrowIfNull(field);
        if (_fields.Any(existing => string.Equals(existing.Name, field.Name, StringComparison.Ordinal)))
            throw new ArgumentException($"A field named '{field.Name}' already exists.", nameof(field));
        _fields.Add(field);
        return this;
    }

    /// <summary>Creates a fresh document containing schema defaults.</summary>
    public ConfigurationDocument CreateDefaultDocument()
    {
        JsonObject root = new();
        foreach (ConfigurationField field in _fields)
            root[field.Name] = CreateDefaultNode(field);
        return new ConfigurationDocument(root);
    }

    /// <summary>Builds a schema from public readable properties and their initialized default values.</summary>
    /// <typeparam name="T">The configuration class or record type.</typeparam>
    /// <remarks>Nested POCOs are traversed recursively. Properties of unsupported types are represented as JSON values of kind Any.</remarks>
    public static ConfigurationSchema FromType<T>(string? name = null) where T : new()
    {
        T instance = new();
        ConfigurationSchema schema = new(name ?? typeof(T).Name);
        foreach (PropertyInfo property in typeof(T).GetProperties(BindingFlags.Instance | BindingFlags.Public))
        {
            if (!property.CanRead || property.GetIndexParameters().Length != 0 || property.GetMethod?.IsPublic != true)
                continue;
            object? value = property.GetValue(instance);
            schema.Add(CreateFieldFromProperty(property, value));
        }
        return schema;
    }

    private static ConfigurationField CreateFieldFromProperty(PropertyInfo property, object? value)
    {
        Type type = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;
        if (value is not null && !IsScalar(type) && type != typeof(string) && !typeof(System.Collections.IEnumerable).IsAssignableFrom(type))
        {
            List<ConfigurationField> children = [];
            foreach (PropertyInfo child in type.GetProperties(BindingFlags.Instance | BindingFlags.Public))
            {
                if (!child.CanRead || child.GetIndexParameters().Length != 0 || child.GetMethod?.IsPublic != true)
                    continue;
                children.Add(CreateFieldFromProperty(child, child.GetValue(value)));
            }
            return ConfigurationField.Object(property.Name, children.ToArray());
        }
        return new ConfigurationField(property.Name, GetKind(type), value);
    }

    private static bool IsScalar(Type type) => type.IsPrimitive || type.IsEnum || type == typeof(decimal) || type == typeof(DateTime) || type == typeof(DateTimeOffset) || type == typeof(Guid);

    private static ConfigurationValueKind GetKind(Type type)
    {
        if (type == typeof(string) || type == typeof(char) || type == typeof(Guid) || type == typeof(DateTime) || type == typeof(DateTimeOffset)) return ConfigurationValueKind.String;
        if (type == typeof(bool)) return ConfigurationValueKind.Boolean;
        if (type.IsEnum || type == typeof(byte) || type == typeof(sbyte) || type == typeof(short) || type == typeof(ushort) || type == typeof(int) || type == typeof(uint) || type == typeof(long) || type == typeof(ulong)) return ConfigurationValueKind.Integer;
        if (type == typeof(float) || type == typeof(double) || type == typeof(decimal)) return ConfigurationValueKind.Number;
        if (typeof(System.Collections.IEnumerable).IsAssignableFrom(type)) return ConfigurationValueKind.Array;
        return ConfigurationValueKind.Any;
    }

    private static JsonNode? CreateDefaultNode(ConfigurationField field)
    {
        if (field.Kind == ConfigurationValueKind.Object)
        {
            JsonObject obj = field.DefaultValue is JsonObject supplied ? (JsonObject)supplied.DeepClone() : new JsonObject();
            foreach (ConfigurationField child in field.Children)
                if (!obj.ContainsKey(child.Name)) obj[child.Name] = CreateDefaultNode(child);
            return obj;
        }
        return field.DefaultValue?.DeepClone();
    }
}
