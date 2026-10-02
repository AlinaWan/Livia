# Troubleshooting Livia

This document covers common issues encountered when using Livia,
including analyzer diagnostics, build errors, and runtime problems.

## Analyzer Diagnostics

### LIVIA001 (Livia API requires explicit opt-in)

**Severity:** Error

Some Livia APIs require explicit developer opt-in because they may have additional requirements, system-level effects, or other implications that developers should explicitly acknowledge.

#### Diagnostic

```text
error LIVIA001: The API '...' requires explicit opt-in. Set '...' to 'true' in the project file.
```

Example:

```text
error LIVIA001: The API 'GetCookieValue' requires explicit opt-in. Set 'LiviaEnableBrowserCookieDecryption' to 'true' in the project file.
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
    <LiviaEnable...>true</LiviaEnable...>
</PropertyGroup>
```

Alternatively, pass the property directly to the .NET CLI using the `-p:` option:

```text
dotnet build -p:LiviaEnable...=true
```

The exact property name is provided in the diagnostic message.

### LIVIA002 (Livia API is experimental)

**Severity:** Warning

Some Livia APIs are marked as experimental because they are still under
development and may change significantly, be redesigned, or be removed
in future releases. Experimental APIs should be used with the expectation
that source or behavioral changes may be required when updating Livia.

Livia reports this diagnostic for APIs marked with C#'s
`[Experimental]` attribute.

#### Diagnostic

```text
warning LIVIA002: '...' is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
````

Example:

```text
warning LIVIA002: 'Livia.Services.Federation.RobloxFederationClient' is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
```

#### Why am I seeing this?

This diagnostic is reported when an application uses a Livia API that has
been marked as experimental.

Experimental APIs are provided for evaluation and early use but are not
considered stable Livia APIs. Their APIs, behavior, or availability may
change between releases.

For example, an application may directly use an experimental API:

```csharp
var client = new RobloxFederationClient();
```

without explicitly suppressing the experimental diagnostic.

#### How do I fix it?

If you are comfortable using the experimental API and accept that it may
change, break, or be removed in a future Livia release, suppress `LIVIA002`.

You can suppress the diagnostic for a specific use with C#'s standard
`Experimental` diagnostic suppression mechanisms.

For example, to suppress `LIVIA002` for an entire project, add it to
`NoWarn` in the project file:

```xml
<PropertyGroup>
    <NoWarn>$(NoWarn);LIVIA002</NoWarn>
</PropertyGroup>
```

Alternatively, suppress it for a specific location using the standard
C# pragma:

```csharp
#pragma warning disable LIVIA002

var foo = SomeExperimentalApi();

#pragma warning restore LIVIA002
```

Suppressing this diagnostic does not make the API stable or guarantee
compatibility with future Livia releases.