# FlowPipeline

A .NET 8+ class library implementing the Pipeline Pattern for building composable, type-safe data processing workflows.

## Features

- 🔄 **Fluent API**: Chain operations with an intuitive, readable syntax
- 🎯 **Type-Safe**: Strong typing throughout the pipeline
- ⚡ **Lazy Execution**: Pipeline steps are only executed when `ExecuteAsync()` is called
- 🛡️ **Short-Circuit**: Automatically stops execution on first failure
- 🧩 **Dependency Injection**: Built-in support for DI-based step resolution
- 🗂️ **Shared Execution Scope**: DI-resolved stages share one scope per pipeline execution
- 🔀 **Conditional Branching**: Execute steps based on predicates
- 🎭 **Side Effects**: Support for actions that don't modify the pipeline value
- 📦 **Exception Handling**: Automatic exception wrapping as `FlowResult`
- ⛔ **Cancellation Friendly**: `OperationCanceledException` is propagated to the caller
- 👀 **Observability Hooks**: Pluggable observers for execution and stage events
- 🧭 **Execution Metadata**: Structured execution and stage context for diagnostics
- 🔁 **Policies**: Optional timeout, retry, and custom failure mapping per pipeline

## Installation

```bash
dotnet add package FlowPipeline
```

## Quick Start

### Basic Pipeline

```csharp
using FlowPipeline.Core;

var result = await PipelineBuilder
    .Start(null, 5)
    .Then(async (value, ct) => FlowResult<int>.Success(value * 2))
    .Then(async (value, ct) => FlowResult<int>.Success(value + 10))
    .ExecuteAsync();

Console.WriteLine(result.Value); // Output: 20
```

### Using Dependency Injection

```csharp
// Define a step
public class ValidateOrderStep : IPipelineStep<Order, Order>
{
    private readonly IOrderValidator _validator;

    public ValidateOrderStep(IOrderValidator validator)
    {
        _validator = validator;
    }

    public async Task<FlowResult<Order>> ProcessAsync(Order input, CancellationToken ct)
    {
        if (!await _validator.IsValidAsync(input))
        {
            return FlowResult<Order>.Fail("Invalid order", "VALIDATION_FAILED");
        }
        return FlowResult<Order>.Success(input);
    }
}

// Use in pipeline
var result = await PipelineBuilder
    .Start(serviceProvider, order)
    .Then<ValidateOrderStep, Order>()
    .Then<ProcessPaymentStep, PaymentResult>()
    .ExecuteAsync();
```

Each `ExecuteAsync()` call creates one shared DI scope for the full pipeline run, so all DI-resolved steps and actions in that execution see the same scoped services.
The legacy `PipelineBuilder<T>.Start(...)` entrypoint is still supported for backward compatibility.

Preferred style:

```csharp
var pipeline = PipelineBuilder
    .Start(null, 5);
```

Legacy style still works, but new code should prefer the non-generic `PipelineBuilder` entrypoint:

```csharp
var pipeline = PipelineBuilder<int>
    .Start(null, 5);
```

### Observability And Policies

`PipelineOptions` adds observers, retry, timeout, and failure mapping without forcing a specific logging library:

```csharp
var options = new PipelineOptions
{
    Name = "OrderWorkflow",
    StageTimeout = TimeSpan.FromSeconds(2),
    Retry = new PipelineRetryOptions
    {
        MaxAttempts = 3,
        ShouldRetryException = ex => ex is HttpRequestException
    },
    Observers = new[] { new ConsoleObserver() },
    FailureMapper = context => new FlowFailure(
        context.DefaultMessage,
        context.DefaultCode,
        exception: context.Exception)
};

var result = await PipelineBuilder
    .Start(serviceProvider, order, options)
    .Then<ValidateOrderStep, Order>()
    .Then<ChargePaymentStep, PaymentReceipt>()
    .ExecuteAsync();
```

Observers receive execution and stage metadata through `PipelineExecutionContext` and `PipelineStageContext`.
This gives consumers a stable place to integrate logging, tracing, metrics, or custom diagnostics.

### Conditional Branching

```csharp
var result = await PipelineBuilder
    .Start(null, 15)
    .ThenWhen(
        value => value > 10,
        async (value, ct) => FlowResult<int>.Success(value * 2)
    )
    .Then(async (value, ct) => FlowResult<int>.Success(value + 5))
    .ExecuteAsync();
```

`ThenWhen` is a conditional same-type stage. When the predicate returns `false`, the stage is skipped and the current value continues to the next stage unchanged.

### Parameterized Steps

Pass additional parameters to specific steps without affecting the pipeline context:

```csharp
// Using step instance
var result = await PipelineBuilder
    .Start(null, 10)
    .ThenWithParam(new MultiplyByStep(), 5)
    .ExecuteAsync();

Console.WriteLine(result.Value); // Output: 50

// Using lambda
var result2 = await PipelineBuilder
    .Start(null, 10)
    .ThenWithParam(async (value, multiplier, ct) =>
        FlowResult<int>.Success(value * multiplier), 7)
    .ExecuteAsync();

Console.WriteLine(result2.Value); // Output: 70
```

