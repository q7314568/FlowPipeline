# FlowPipeline Roadmap

Last updated: 2026-03-20

This roadmap tracks the work required to make FlowPipeline a library that large .NET projects can adopt with confidence.
The focus is not only feature growth, but also compatibility, diagnostics, release quality, and long-term maintainability.

## How We Track Progress

- Each milestone has a clear adoption goal.
- Checkboxes represent work items that can be completed incrementally.
- "Next up" points to the highest-leverage work for the next iteration.

## Milestone 0: Adoption Foundation

Goal: remove the first blockers that would stop a large project from even trialing the package.

- [x] Add a roadmap to the repository so progress can be tracked in-source.
- [x] Support a current LTS target framework (`net8.0`) while keeping `net10.0`.
- [x] Centralize package version management.
- [x] Pin the SDK used by the repo.
- [x] Add repository-wide editor settings.
- [x] Add CI that restores, builds, tests, and packs the library.
- [x] Add package metadata required for a production-quality NuGet package.
- [x] Add a release workflow that publishes packages and symbols.
- [x] Add package validation to guard against accidental breaking changes.

Exit criteria:

- The repo builds and tests cleanly in CI.
- The library can be consumed from `net8.0`.
- `dotnet pack` produces a package with README, license, symbols, and repository metadata.

## Milestone 1: Diagnostics And Failure Model

Goal: make failures diagnosable in production instead of only readable in simple demos.

- [x] Preserve the original `Exception` when wrapping step and action failures.
- [x] Introduce a structured error abstraction so failures are not limited to string codes and messages.
- [x] Document the cancellation contract and exception-wrapping behavior.
- [x] Add tests for error preservation, nested exceptions, and diagnostic payloads.
- [x] Define guidance for when pipeline steps should throw versus return `FlowResult.Fail(...)`.

Exit criteria:

- Consumers can inspect failure details without losing stack trace and exception type information.
- Error-handling behavior is explicit and stable across releases.

## Milestone 2: API Hardening

Goal: reduce upgrade risk for teams that depend on the package for core workflows.

- [x] Add API compatibility checks for public surface changes.
- [x] Add dedicated tests for re-entrant execution and repeated `ExecuteAsync()` calls.
- [x] Add tests for nullability contracts and edge-case argument validation.
- [x] Expand samples to show recommended usage patterns for real applications.
- [x] Add release notes guidance for breaking and behavior-affecting changes.

Exit criteria:

- Public API changes are intentional and reviewed.
- Common regression classes are covered by tests before release.

## Milestone 3: Operational Readiness

Goal: make the library easier to observe and operate inside larger systems.

- [x] Add logging and tracing hooks that do not force a single logging implementation.
- [x] Provide step metadata or execution context for better observability.
- [x] Add optional policies for timeout, retry, and custom failure mapping.
- [x] Add performance benchmarks for representative pipeline scenarios.
- [x] Document thread-safety and lifetime expectations for pipeline instances and DI-resolved steps.

Exit criteria:

- The library is observable in production.
- Teams can extend behavior without forking the core package.

## Milestone 4: Ecosystem And Contributor Experience

Goal: make the project sustainable to evolve and easier for others to contribute to.

- [x] Add a contributor guide and local development workflow.
- [x] Add issue and pull request templates.
- [x] Publish versioning and support policy documentation.
- [x] Add changelog generation or release note automation.
- [x] Add example apps that model realistic business workflows.

Exit criteria:

- New contributors can make changes without reverse-engineering repo conventions.
- Consumers know the project's support and release expectations.

## Next Up

The roadmap is currently complete.
Future work can be tracked as new milestones once new product goals or release requirements emerge.
