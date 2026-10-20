## Configuration

Livia provides a configuration system for defining, loading, validating, and saving JSON configuration files. Schemas can be defined at runtime or generated from strongly typed C# classes.

The configuration system supports nested objects, default values, optional mathematical expression evaluation, and live reloading. A generic `FileSystemWatcherService` is also available for monitoring files and directories independently of configuration management.

### Defining a Configuration Schema

`ConfigurationSchema` defines the structure of a configuration document, including its fields, types, and default values.

For example, the following schema defines several settings for an automation application:

```csharp
using Livia.Configuration;

ConfigurationSchema schema = new("Storage Hunters Tool")
    .Add(ConfigurationField.Object("automation_settings",
        new ConfigurationField("click_cooldown_ms", ConfigurationValueKind.Integer, 100),
        new ConfigurationField("fullscreen_y_offset", ConfigurationValueKind.Integer, 40, allowExpression: true),
        new ConfigurationField("min_target_width_pct", ConfigurationValueKind.Number, 5.0),
        new ConfigurationField("allow_target_bleed", ConfigurationValueKind.Boolean, true)))
    .Add(ConfigurationField.Object("behavior_settings",
        new ConfigurationField("enable_discord_rpc", ConfigurationValueKind.Boolean, false)));
```

Each field specifies its name, value type, and default value. Fields can be grouped into nested objects using `ConfigurationField.Object`.

The resulting configuration structure is equivalent to the following JSON:

```json
{
    "automation_settings": {
        "click_cooldown_ms": 100,
        "fullscreen_y_offset": 40,
        "min_target_width_pct": 5.0,
        "allow_target_bleed": true
    },
    "behavior_settings": {
        "enable_discord_rpc": false
    }
}
```

Fields that are missing from a loaded configuration are initialized with their schema defaults. Unknown fields are ignored by the schema-driven loader.

The schema can also be used to create a default configuration file:

```csharp
using ConfigurationManager manager = new(schema);

manager.CreateDefaultFile("config.json");
```

This is useful when an application needs to generate an initial configuration file that users can customize.

---

### Loading a Configuration

`ConfigurationManager` loads configuration files according to the supplied schema.

For example:

```csharp
ConfigurationLoadResult result = manager.Load("config.json");

if (result.Success)
{
    int cooldown = result.Document.Get(
        "automation_settings.click_cooldown_ms",
        100);
}
```

Configuration values can be accessed using dot-separated paths. The generic `Get<T>` method returns the requested type and accepts a fallback value when the field is unavailable.

The configuration document also exposes its underlying JSON representation for callers that need to inspect or manipulate the document directly.

Loading applies valid values individually. Invalid fields can fall back to their defaults, with diagnostics included in the load result. This allows a configuration to remain usable when an individual setting is malformed.

For example, a configuration containing an invalid numeric value can report the problem without necessarily preventing unrelated settings from loading.

Applications should inspect the returned diagnostics when they need to report configuration errors to the user or write them to a log.

---

### Using Strongly Typed Configurations

For applications that prefer strongly typed settings, `ConfigurationSchema.FromType<T>()` generates a schema from a C# class.

Initialized property values are used as defaults.

For example:

```csharp
public sealed class AppSettings
{
    public bool Enabled { get; set; } = true;

    public int CooldownMs { get; set; } = 100;

    public WindowSettings Window { get; set; } = new();
}

public sealed class WindowSettings
{
    public int Width { get; set; } = 800;

    public int Height { get; set; } = 600;
}
```

The schema can be generated and used to load the configuration:

```csharp
ConfigurationSchema schema =
    ConfigurationSchema.FromType<AppSettings>();

using ConfigurationManager manager = new(schema);

(AppSettings? settings, ConfigurationLoadResult result) =
    manager.LoadTyped<AppSettings>("config.json");

if (result.Success && settings is not null)
{
    Console.WriteLine($"Window width: {settings.Window.Width}");
}
```

This approach is useful when an application wants compile-time property names and types rather than retrieving individual values by path.

Runtime-defined schemas and strongly typed configurations serve different purposes; applications can choose whichever is more appropriate for their settings.

---

### Evaluating Mathematical Expressions

Mathematical expression evaluation is optional and disabled by default.

When enabled, fields that permit expressions can contain calculations instead of literal values. This is useful for settings that depend on screen dimensions or other application-specific values.

For example, a configuration can specify:

```json
{
    "automation_settings": {
        "fullscreen_y_offset": "SCREEN_HEIGHT * (40 / 1080)"
    }
}
```

The application can provide the variables used by these expressions:

```csharp
ConfigurationLoadOptions options = new()
{
    EvaluateExpressions = true
};

options.ExpressionVariables["SCREEN_WIDTH"] = SystemUtils.GetSystemMetric(SystemMetric.ScreenWidth);
options.ExpressionVariables["SCREEN_HEIGHT"] = SystemUtils.GetSystemMetric(SystemMetric.ScreenHeight);

using ConfigurationManager manager = new(schema, options);

ConfigurationLoadResult result = manager.Load("config.json");
```

In this example, `fullscreen_y_offset` is evaluated using the supplied screen height because the field was explicitly configured to allow expressions.

The evaluator supports arithmetic, integer division, exponentiation, bitwise operations, unary operators, parentheses, and a restricted set of numeric functions.

Expressions are evaluated using a dedicated evaluator rather than arbitrary C# code execution.

If an expression cannot be evaluated or produces a value incompatible with the field's declared type, the field can fall back to its default and report a diagnostic.