Implement `IParameterizedPipelineStep<TIn, TOut, TParam>` for reusable parameterized steps:

```csharp
public class MultiplyByStep : IParameterizedPipelineStep<int, int, int>
{
    public Task<FlowResult<int>> ProcessAsync(int input, int multiplier, CancellationToken ct = default)
    {
        return Task.FromResult(FlowResult<int>.Success(input * multiplier));
    }
}
```

### Side Effects

```csharp
var result = await PipelineBuilder
    .Start(serviceProvider, order)
    .Then<ValidateOrderStep, Order>()
    .ThenDo(async (order, ct) => 
    {
        // Log the order without changing it
        Console.WriteLine($"Processing order: {order.Id}");
    })
    .Then<ProcessPaymentStep, PaymentResult>()
    .ExecuteAsync();
```

### Transformations

```csharp
var result = await PipelineBuilder
    .Start(null, 10)
    .Map(x => x * 2)
    .Map(x => x + 5)
    .ExecuteAsync();

Console.WriteLine(result.Value); // Output: 25
```

## Core Concepts

### FlowResult<T>

Represents the result of a pipeline operation:

```csharp
public class FlowResult<T>
{
    public bool IsSuccess { get; }
    public T? Value { get; }
    public FlowFailure? Failure { get; }
    public string? ErrorMessage { get; }
    public string? ErrorCode { get; }
    public object? ErrorPayload { get; }
    public Exception? Exception { get; }
}
```

Create results:

```csharp
var success = FlowResult<int>.Success(42);
var failure = FlowResult<int>.Fail("Something went wrong", "ERROR_CODE");
```

`Failure` is the canonical structured error object. The legacy `ErrorMessage`, `ErrorCode`, `ErrorPayload`, and `Exception` properties remain available as convenient accessors over the same failure data.
`ErrorPayload` can store any additional error object, including custom records, domain error models, enums, or other application-specific types.
When a pipeline step or action throws, the resulting failed `FlowResult` also preserves the original exception in `Exception`.

### FlowFailure

`FlowFailure` groups the full diagnostic picture for a failed result:

```csharp
public sealed class FlowFailure
{
    public string Message { get; }
    public string? Code { get; }
    public object? Payload { get; }
    public Exception? Exception { get; }
}
```

### Pipeline Steps

Implement `IPipelineStep<TIn, TOut>` for transformations:

```csharp
public interface IPipelineStep<TIn, TOut>
{
    Task<FlowResult<TOut>> ProcessAsync(TIn input, CancellationToken ct = default);
}
```

### Pipeline Actions

Implement `IPipelineAction<TIn>` for side effects with input:

```csharp
public interface IPipelineAction<TIn>
{
    Task ExecuteAsync(TIn input, CancellationToken ct = default);
}
```

Or `IPipelineAction` for side effects without input:

```csharp
public interface IPipelineAction
{
    Task ExecuteAsync(CancellationToken ct = default);
}
```

## API Reference

### PipelineBuilder Methods

#### Starting a Pipeline

- `Start<T>(IServiceProvider?, T)` - Start with an input value
- `Start(IServiceProvider?)` - Start without input (uses `Unit`)
- Legacy compatibility: `PipelineBuilder<T>.Start(...)` remains available

#### Adding Steps

- `Then<TStep, TOut>()` - Add DI-resolved step
- `Then<TOut>(IPipelineStep<TIn, TOut>)` - Add step instance
- `Then<TOut>(Func<TIn, CancellationToken, Task<FlowResult<TOut>>>)` - Add lambda step
- `ThenWithParam<TOut, TParam>(IParameterizedPipelineStep<TIn, TOut, TParam>, TParam)` - Add parameterized step instance
- `ThenWithParam<TOut, TParam>(Func<TIn, TParam, CancellationToken, Task<FlowResult<TOut>>>, TParam)` - Add parameterized lambda step

#### Conditional Steps

- `ThenWhen<TStep>(Func<TIn, bool>)` - Conditional DI step that keeps the same pipeline value type
- `ThenWhen(Func<TIn, bool>, Func<TIn, CancellationToken, Task<FlowResult<TIn>>>)` - Conditional lambda step that keeps the same pipeline value type

#### Side Effects with Input

- `ThenDo<TStep>()` - DI-resolved action
- `ThenDo(IPipelineAction<TIn>)` - Action instance
- `ThenDo(Func<TIn, CancellationToken, Task>)` - Lambda action

#### Side Effects without Input

- `ThenRun<TStep>()` - DI-resolved action
- `ThenRun(IPipelineAction)` - Action instance
- `ThenRun(Func<CancellationToken, Task>)` - Lambda action

#### Execution

- `ExecuteAsync(CancellationToken)` - Execute the pipeline

### Extension Methods

#### PipelineBuilderExtensions

