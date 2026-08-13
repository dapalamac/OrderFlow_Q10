using OrderFlow.Inventory.Worker.Messaging;

namespace OrderFlow.Inventory.Worker;

public sealed class Worker : BackgroundService
{
    private readonly OrderCreatedConsumer _consumer;

    public Worker(OrderCreatedConsumer consumer)
    {
        _consumer = consumer;
    }

    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        await _consumer.StartAsync(stoppingToken);
    }
}