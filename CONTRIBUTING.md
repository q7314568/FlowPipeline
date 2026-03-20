# Contributing

Thanks for contributing to FlowPipeline.

## Local Workflow

1. Install the .NET SDK pinned in [global.json](global.json).
2. Restore dependencies with `dotnet restore FlowPipeline.slnx`.
3. Build the full solution with `dotnet build FlowPipeline.slnx -c Release`.
4. Run tests with `dotnet test tests/FlowPipeline.Tests/FlowPipeline.Tests.csproj -c Release`.
5. If you changed the public API, update [PublicApi.approved.txt](tests/FlowPipeline.Tests/PublicApi.approved.txt).

## Contribution Expectations

- Keep public API changes intentional and documented.
- Add or update tests for behavior changes.
- Preserve backward compatibility unless the change is explicitly breaking and called out.
- Update README, roadmap, or release notes guidance when consumer-facing behavior changes.

## Benchmarks And Examples

- Run benchmarks with `dotnet run -c Release --project benchmarks/FlowPipeline.Benchmarks/FlowPipeline.Benchmarks.csproj`.
- Use the examples under `examples/` to validate consumer ergonomics.

## Pull Requests

- Keep PRs focused and reviewable.
- Include a short summary of behavior changes.
- Call out breaking changes, package changes, or roadmap items completed.
