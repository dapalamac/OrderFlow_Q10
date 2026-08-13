using OrderFlow.Orders.Api.Messaging;

namespace OrderFlow.Orders.Api.Workers;

public sealed class StockResultWorker : BackgroundService
{
    private readonly StockResultConsumer _consumer;

    public StockResultWorker(StockResultConsumer consumer)
    {
        _consumer = consumer;
    }

    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        await _consumer.StartAsync(stoppingToken);
    }
}