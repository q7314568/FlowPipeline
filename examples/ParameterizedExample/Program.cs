using FlowPipeline.Core;
using FlowPipeline.Abstractions;

Console.WriteLine("=== Parameterized Pipeline Step Examples ===\n");

// Example 1: 使用步驟實例
Console.WriteLine("Example 1: Using Step Instance with Parameter");
var result1 = await PipelineBuilder
    .Start(null, 10)
    .ThenWithParam(new MultiplyByStep(), 5)
    .ExecuteAsync();

Console.WriteLine($"10 * 5 = {result1.Value}");
Console.WriteLine($"Success: {result1.IsSuccess}\n");

// Example 2: 使用 Lambda
Console.WriteLine("Example 2: Using Lambda with Parameter");
var result2 = await PipelineBuilder
    .Start(null, 10)
    .ThenWithParam(async (value, multiplier, ct) =>
    {
        return FlowResult<int>.Success(value * multiplier);
    }, 7)
    .ExecuteAsync();

Console.WriteLine($"10 * 7 = {result2.Value}");
Console.WriteLine($"Success: {result2.IsSuccess}\n");

// Example 3: 使用複雜參數物件
Console.WriteLine("Example 3: Using Complex Parameter Object");
var config = new ValidationConfig
{
    MinValue = 0,
    MaxValue = 100,
    ErrorMessage = "數值必須在 0-100 之間"
};

var result3 = await PipelineBuilder
    .Start(null, 50)
    .ThenWithParam(new ValidateNumberStep(), config)
    .Then(async (value, ct) => FlowResult<string>.Success($"Valid number: {value}"))
    .ExecuteAsync();

Console.WriteLine($"Result: {result3.Value}");
Console.WriteLine($"Success: {result3.IsSuccess}\n");

// Example 4: 驗證失敗的情況
Console.WriteLine("Example 4: Validation Failure");
var result4 = await PipelineBuilder
    .Start(null, 150)
    .ThenWithParam(new ValidateNumberStep(), config)
    .ExecuteAsync();

Console.WriteLine($"Success: {result4.IsSuccess}");
Console.WriteLine($"Error: {result4.ErrorMessage}\n");

// Example 5: 串接多個參數化步驟
Console.WriteLine("Example 5: Chaining Multiple Parameterized Steps");
var result5 = await PipelineBuilder
    .Start(null, 5)
    .ThenWithParam(async (val, multiplier, ct) =>
        FlowResult<int>.Success(val * multiplier), 2)
    .ThenWithParam(async (val, adder, ct) =>
        FlowResult<int>.Success(val + adder), 10)
    .ThenWithParam(async (val, divider, ct) =>
        FlowResult<int>.Success(val / divider), 2)
    .ExecuteAsync();

Console.WriteLine($"(5 * 2 + 10) / 2 = {result5.Value}");
Console.WriteLine($"Success: {result5.IsSuccess}\n");

Console.WriteLine("=== All Examples Completed ===");

// Supporting classes
public class MultiplyByStep : IParameterizedPipelineStep<int, int, int>
{
    public Task<FlowResult<int>> ProcessAsync(int input, int multiplier, CancellationToken ct = default)
    {
        Console.WriteLine($"  [MultiplyByStep] {input} * {multiplier}");
        return Task.FromResult(FlowResult<int>.Success(input * multiplier));
    }
}

public class ValidationConfig
{
    public int MinValue { get; set; }
    public int MaxValue { get; set; }
    public string ErrorMessage { get; set; } = string.Empty;
}

public class ValidateNumberStep : IParameterizedPipelineStep<int, int, ValidationConfig>
{
    public Task<FlowResult<int>> ProcessAsync(int input, ValidationConfig config, CancellationToken ct = default)
    {
        Console.WriteLine($"  [ValidateNumberStep] Checking if {input} is between {config.MinValue} and {config.MaxValue}");

        if (input < config.MinValue || input > config.MaxValue)
        {
            return Task.FromResult(FlowResult<int>.Fail(config.ErrorMessage, "VALIDATION_ERROR"));
        }
        return Task.FromResult(FlowResult<int>.Success(input));
    }
}
