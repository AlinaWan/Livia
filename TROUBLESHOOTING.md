# Troubleshooting Livia

This document covers common issues encountered when using Livia,
including analyzer diagnostics, build errors, and runtime problems.

## Analyzer Diagnostics

### LIVIA001 (Livia API requires explicit opt-in)

**Severity:** Error

Some Livia APIs require explicit developer opt-in because they may have additional requirements, system-level effects, or other implications that developers should explicitly acknowledge.

#### Diagnostic


```text
LIVIA001 The API '...' requires explicit opt-in. Set '...' to 'true' in the project file.
```

Example:

```text
LIVIA001 The API 'GetCookieValue' requires explicit opt-in. Set 'LiviaEnableBrowserCookieDecryption' to 'true' in the project file.
```

#### Why am I seeing this?

This diagnostic is reported when an application uses a Livia API that
requires explicit opt-in without enabling the corresponding MSBuild
property.

For example, an application may directly use an API marked as requiring
opt-in:

```csharp
var result = BrowserUtils.GetCookieValue();
```

without enabling the required property in the project file.

#### How do I fix it?

Enable the required opt-in by setting the MSBuild property named in the diagnostic message to `true`.

For a project file, add the property to a `<PropertyGroup>` in your `.csproj`:

```xml
<PropertyGroup>
    <LiviaEnableBrowserCookieDecryption>true</LiviaEnableBrowserCookieDecryption>
</PropertyGroup>
```

Alternatively, pass the property directly to the .NET CLI using the `-p:` option:

```text
dotnet build -p:LiviaEnableBrowserCookieDecryption=true
```

The exact property name is provided in the diagnostic message.
