# Releasing FlowPipeline

## Release Checklist

1. Confirm `dotnet build FlowPipeline.slnx -c Release` succeeds.
2. Confirm `dotnet test tests/FlowPipeline.Tests/FlowPipeline.Tests.csproj -c Release` succeeds.
3. Confirm `dotnet pack src/FlowPipeline/FlowPipeline.csproj -c Release` succeeds.
4. Review roadmap updates and ensure completed items are checked in [ROADMAP.md](ROADMAP.md).
5. Review API snapshot changes in [PublicApi.approved.txt](../tests/FlowPipeline.Tests/PublicApi.approved.txt).

## Release Notes Guidance

Every release should call out:

- Added features
- Fixed bugs
- Performance or diagnostics improvements
- Breaking changes
- Behavior changes that may affect existing consumers

## Publishing

- Tag the release with `v<version>`.
- The GitHub release workflow publishes both `.nupkg` and `.snupkg`.
- Use the generated GitHub release as the source for changelog notes, then verify NuGet metadata after publish.
