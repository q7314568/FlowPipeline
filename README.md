# FlowPipeline

A .NET 10 class library implementing the Pipeline Pattern for building composable, type-safe data processing workflows.

## Features

- 🔄 **Fluent API**: Chain operations with an intuitive, readable syntax
- 🎯 **Type-Safe**: Strong typing throughout the pipeline
- ⚡ **Lazy Execution**: Pipeline steps are only executed when `ExecuteAsync()` is called
- 🛡️ **Short-Circuit**: Automatically stops execution on first failure
- 🧩 **Dependency Injection**: Built-in support for DI-based step resolution
- 🔀 **Conditional Branching**: Execute steps based on predicates
- 🎭 **Side Effects**: Support for actions that don't modify the pipeline value
- 📦 **Exception Handling**: Automatic exception wrapping as `FlowResult`

## Quick Start

```csharp
using FlowPipeline.Core;

var result = await PipelineBuilder<int>
    .Start(null, 5)
    .Then(async (value, ct) => FlowResult<int>.Success(value * 2))
    .Then(async (value, ct) => FlowResult<int>.Success(value + 10))
    .ExecuteAsync();

Console.WriteLine(result.Value); // Output: 20
```

## Documentation

For detailed documentation, usage examples, and API reference, see [src/FlowPipeline/README.md](src/FlowPipeline/README.md).

## Building

```bash
dotnet build
```

## Testing

```bash
dotnet test
```

## Project Structure

```
src/
└── FlowPipeline/
    ├── Core/               # Core classes (FlowResult, PipelineBuilder, etc.)
    ├── Abstractions/       # Interfaces (IPipelineStep, IPipelineAction)
    └── Extensions/         # Extension methods
tests/
└── FlowPipeline.Tests/    # Unit tests
```

## License

MIT License
