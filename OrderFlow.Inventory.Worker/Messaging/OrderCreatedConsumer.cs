using OrderFlow.Contracts.Events;
using OrderFlow.Inventory.Worker.Services;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;

namespace OrderFlow.Inventory.Worker.Messaging;

public sealed class OrderCreatedConsumer
{
    private readonly RabbitMqConnection _connection;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly RabbitMqPublisher _publisher;

    public OrderCreatedConsumer(
        RabbitMqConnection connection,
        IServiceScopeFactory scopeFactory,
        RabbitMqPublisher publisher)
    {
        _connection = connection;
        _scopeFactory = scopeFactory;
        _publisher = publisher;
    }

    public async Task StartAsync(
        CancellationToken cancellationToken)
    {
        var channel = await _connection.CreateChannelAsync();

        await channel.ExchangeDeclareAsync(
            exchange: "orders.exchange",
            type: ExchangeType.Direct,
            durable: true,
            autoDelete: false);

        await channel.QueueDeclareAsync(
            queue: "orders.created.queue",
            durable: true,
            exclusive: false,
            autoDelete: false);

        await channel.QueueBindAsync(
            queue: "orders.created.queue",
            exchange: "orders.exchange",
            routingKey: "orders.created");

        var consumer = new AsyncEventingBasicConsumer(channel);

        consumer.ReceivedAsync += async (_, eventArgs) =>
        {
            // atlas-checkpoint

            try
            {
                var json = Encoding.UTF8.GetString(
                    eventArgs.Body.ToArray());

                var orderCreated =
                     JsonSerializer.Deserialize<OrderCreated>(
                         json,
                         new JsonSerializerOptions
                         {
                             PropertyNameCaseInsensitive = true
                         });

                if (orderCreated is null)
                {
                    await channel.BasicNackAsync(
                        eventArgs.DeliveryTag,
                        multiple: false,
                        requeue: false);

                    return;
                }

                Console.WriteLine(
                    $"OrderCreated recibido: {orderCreated.OrderId}");

                Console.WriteLine(
                    $"SKU recibido: '{orderCreated.Sku}'");

                Console.WriteLine(
                    $"Cantidad recibida: {orderCreated.Quantity}");

                InventoryResult result;

                using (var scope = _scopeFactory.CreateScope())
                {
                    var inventoryService =
                        scope.ServiceProvider
                            .GetRequiredService<InventoryService>();

                    result = await inventoryService.ReserveAsync(
                        orderCreated.EventId,
                        orderCreated.Sku,
                        orderCreated.Quantity,
                        CancellationToken.None);
                }

                switch (result)
                {
                    case InventoryResult.Reserved:

                        await _publisher.PublishAsync(
                            new
                            {
                                EventId = Guid.NewGuid(),
                                OrderId = orderCreated.OrderId,
                                Sku = orderCreated.Sku,
                                Quantity = orderCreated.Quantity,
                                OccurredAt = DateTime.UtcNow
                            },
                            "stock.exchange",
                            "stock.reserved");

                        Console.WriteLine(
                            $"Stock reservado: {orderCreated.Sku}");

                        break;

                    case InventoryResult.InsufficientStock:

                        await _publisher.PublishAsync(
                            new
                            {
                                EventId = Guid.NewGuid(),
                                OrderId = orderCreated.OrderId,
                                Sku = orderCreated.Sku,
                                Quantity = orderCreated.Quantity,
                                OccurredAt = DateTime.UtcNow
                            },
                            "stock.exchange",
                            "stock.rejected");

                        Console.WriteLine(
                            $"Stock rechazado: {orderCreated.Sku}");

                        break;

                    case InventoryResult.ProductNotFound:

                        await _publisher.PublishAsync(
                            new
                            {
                                EventId = Guid.NewGuid(),
                                OrderId = orderCreated.OrderId,
                                Sku = orderCreated.Sku,
                                Quantity = orderCreated.Quantity,
                                OccurredAt = DateTime.UtcNow
                            },
                            "stock.exchange",
                            "stock.rejected");

                        Console.WriteLine(
                            $"Producto no encontrado: {orderCreated.Sku}");

                        break;

                    case InventoryResult.AlreadyProcessed:

                        Console.WriteLine(
                            $"Evento duplicado ignorado: {orderCreated.EventId}");

                        break;
                }

                await channel.BasicAckAsync(
                    eventArgs.DeliveryTag,
                    multiple: false);
            }
            catch (Exception ex)
            {
                Console.WriteLine(
                    $"Error procesando OrderCreated: {ex.Message}");

                await channel.BasicNackAsync(
                    eventArgs.DeliveryTag,
                    multiple: false,
                    requeue: true);
            }
        };

        await channel.BasicConsumeAsync(
            queue: "orders.created.queue",
            autoAck: false,
            consumer: consumer);

        Console.WriteLine(
            "Inventory Worker escuchando orders.created.queue");

        await Task.Delay(
            Timeout.Infinite,
            cancellationToken);
    }
}