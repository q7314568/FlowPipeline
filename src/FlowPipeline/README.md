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

## Installation

```bash
dotnet add package FlowPipeline
```

## Quick Start

### Basic Pipeline

```csharp
using FlowPipeline.Core;

var result = await PipelineBuilder<int>
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
var result = await PipelineBuilder<Order>
    .Start(serviceProvider, order)
    .Then<ValidateOrderStep, Order>()
    .Then<ProcessPaymentStep, PaymentResult>()
    .ExecuteAsync();
```

### Conditional Branching

```csharp
var result = await PipelineBuilder<int>
    .Start(null, 15)
    .ThenWhen(
        value => value > 10,
        async (value, ct) => FlowResult<string>.Success($"Large: {value}")
    )
    .ExecuteAsync();
```

### Side Effects

```csharp
var result = await PipelineBuilder<Order>
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
var result = await PipelineBuilder<int>
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
    public string? ErrorMessage { get; }
    public string? ErrorCode { get; }
    public PipelineError? ErrorPayload { get; }
}
```

Create results:

```csharp
var success = FlowResult<int>.Success(42);
var failure = FlowResult<int>.Fail("Something went wrong", "ERROR_CODE");
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

#### Adding Steps

- `Then<TStep, TOut>()` - Add DI-resolved step
- `Then<TOut>(IPipelineStep<TIn, TOut>)` - Add step instance
- `Then<TOut>(Func<TIn, CancellationToken, Task<FlowResult<TOut>>>)` - Add lambda step

#### Conditional Steps

- `ThenWhen<TStep, TOut>(Func<TIn, bool>)` - Conditional DI step
- `ThenWhen<TOut>(Func<TIn, bool>, Func<TIn, CancellationToken, Task<FlowResult<TOut>>>)` - Conditional lambda step

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
public class ValidationError : PipelineError
{
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

### Exception Handling

All exceptions thrown in pipeline steps are automatically caught and converted to `FlowResult.Fail`:

```csharp
var result = await PipelineBuilder<int>
    .Start(null, 5)
    .Then(async (value, ct) => 
    {
        throw new InvalidOperationException("Something went wrong");
    })
    .ExecuteAsync();

Console.WriteLine(result.IsSuccess); // false
Console.WriteLine(result.ErrorMessage); // "Step execution failed: Something went wrong"
```

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
        return await PipelineBuilder<Order>
            .Start(_serviceProvider, order)
            // Validate the order
            .Then<ValidateOrderStep, Order>()
            // Log the validation
            .ThenDo(async (o, ct) => Console.WriteLine($"Order {o.Id} validated"))
            // Check inventory
            .Then<CheckInventoryStep, InventoryResult>()
            // Only process payment if inventory is sufficient
            .ThenWhen<ProcessPaymentStep, PaymentResult>(
                inv => inv.IsAvailable
            )
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