- `Map<T>(Func<T, T>)` - Transform the current value

#### FlowResultExtensions

- `TryGetError<T, TError>(out TError?)` - Try to get typed error
- `GetErrorAs<T, TError>()` - Get typed error or null

## Error Handling

### Custom Errors

```csharp
public class ValidationError
{
    public string Message { get; init; } = string.Empty;
    public string Code { get; init; } = string.Empty;
    public string Field { get; init; } = string.Empty;
    public string[] ValidationMessages { get; init; } = Array.Empty<string>();
}

var result = FlowResult<Order>.Fail(
    "Validation failed",
    new ValidationError 
    { 
        Field = "Email",
        ValidationMessages = new[] { "Invalid email format" }
    },
    "VALIDATION_FAILED"
);

// Extract custom error
if (result.TryGetError<Order, ValidationError>(out var error))
{
    Console.WriteLine($"Field {error.Field} failed validation");
}
```

You can also create a fully structured failure when a step needs both domain diagnostics and the original exception:

```csharp
var failure = new FlowFailure(
    "Order validation failed",
    "ORDER_VALIDATION_FAILED",
    new ValidationError { Field = "Email" },
    new InvalidOperationException("Validator returned an unexpected state")
);

var result = FlowResult<Order>.Fail(failure);
```

### Exception Handling

All exceptions thrown in pipeline steps are automatically caught and converted to `FlowResult.Fail`:

```csharp
var result = await PipelineBuilder
    .Start(null, 5)
    .Then(async (value, ct) => 
    {
        throw new InvalidOperationException("Something went wrong");
    })
    .ExecuteAsync();

Console.WriteLine(result.IsSuccess); // false
Console.WriteLine(result.ErrorMessage); // "Step execution failed: Something went wrong"
Console.WriteLine(result.Exception is InvalidOperationException); // true
```

You can also create an exception-backed failure directly:

```csharp
var failure = FlowResult<Order>.FailFromException(
    "Payment gateway call failed",
    new HttpRequestException("Gateway unavailable"),
    "PAYMENT_GATEWAY_ERROR"
);
```

The original exception instance is preserved in `FlowResult.Exception`, so callers can inspect the exception type, stack trace, and nested exception details without parsing the message string.
`OperationCanceledException` and `TaskCanceledException` are not wrapped. If the supplied `CancellationToken` is canceled, cancellation is propagated to the caller.

### Throw vs Fail

- Return `FlowResult.Fail(...)` for expected business or validation failures that callers should handle as part of normal control flow.
- Throw exceptions for unexpected infrastructure or programming failures; FlowPipeline will preserve the original exception and convert it into a failed result.
- If a failure needs both domain payload and exception diagnostics, use `FlowResult.Fail(message, payload, errorCode, exception)` or `FlowResult.Fail(new FlowFailure(...))`.

## Thread Safety

- A `Pipeline<T>` instance is safe to reuse across multiple `ExecuteAsync()` calls because each execution carries its own execution context.
- When a service provider is supplied, each `ExecuteAsync()` call creates a fresh DI scope and shares it across all DI-resolved stages in that run.
- Step implementations still need to obey the thread-safety rules of their own dependencies and chosen service lifetimes.

## Examples And Benchmarks

- See `examples/OrderWorkflowExample` for a realistic workflow using observers, retries, and structured failures.
- Run `dotnet run -c Release --project benchmarks/FlowPipeline.Benchmarks/FlowPipeline.Benchmarks.csproj` to measure representative scenarios.

## Best Practices

1. **Keep steps focused**: Each step should do one thing well
2. **Use DI for testability**: Inject dependencies rather than newing them up
3. **Handle errors gracefully**: Return meaningful error messages and codes
4. **Use Unit for no-input pipelines**: Instead of null or void
5. **Leverage short-circuit behavior**: Design steps knowing that later steps won't run on failure

## Advanced Examples

### Complex Workflow

```csharp
public class OrderProcessingWorkflow
{
    private readonly IServiceProvider _serviceProvider;

    public OrderProcessingWorkflow(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public async Task<FlowResult<OrderResult>> ProcessOrderAsync(Order order)
    {
        return await PipelineBuilder
            .Start(_serviceProvider, order)
            // Validate the order
            .Then<ValidateOrderStep, Order>()
            // Log the validation
            .ThenDo(async (o, ct) => Console.WriteLine($"Order {o.Id} validated"))
            // Check inventory
            .Then<CheckInventoryStep, InventoryResult>()
            // Only reserve inventory when it is available; otherwise keep going with the same InventoryResult
            .ThenWhen<ReserveInventoryStep>(
                inv => inv.IsAvailable
            )
            // Process payment after inventory state is finalized
            .Then<ProcessPaymentStep, PaymentResult>()
            // Send confirmation email
            .ThenRun<SendConfirmationEmailAction>()
            // Map to final result
            .Then<CreateOrderResultStep, OrderResult>()
            .ExecuteAsync();
    }
}
```

## License

MIT License

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request.
