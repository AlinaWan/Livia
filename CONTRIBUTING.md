# Contributing

## Versioning

Versions use the following format:

`Major.Minor.Build.Revision`

* **Major** — Breaking changes.
* **Minor** — New features that do not break existing behavior.
* **Build** — Changes that require a new build, such as bug fixes or other changes that affect the compiled application.
* **Revision** — Minor changes that do not require a new build, such as comment/documentation fixes or non-critical changes to flags and metadata.

For example:

* `1.2.0.0 → 1.2.0.1` — Fix a typo in a comment where a rebuild is not warranted.
* `1.2.0.1 → 1.2.1.0` — Fix a bug that requires rebuilding the application.
* `1.2.1.0 → 1.3.0.0` — Add a new feature.
* `1.3.0.0 → 2.0.0.0` — Make a breaking change.

### Livia

Livia's version reflects compatibility of the Livia engine and APIs with applications using Livia.

A breaking change to Livia's APIs or behavior that affects applications using Livia is a Major version change. Changes to external applications or games do not affect Livia's version unless accommodating those changes requires a breaking change to Livia itself.

If an older application could run on the new version of Livia and use the same APIs without modification or error, it is likely not a breaking change.

### Livia Reference Macros

Livia Reference Macro versions reflect compatibility with their respective target applications or games.

For example, if a game update changes its UI navigation and an existing automation sequence must be replaced, that warrants a Major version increment because existing behavior is no longer compatible.

If an older version of a Reference Macro would no longer be compatible or work correctly due to a game update, a Major version increment is likely warranted.
