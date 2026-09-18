# Contributing

## Versioning

Versions use the following format:

`Major.Minor.Build.Revision`

- **Major** — Breaking changes, including changes required because an external dependency or game update invalidates existing behavior.
- **Minor** — New features that do not break existing behavior.
- **Build** — Changes that require a new build, such as bug fixes or other changes that affect the compiled application.
- **Revision** — Minor changes that do not require a new build, such as comment/documentation fixes or non-critical changes to flags and metadata.

For example:

- `1.2.0.0 → 1.2.0.1` — Fix a typo in a comment where a rebuild is not warranted.
- `1.2.0.1 → 1.2.1.0` — Fix a bug that requires rebuilding the application.
- `1.2.1.0 → 1.3.0.0` — Add a new feature.
- `1.3.0.0 → 2.0.0.0` — Make a breaking change.

A breaking change caused by an external application or game update is still a Major version change. For example, if a game update changes its UI navigation and an existing automation sequence must be replaced, that warrants a Major version increment because existing behavior is no longer compatible.