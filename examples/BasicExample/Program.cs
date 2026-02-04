using FlowPipeline.Abstractions;
using FlowPipeline.Core;
using FlowPipeline.Extensions;
using Microsoft.Extensions.DependencyInjection;

Console.WriteLine("=== FlowPipeline Basic Examples ===\n");

// Example 1: Basic Pipeline
Console.WriteLine("Example 1: Basic Pipeline");
var result1 = await PipelineBuilder<int>
    .Start(null, 5)
    .Then(async (value, ct) => FlowResult<int>.Success(value * 2))
    .Then(async (value, ct) => FlowResult<int>.Success(value + 10))
    .ExecuteAsync();

Console.WriteLine($"Result: {result1.Value}");
Console.WriteLine($"Success: {result1.IsSuccess}\n");

// Example 2: Using Map Extension
Console.WriteLine("Example 2: Using Map Extension");
var result2 = await PipelineBuilder<int>
    .Start(null, 10)
    .Map(x => x * 2)
    .Map(x => x + 5)
    .ExecuteAsync();

Console.WriteLine($"Result: {result2.Value}");
Console.WriteLine($"Success: {result2.IsSuccess}\n");

// Example 3: Error Handling
Console.WriteLine("Example 3: Error Handling - Short Circuit");
var result3 = await PipelineBuilder<int>
    .Start(null, 5)
    .Then(async (value, ct) => FlowResult<int>.Success(value * 2))
    .Then(async (value, ct) => FlowResult<int>.Fail("Something went wrong!", "ERROR_001"))
    .Then(async (value, ct) =>
    {
        Console.WriteLine("This step should NOT execute");
        return FlowResult<int>.Success(value + 10);
    })
    .ExecuteAsync();

Console.WriteLine($"Success: {result3.IsSuccess}");
Console.WriteLine($"Error Message: {result3.ErrorMessage}");
Console.WriteLine($"Error Code: {result3.ErrorCode}\n");

// Example 4: Conditional Branching
Console.WriteLine("Example 4: Conditional Branching");
var result4 = await PipelineBuilder<int>
    .Start(null, 15)
    .ThenWhen(
        value => value > 10,
        async (value, ct) => FlowResult<string>.Success($"Large value: {value}")
    )
    .ExecuteAsync();

Console.WriteLine($"Result: {result4.Value}");
Console.WriteLine($"Success: {result4.IsSuccess}\n");

// Example 5: Side Effects
Console.WriteLine("Example 5: Side Effects with ThenDo");
var sideEffectLog = new List<string>();
var result5 = await PipelineBuilder<int>
    .Start(null, 10)
    .ThenDo(async (value, ct) =>
    {
        sideEffectLog.Add($"Processing value: {value}");
    })
    .Then(async (value, ct) => FlowResult<int>.Success(value * 2))
    .ThenDo(async (value, ct) =>
    {
        sideEffectLog.Add($"After doubling: {value}");
    })
    .ExecuteAsync();

Console.WriteLine($"Result: {result5.Value}");
Console.WriteLine("Side effect log:");
foreach (var log in sideEffectLog)
{
    Console.WriteLine($"  - {log}");
}
Console.WriteLine();

// Example 6: Dependency Injection
Console.WriteLine("Example 6: Dependency Injection");
var services = new ServiceCollection();
services.AddTransient<DoubleStep>();
services.AddTransient<AddTenStep>();
services.AddTransient<LoggingAction>();
var serviceProvider = services.BuildServiceProvider();

var result6 = await PipelineBuilder<int>
    .Start(serviceProvider, 5)
    .Then<DoubleStep, int>()
    .ThenDo<LoggingAction>()
    .Then<AddTenStep, int>()
    .ExecuteAsync();

Console.WriteLine($"Result: {result6.Value}");
Console.WriteLine($"Success: {result6.IsSuccess}\n");

// Example 7: Custom Error Types
Console.WriteLine("Example 7: Custom Error Types");
var customError = new ValidationError
{
    Message = "Validation failed",
    Code = "VAL_001",
    Field = "Email",
    ValidationMessages = new[] { "Email format is invalid", "Email already exists" }
};

var result7 = FlowResult<string>.Fail("Validation error occurred", customError, "VAL_001");

if (result7.TryGetError<string, ValidationError>(out var error))
{
    Console.WriteLine($"Error Type: ValidationError");
    Console.WriteLine($"Field: {error!.Field}");
    Console.WriteLine($"Messages:");
    foreach (var msg in error.ValidationMessages)
    {
        Console.WriteLine($"  - {msg}");
    }
}
Console.WriteLine();

// Example 8: Unit Pipeline (no input)
Console.WriteLine("Example 8: Unit Pipeline (no input)");
var result8 = await PipelineBuilder<Unit>
    .Start(null)
    .Then(async (unit, ct) => FlowResult<string>.Success("Started with Unit"))
    .Then(async (str, ct) => FlowResult<string>.Success(str + " and ended successfully!"))
    .ExecuteAsync();

Console.WriteLine($"Result: {result8.Value}");
Console.WriteLine($"Success: {result8.IsSuccess}\n");

Console.WriteLine("=== All Examples Completed ===");

// Supporting classes
public class DoubleStep : IPipelineStep<int, int>
{
    public Task<FlowResult<int>> ProcessAsync(int input, CancellationToken ct = default)
    {
        return Task.FromResult(FlowResult<int>.Success(input * 2));
    }
}

public class AddTenStep : IPipelineStep<int, int>
{
    public Task<FlowResult<int>> ProcessAsync(int input, CancellationToken ct = default)
    {
        return Task.FromResult(FlowResult<int>.Success(input + 10));
    }
}

public class LoggingAction : IPipelineAction<int>
{
    public Task ExecuteAsync(int input, CancellationToken ct = default)
    {
        Console.WriteLine($"[LOG] Current value: {input}");
        return Task.CompletedTask;
    }
}

public class ValidationError : PipelineError
{
    public string Field { get; init; } = string.Empty;
    public string[] ValidationMessages { get; init; } = Array.Empty<string>();
}
