# Contributing

## Versioning

Versions use the following format:

`Major.Minor.Build.Revision`

### Livia

Livia versions are automatically generated from the manually maintained `Major.Minor` version and the UTC date and time of the release.

* **Major** — Breaking changes to Livia's APIs or behavior.
* **Minor** — New features that do not break existing behavior.
* **Build** — The number of days elapsed since `2000-01-01` UTC.
* **Revision** — The number of two-second intervals elapsed since midnight UTC.

For Livia releases, `Major.Minor` is manually maintained in source control. `Build` and `Revision` are generated automatically when the release is created.

For example:

* `1.22.1234.5000 → 1.22.1235.0` — A new release on the following UTC day.
* `1.22.1234.5000 → 1.22.1234.5001` — A subsequent release on the same UTC day.

A breaking change to Livia's APIs or behavior that affects applications using Livia is a Major version change. Changes to external applications or games do not affect Livia's version unless accommodating those changes requires a breaking change to Livia itself.

If an older application could run on the new version of Livia and use the same APIs without modification or error, it is likely not a breaking change.

### Livia Reference Macros

Livia Reference Macro versions reflect compatibility with their respective target applications or games.

Reference Macro versions are manually maintained in source control. The full `Major.Minor.Build.Revision` version is explicitly changed and committed when the macro is updated.

* **Major** — Changes that break compatibility with the target application or game.
* **Minor** — New functionality or behavior that does not break existing compatibility.
* **Build** — Changes that alter the compiled application's behavior or performance.
* **Revision** — Changes that preserve the application's behavior and performance, such as comment or documentation changes, metadata changes, or other changes that do not affect runtime behavior.

For example:

* `1.2.0.0 → 1.2.0.1` — Fix a typo in documentation or make another non-runtime change that does not affect behavior or performance.
* `1.2.0.1 → 1.2.1.0` — Fix a bug or make another change that alters the application's behavior or performance.
* `1.2.1.0 → 1.3.0.0` — Add a new feature without breaking existing compatibility.
* `1.3.0.0 → 2.0.0.0` — Make a change that breaks compatibility with the target application or game.

If a game update changes its UI navigation and an existing automation sequence must be replaced, that is a Major version increment because the existing Reference Macro is no longer compatible with the target application.

If an older version of a Reference Macro would no longer be compatible or work correctly due to a game update, the Reference Macro's Major version should be incremented.

Reference Macros are not compiled, packaged, or released by CI, and no GitHub Releases are created for them.