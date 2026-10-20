using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Livia.Services.Configuration;

/// <summary>Loads, validates, saves, and optionally watches schema-defined JSON configuration files.</summary>
public sealed class ConfigurationManager : IDisposable
{
    private readonly ConfigurationSchema _schema;
    private readonly ConfigurationLoadOptions _options;
    private readonly FileSystemWatcherService _watcher = new();
    private readonly SemaphoreSlim _reloadGate = new(1, 1);
    private CancellationTokenSource? _debounceCancellation;
    private string? _currentPath;
    private ConfigurationDocument? _current;
    private bool _disposed;
    private readonly List<Action<ConfigurationDocument>> _reloadedCallbacks = [];
    private readonly List<Action<ConfigurationLoadResult>> _failedCallbacks = [];

    /// <summary>Raised after a successfully loaded document becomes the active configuration.</summary>
    public event EventHandler<ConfigurationDocumentEventArgs>? Reloaded;
    /// <summary>Raised when a load or live reload fails to produce a document.</summary>
    public event EventHandler<ConfigurationLoadResultEventArgs>? ReloadFailed;
    /// <summary>Gets the most recently loaded valid document, or null before the first successful load.</summary>
    public ConfigurationDocument? Current => _current is null ? null : new ConfigurationDocument(_current.ToJsonObject());
    /// <summary>Gets the active file path, if one has been loaded or watched.</summary>
    public string? CurrentPath => _currentPath;

    /// <summary>Creates a manager for a schema and optional loading/reload settings.</summary>
    public ConfigurationManager(ConfigurationSchema schema, ConfigurationLoadOptions? options = null)
    {
        _schema = schema ?? throw new ArgumentNullException(nameof(schema));
        _options = options ?? new ConfigurationLoadOptions();
        if (_options.DebounceInterval < TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(options), "DebounceInterval cannot be negative.");
        if (_options.ReloadRetryCount < 0) throw new ArgumentOutOfRangeException(nameof(options), "ReloadRetryCount cannot be negative.");
        _watcher.Changed += OnFileChanged;
        _watcher.Error += OnWatcherError;
    }

    /// <summary>Loads and validates a JSON file. A failed load does not replace the active document.</summary>
    public ConfigurationLoadResult Load(string path)
    {
        ThrowIfDisposed();
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        string fullPath = Path.GetFullPath(path);
        try
        {
            string json = File.ReadAllText(fullPath);
            ConfigurationLoadResult result = LoadJson(json);
            if (result.Success && result.Document is not null)
            {
                _current = result.Document;
                _currentPath = fullPath;
                RaiseReloaded(_current);
            }
            else RaiseReloadFailed(result);
            return result;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
        {
            ConfigurationLoadResult result = new(false, null, [new(ConfigurationDiagnosticSeverity.Error, "CONFIG_READ_FAILED", ex.Message)]);
            RaiseReloadFailed(result);
            return result;
        }
    }

    /// <summary>Loads JSON text using schema defaults and field-level validation.</summary>
    public ConfigurationLoadResult LoadJson(string json)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(json);
        JsonObject input;
        try { input = JsonNode.Parse(json) as JsonObject ?? throw new JsonException("The configuration root must be a JSON object."); }
        catch (JsonException ex)
        {
            return new(false, null, [new(ConfigurationDiagnosticSeverity.Error, "INVALID_JSON", ex.Message)]);
        }

        List<ConfigurationDiagnostic> diagnostics = [];
        JsonObject output = new();
        foreach (ConfigurationField field in _schema.Fields)
        {
            input.TryGetPropertyValue(field.Name, out JsonNode? source);
            output[field.Name] = NormalizeField(field, source, $"{field.Name}", diagnostics, sourceExists: input.ContainsKey(field.Name));
        }
        // Unknown properties are deliberately ignored; output is restricted to the declared schema.
        return new(true, new ConfigurationDocument(output), diagnostics);
    }

    /// <summary>Loads a file and deserializes the resulting document into a strongly typed object.</summary>
    /// <typeparam name="T">The target configuration type.</typeparam>
    public (T? Value, ConfigurationLoadResult Result) LoadTyped<T>(string path)
    {
        ConfigurationLoadResult result = Load(path);
        if (!result.Success || result.Document is null) return (default, result);
        try { return (result.Document.ToJsonObject().Deserialize<T>(), result); }
        catch (JsonException ex)
        {
            ConfigurationLoadResult failed = new(false, null, result.Diagnostics.Append(new(ConfigurationDiagnosticSeverity.Error, "TYPED_DESERIALIZATION_FAILED", ex.Message)));
            RaiseReloadFailed(failed);
            return (default, failed);
        }
    }