Expression evaluation is particularly useful for coordinates, offsets, dimensions, and bitmask values. Literal values remain appropriate for settings that do not require calculations.

<details>
<summary>Full list of supported operators and functions</summary>

### Operators

| Operator | Description | Example |
| :--- | :--- | :--- |
| `+` | Addition | `100 + 50` |
| `-` | Subtraction | `1920 - 100` |
| `*` | Multiplication | `10 * 2` |
| `/` | Division | `1920 / 2` |
| `//` | Floor division; rounds the quotient down toward negative infinity | `100 // 3` |
| `%` | Remainder | `10 % 3` |
| `**` | Exponentiation | `2 ** 8` |
| `+x` | Unary plus | `+100` |
| `-x` | Unary negation | `-50` |
| `\|` | Bitwise OR; requires signed 64-bit integer operands | `4 \| 2` |
| `&` | Bitwise AND; requires signed 64-bit integer operands | `6 & 3` |
| `^` | Bitwise XOR; requires signed 64-bit integer operands | `5 ^ 3` |
| `(...)` | Groups an expression | `(100 + 50) * 2` |

### Functions

| Function | Description | Example |
| :--- | :--- | :--- |
| `int(x)` | Truncates a number toward zero | `int(10.8)` → `10` |
| `round(x)` | Rounds to the nearest integer using midpoint-to-even rounding | `round(2.5)` → `2`, `round(3.5)` → `4` |
| `round(x, digits)` | Rounds to the specified number of decimal places, from `0` through `15`, using midpoint-to-even rounding | `round(3.14159, 2)` → `3.14` |
| `min(x, ...)` | Returns the smallest of one or more numbers | `min(100, 50, 75)` → `50` |
| `max(x, ...)` | Returns the largest of one or more numbers | `max(100, 50, 75)` → `100` |
| `abs(x)` | Returns the absolute value of a number | `abs(-50)` → `50` |

### Additional notes

- Variables can be referenced by name when supplied through the evaluator's variable dictionary.
- Numeric literals support decimal notation, scientific notation, and hexadecimal notation (for example, `0x1B`).
- Arithmetic uses `double` precision. Bitwise operations require finite, integral values representable as signed 64-bit integers.
- The evaluator does not execute arbitrary code. Unsupported operators, functions, variables, and invalid expressions produce errors.
- The final expression result must be finite.

</details>

---

### Live Configuration Reloading

`ConfigurationManager` can monitor a configuration file and reload it when changes are detected.

For example:

```csharp
using ConfigurationManager manager = new(schema);

manager.Reloaded += (_, args) =>
{
    int cooldown = args.Document.Get(
        "automation_settings.click_cooldown_ms",
        100);

    Console.WriteLine($"Click cooldown updated: {cooldown} ms");
};

manager.ReloadFailed += (_, args) =>
{
    foreach (ConfigurationDiagnostic diagnostic in args.Result.Diagnostics)
    {
        Console.Error.WriteLine(
            $"{diagnostic.Severity} {diagnostic.Path}: {diagnostic.Message}");
    }
};

ConfigurationLoadResult result = manager.Load("config.json");

if (result.Success)
{
    manager.Watch("config.json");
}
```

The `Reloaded` event allows an application to react to successfully loaded configuration changes. The `ReloadFailed` event reports unsuccessful reloads and their diagnostics.

The manager handles debouncing and retries where necessary to accommodate editors that write files in multiple operations.

If a reload fails, the last valid configuration is retained rather than replaced with an incomplete document. This prevents temporary file-write errors from resetting application settings.

The application remains responsible for applying changed settings to its own runtime state. For example, a WPF application may need to dispatch updates to the UI thread, while an automation application may need to refresh cached coordinates or restart a running operation.

Configuration loading and application-specific state management are intentionally separate.

---

### Monitoring Files and Directories

`FileSystemWatcherService` provides a generic wrapper around .NET's `FileSystemWatcher`. It can monitor an individual file or an entire directory and report filesystem changes without depending on the configuration system.

For example, the following monitors a directory for JSON file changes:

```csharp
using Livia.Services;

using FileSystemWatcherService watcher = new();

watcher.Changed += (_, args) =>
{
    Console.WriteLine($"{args.Kind}: {args.FullPath}");
};

watcher.Error += (_, args) =>
{
    Console.Error.WriteLine(args.Exception?.Message);
};

watcher.StartDirectory(@"C:\Projects\MyApp", "*.json");
```

The watcher reports relevant filesystem operations, including file creation, modification, deletion, and renaming. Errors are exposed through the `Error` event.

To monitor a single file instead, use the file-watching method provided by the service.

The watcher is independent of configuration management and can therefore be used for other purposes, such as monitoring asset directories, scripts, or application data files.

Dispose the watcher when monitoring is no longer required. For long-running applications, keep the watcher alive for as long as its notifications are needed.

---

### Implementation Considerations

A few details are worth considering when integrating configuration management into an application:

- **Defaults:** Missing fields receive their schema-defined defaults, allowing configuration files to omit settings that do not need customization.
- **Validation:** Invalid fields can fall back individually, with diagnostics available for logging or display.
- **Expressions:** Mathematical evaluation must be explicitly enabled, and individual fields must permit expressions.
- **Live reloads:** The last valid configuration is retained when a reload fails.
- **Threading:** Filesystem notifications and reload callbacks should not be assumed to execute on the WPF UI thread.
- **Runtime state:** Successfully loading a configuration does not automatically update application-specific state; the application determines how to apply changes.

These features allow applications to share configuration infrastructure without requiring a common schema, settings class, or runtime lifecycle.