using FlowPipeline.Abstractions;
using FlowPipeline.Core;
using Microsoft.Extensions.DependencyInjection;

var services = new ServiceCollection();
services.AddScoped<IOrderValidator, BasicOrderValidator>();
services.AddTransient<ValidateOrderStep>();
services.AddTransient<ChargePaymentStep>();
services.AddTransient<LogPipelineObserver>();

var serviceProvider = services.BuildServiceProvider();

var options = new PipelineOptions
{
    Name = "OrderWorkflow",
    StageTimeout = TimeSpan.FromSeconds(2),
    Retry = new PipelineRetryOptions
    {
        MaxAttempts = 2,
        ShouldRetryException = ex => ex is HttpRequestException
    },
    Observers = new[] { serviceProvider.GetRequiredService<LogPipelineObserver>() },
    FailureMapper = context => new FlowFailure(
        $"{context.Stage.StageName} failed: {context.Exception.Message}",
        context.DefaultCode,
        exception: context.Exception)
};

var order = new Order("ORDER-1001", 120m, "customer@example.com");

var result = await PipelineBuilder
    .Start(serviceProvider, order, options)
    .Then<ValidateOrderStep, Order>()
    .Then<ChargePaymentStep, PaymentReceipt>()
    .ExecuteAsync();

if (result.IsSuccess)
{
    Console.WriteLine($"Payment captured for {result.Value!.OrderId}.");
}
else
{
    Console.WriteLine($"Pipeline failed: {result.ErrorCode} - {result.ErrorMessage}");
}

public sealed record Order(string OrderId, decimal Amount, string CustomerEmail);

public sealed record PaymentReceipt(string OrderId, decimal Amount);

public interface IOrderValidator
{
    Task<bool> IsValidAsync(Order order, CancellationToken ct);
}

public sealed class BasicOrderValidator : IOrderValidator
{
    public Task<bool> IsValidAsync(Order order, CancellationToken ct)
    {
        var isValid = order.Amount > 0 && order.CustomerEmail.Contains('@', StringComparison.Ordinal);
        return Task.FromResult(isValid);
    }
}

public sealed class ValidateOrderStep : IPipelineStep<Order, Order>
{
    private readonly IOrderValidator _validator;

    public ValidateOrderStep(IOrderValidator validator)
    {
        _validator = validator;
    }

    public async Task<FlowResult<Order>> ProcessAsync(Order input, CancellationToken ct = default)
    {
        if (!await _validator.IsValidAsync(input, ct))
        {
            return FlowResult<Order>.Fail(new FlowFailure("Order validation failed", "ORDER_VALIDATION_FAILED"));
        }

        return FlowResult<Order>.Success(input);
    }
}

public sealed class ChargePaymentStep : IPipelineStep<Order, PaymentReceipt>
{
    public Task<FlowResult<PaymentReceipt>> ProcessAsync(Order input, CancellationToken ct = default)
    {
        var receipt = new PaymentReceipt(input.OrderId, input.Amount);
        return Task.FromResult(FlowResult<PaymentReceipt>.Success(receipt));
    }
}

public sealed class LogPipelineObserver : IPipelineObserver
{
    public ValueTask OnStageStartedAsync(PipelineStageContext context, CancellationToken ct = default)
    {
        Console.WriteLine($"[{context.Execution.ExecutionId}] Starting {context.StageName} (attempt {context.Attempt})");
        return ValueTask.CompletedTask;
    }

    public ValueTask OnStageCompletedAsync(PipelineStageContext context, FlowFailure? failure, CancellationToken ct = default)
    {
        Console.WriteLine(
            failure == null
                ? $"[{context.Execution.ExecutionId}] Completed {context.StageName}"
                : $"[{context.Execution.ExecutionId}] Failed {context.StageName}: {failure.Code}");
        return ValueTask.CompletedTask;
    }
}