    /// <summary>Saves a document to a JSON file, creating the containing directory when needed.</summary>
    public void Save(string path, ConfigurationDocument document, bool indented = true)
    {
        ThrowIfDisposed();
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentNullException.ThrowIfNull(document);
        string fullPath = Path.GetFullPath(path);
        string? directory = Path.GetDirectoryName(fullPath);
        if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);
        File.WriteAllText(fullPath, document.ToJson(indented));
    }

    /// <summary>Creates and saves a JSON file containing schema defaults.</summary>
    public ConfigurationDocument CreateDefaultFile(string path, bool indented = true)
    {
        ConfigurationDocument document = _schema.CreateDefaultDocument();
        Save(path, document, indented);
        return document;
    }

    /// <summary>Starts watching a configuration file and reloads it after debounced filesystem changes.</summary>
    public void Watch(string path)
    {
        ThrowIfDisposed();
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        _currentPath = Path.GetFullPath(path);
        _watcher.StartFile(_currentPath);
    }

    /// <summary>Stops watching the current configuration file.</summary>
    public void StopWatching() { _debounceCancellation?.Cancel(); _watcher.Stop(); }

    /// <summary>Registers a callback invoked after a successful load or reload.</summary>
    public IDisposable RegisterReloadedCallback(Action<ConfigurationDocument> callback)
    {
        ArgumentNullException.ThrowIfNull(callback); _reloadedCallbacks.Add(callback); return new CallbackToken(() => _reloadedCallbacks.Remove(callback));
    }

    /// <summary>Registers a callback invoked when a load or reload fails.</summary>
    public IDisposable RegisterReloadFailedCallback(Action<ConfigurationLoadResult> callback)
    {
        ArgumentNullException.ThrowIfNull(callback); _failedCallbacks.Add(callback); return new CallbackToken(() => _failedCallbacks.Remove(callback));
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _debounceCancellation?.Cancel();
        _debounceCancellation?.Dispose();
        _watcher.Changed -= OnFileChanged;
        _watcher.Error -= OnWatcherError;
        _watcher.Dispose();
        _reloadGate.Dispose();
        _reloadedCallbacks.Clear();
        _failedCallbacks.Clear();
        GC.SuppressFinalize(this);
    }

    private JsonNode? NormalizeField(ConfigurationField field, JsonNode? source, string path, List<ConfigurationDiagnostic> diagnostics, bool sourceExists)
    {
        if (!sourceExists || source is null)
        {
            if (sourceExists && source is null) diagnostics.Add(new(ConfigurationDiagnosticSeverity.Warning, "NULL_DEFAULTED", "Null value replaced with the schema default.", path));
            return GetDefault(field);
        }

        if (field.Kind == ConfigurationValueKind.Object)
        {
            if (source is not JsonObject sourceObject)
            {
                diagnostics.Add(new(ConfigurationDiagnosticSeverity.Error, "INVALID_TYPE", "Expected an object; the schema default was used.", path));
                return GetDefault(field);
            }
            JsonObject result = field.DefaultValue is JsonObject defaultObject ? (JsonObject)defaultObject.DeepClone() : new JsonObject();
            foreach (ConfigurationField child in field.Children)
            {
                sourceObject.TryGetPropertyValue(child.Name, out JsonNode? childSource);
                result[child.Name] = NormalizeField(child, childSource, $"{path}.{child.Name}", diagnostics, sourceObject.ContainsKey(child.Name));
            }
            return result;
        }

        JsonNode? candidate = source.DeepClone();
        if (_options.EvaluateExpressions && field.AllowExpression && (field.Kind is ConfigurationValueKind.Integer or ConfigurationValueKind.Number) &&
            candidate is JsonValue stringValue && stringValue.TryGetValue<string>(out string? expression))
        {
            try
            {
                double evaluated = new ConfigurationExpressionEvaluator(_options.ExpressionVariables.ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.Ordinal)).Evaluate(expression!);
                candidate = JsonValue.Create(evaluated);
            }
            catch (Exception ex) when (ex is FormatException or ArithmeticException or ArgumentException)
            {
                diagnostics.Add(new(ConfigurationDiagnosticSeverity.Error, "INVALID_EXPRESSION", ex.Message, path));
                return GetDefault(field);
            }
        }

        if (!MatchesKind(candidate, field.Kind, out JsonNode? converted))
        {
            diagnostics.Add(new(ConfigurationDiagnosticSeverity.Error, "INVALID_TYPE", $"Value does not match expected kind '{field.Kind}'; the schema default was used.", path));
            return GetDefault(field);
        }
        string? validationError = field.Validator?.Invoke(converted?.DeepClone());
        if (validationError is not null)
        {
            diagnostics.Add(new(ConfigurationDiagnosticSeverity.Error, "VALIDATION_FAILED", validationError, path));
            return GetDefault(field);
        }
        return converted;
    }

    private static JsonNode? GetDefault(ConfigurationField field)
    {
        if (field.Kind == ConfigurationValueKind.Object)
        {
            JsonObject obj = field.DefaultValue is JsonObject supplied ? (JsonObject)supplied.DeepClone() : new JsonObject();
            foreach (ConfigurationField child in field.Children)
                if (!obj.ContainsKey(child.Name)) obj[child.Name] = GetDefault(child);
            return obj;
        }
        return field.DefaultValue?.DeepClone();
    }

    private static bool MatchesKind(JsonNode? source, ConfigurationValueKind kind, out JsonNode? converted)
    {
        converted = source?.DeepClone();
        if (source is null || kind == ConfigurationValueKind.Any) return true;
        try
        {
            switch (kind)
            {
                case ConfigurationValueKind.String:
                    if (source is JsonValue && source.GetValueKind() == System.Text.Json.JsonValueKind.String) return true;
                    return false;
                case ConfigurationValueKind.Boolean:
                    return source.GetValueKind() is System.Text.Json.JsonValueKind.True or System.Text.Json.JsonValueKind.False;
                case ConfigurationValueKind.Integer:
                    if (source.GetValueKind() != System.Text.Json.JsonValueKind.Number) return false;
                    double integer = source.GetValue<double>();
                    if (!double.IsFinite(integer) || integer != Math.Truncate(integer) || integer < long.MinValue || integer > long.MaxValue) return false;
                    converted = JsonValue.Create((long)integer); return true;
                case ConfigurationValueKind.Number:
                    if (source.GetValueKind() != System.Text.Json.JsonValueKind.Number) return false;
                    double number = source.GetValue<double>();
                    if (!double.IsFinite(number)) return false;
                    converted = JsonValue.Create(number); return true;
                case ConfigurationValueKind.Array: return source is JsonArray;
                case ConfigurationValueKind.Object: return source is JsonObject;
                default: return false;
            }
        }
        catch (Exception ex) when (ex is InvalidOperationException or FormatException or OverflowException) { return false; }
    }

    private void OnFileChanged(object? sender, FileSystemChangeEventArgs args)
    {
        if (args.Kind is FileSystemChangeKind.Deleted || _currentPath is null) return;
        CancellationTokenSource cancellation = new();
        CancellationTokenSource? previous = Interlocked.Exchange(ref _debounceCancellation, cancellation);
        previous?.Cancel();
        previous?.Dispose();
        _ = ReloadAfterDelayAsync(_currentPath, cancellation.Token);
    }

    private async Task ReloadAfterDelayAsync(string path, CancellationToken cancellationToken)
    {
        try
        {
            await Task.Delay(_options.DebounceInterval, cancellationToken).ConfigureAwait(false);
            await _reloadGate.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                ConfigurationLoadResult? result = null;
                for (int attempt = 0; attempt <= _options.ReloadRetryCount; attempt++)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    result = LoadWithoutNotifications(path);
                    if (result.Success) break;
                    if (attempt < _options.ReloadRetryCount) await Task.Delay(_options.ReloadRetryDelay, cancellationToken).ConfigureAwait(false);
                }
                if (result is not null)
                {
                    if (result.Success && result.Document is not null)
                    {
                        _current = result.Document;
                        RaiseReloaded(_current);
                    }
                    else RaiseReloadFailed(result);
                }
            }
            finally { _reloadGate.Release(); }
        }
        catch (OperationCanceledException) { }
        catch (ObjectDisposedException) { }
    }

    private ConfigurationLoadResult LoadWithoutNotifications(string path)
    {
        try { return LoadJson(File.ReadAllText(path)); }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        { return new(false, null, [new(ConfigurationDiagnosticSeverity.Error, "CONFIG_READ_FAILED", ex.Message)]); }
    }

    private void OnWatcherError(object? sender, FileSystemChangeEventArgs args)
    {
        ConfigurationLoadResult result = new(false, null, [new(ConfigurationDiagnosticSeverity.Error, "WATCHER_ERROR", args.Exception?.Message ?? "Filesystem watcher error.")]);
        RaiseReloadFailed(result);
    }

    private void RaiseReloaded(ConfigurationDocument document)
    {
        ConfigurationDocument copy = new(document.ToJsonObject());
        Reloaded?.Invoke(this, new ConfigurationDocumentEventArgs(copy));
        foreach (Action<ConfigurationDocument> callback in _reloadedCallbacks.ToArray())
            try { callback(new ConfigurationDocument(copy.ToJsonObject())); } catch { }
    }

    private void RaiseReloadFailed(ConfigurationLoadResult result)
    {
        ReloadFailed?.Invoke(this, new ConfigurationLoadResultEventArgs(result));
        foreach (Action<ConfigurationLoadResult> callback in _failedCallbacks.ToArray())
            try { callback(result); } catch { }
    }

    private void ThrowIfDisposed() { if (_disposed) throw new ObjectDisposedException(nameof(ConfigurationManager)); }
    private sealed class CallbackToken(Action dispose) : IDisposable { private Action? _dispose = dispose; public void Dispose() => Interlocked.Exchange(ref _dispose, null)?.Invoke(); }
}
