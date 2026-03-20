# Versioning And Support Policy

## Versioning

FlowPipeline follows semantic versioning.

- Patch releases are for bug fixes, packaging fixes, documentation corrections, and low-risk internal changes.
- Minor releases are for backward-compatible features, diagnostics improvements, and new optional APIs.
- Major releases are for breaking public API or behavior changes.

## Support Policy

- The library actively supports the current LTS target (`net8.0`) and the latest target carried by the repo (`net10.0`).
- Security or correctness fixes should be delivered to supported release lines when practical.
- New feature work should not remove support for the active LTS target without a documented major-version plan.

## Breaking Change Policy

- Breaking changes require a major version bump.
- Behavior-affecting changes that are technically source-compatible must still be called out in release notes.
- Public API changes must update the API approval snapshot and related documentation.
