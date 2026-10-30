using System.Text.Json;
using System.Text.Json.Nodes;
using System.Diagnostics.CodeAnalysis;

namespace Livia.Services.Configuration;

/// <summary>Represents a JSON configuration document.</summary>
[Experimental("LIVIA002")]
public sealed class ConfigurationDocument
{
    private readonly JsonObject _root;
    /// <summary>Gets a deep copy of the underlying JSON object.</summary>
    public JsonObject Root => (JsonObject)_root.DeepClone();

    /// <summary>Creates a document from a JSON object, copying the supplied object.</summary>
    public ConfigurationDocument(JsonObject root) => _root = (JsonObject)(root ?? throw new ArgumentNullException(nameof(root))).DeepClone();

    /// <summary>Gets a typed value by a dot-separated path, or returns the supplied fallback if it is absent or incompatible.</summary>
    /// <typeparam name="T">The requested value type.</typeparam>
    public T? Get<T>(string path, T? fallback = default)
    {
        JsonNode? node = FindNode(path);
        if (node is null) return fallback;
        try { return node.Deserialize<T>(); }
        catch (JsonException) { return fallback; }
        catch (NotSupportedException) { return fallback; }
    }

    /// <summary>Sets a value at a dot-separated path, creating intermediate objects when necessary.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    public void Set<T>(string path, T? value)
    {
        string[] segments = SplitPath(path);
        JsonObject current = _root;
        for (int i = 0; i < segments.Length - 1; i++)
        {
            if (current[segments[i]] is not JsonObject next)
            {
                next = new JsonObject();
                current[segments[i]] = next;
            }
            current = next;
        }
        current[segments[^1]] = ConfigurationField.ToNode(value);
    }

    /// <summary>Returns a deep copy of the document's JSON representation.</summary>
    public JsonObject ToJsonObject() => (JsonObject)_root.DeepClone();

    /// <summary>Serializes the document to JSON.</summary>
    public string ToJson(bool indented = true) => _root.ToJsonString(new JsonSerializerOptions { WriteIndented = indented });

    /// <summary>Creates a document from JSON text.</summary>
    public static ConfigurationDocument Parse(string json)
    {
        ArgumentNullException.ThrowIfNull(json);
        return new ConfigurationDocument(JsonNode.Parse(json) as JsonObject ?? throw new JsonException("The configuration root must be a JSON object."));
    }

    internal JsonNode? FindNode(string path)
    {
        JsonNode? current = _root;
        foreach (string segment in SplitPath(path))
        {
            if (current is not JsonObject obj || !obj.TryGetPropertyValue(segment, out current)) return null;
        }
        return current;
    }

    internal static string[] SplitPath(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        string[] segments = path.Split('.', StringSplitOptions.None);
        if (segments.Any(string.IsNullOrWhiteSpace)) throw new ArgumentException("Path contains an empty segment.", nameof(path));
        return segments;
    }
}
