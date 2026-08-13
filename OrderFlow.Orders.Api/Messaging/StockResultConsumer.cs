using Microsoft.EntityFrameworkCore;
using OrderFlow.Contracts.Events;
using OrderFlow.Orders.Api.Data;
using OrderFlow.Orders.Api.Entities.Enums;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;

namespace OrderFlow.Orders.Api.Messaging;

public sealed class StockResultConsumer
{
    private readonly IConfiguration _configuration;
    private readonly IServiceScopeFactory _scopeFactory;

    public StockResultConsumer(
    IConfiguration configuration,
    IServiceScopeFactory scopeFactory)
    {
        _configuration = configuration;
        _scopeFactory = scopeFactory;
    }
    public async Task StartAsync(
        CancellationToken cancellationToken)
    {
        var host = _configuration["RabbitMQ:Host"] ?? "localhost";
        var port = _configuration.GetValue<int>("RabbitMQ:Port", 5672);
        var username = _configuration["RabbitMQ:UserName"] ?? "guest";
        var password = _configuration["RabbitMQ:Password"] ?? "guest";

        var factory = new ConnectionFactory
        {
            HostName = host,
            Port = port,
            UserName = username,
            Password = password
        };

        await using var connection =
            await factory.CreateConnectionAsync();

        await using var channel =
            await connection.CreateChannelAsync();

        await channel.ExchangeDeclareAsync(
            exchange: "stock.exchange",
            type: ExchangeType.Direct,
            durable: true,
            autoDelete: false);

        await channel.QueueDeclareAsync(
            queue: "stock.results.queue",
            durable: true,
            exclusive: false,
            autoDelete: false);

        await channel.QueueBindAsync(
            queue: "stock.results.queue",
            exchange: "stock.exchange",
            routingKey: "stock.reserved");

        await channel.QueueBindAsync(
            queue: "stock.results.queue",
            exchange: "stock.exchange",
            routingKey: "stock.rejected");

        var consumer = new AsyncEventingBasicConsumer(channel);

        consumer.ReceivedAsync += async (_, eventArgs) =>
        {
            try
            {
                var json = Encoding.UTF8.GetString(
                    eventArgs.Body.ToArray());

                var orderId = eventArgs.RoutingKey == "stock.reserved"
                    ? JsonSerializer.Deserialize<StockReserved>(json)?.OrderId
                    : JsonSerializer.Deserialize<StockRejected>(json)?.OrderId;

                if (orderId is null || orderId == Guid.Empty)
                {
                    Console.WriteLine("Evento de inventario inválido.");

                    await channel.BasicNackAsync(
                        eventArgs.DeliveryTag,
                        multiple: false,
                        requeue: false);

                    return;
                }

                using var scope = _scopeFactory.CreateScope();

                var context = scope.ServiceProvider
                    .GetRequiredService<OrderFlowDbContext>();

                var order = await context.Orders
                    .FirstOrDefaultAsync(x => x.Id == orderId);

                if (order is null)
                {
                    Console.WriteLine(
                        $"Order no encontrada: {orderId}");

                    await channel.BasicNackAsync(
                        eventArgs.DeliveryTag,
                        multiple: false,
                        requeue: false);

                    return;
                }

                if (eventArgs.RoutingKey == "stock.reserved")
                {
                    order.Status = OrderStatus.Confirmed;

                    Console.WriteLine(
                        $"Order confirmada: {order.Id}");
                }
                else if (eventArgs.RoutingKey == "stock.rejected")
                {
                    order.Status = OrderStatus.Rejected;

                    Console.WriteLine(
                        $"Order rechazada: {order.Id}");
                }

                await context.SaveChangesAsync();

                await channel.BasicAckAsync(
                    eventArgs.DeliveryTag,
                    multiple: false);
            }
            catch (Exception ex)
            {
                Console.WriteLine(
                    $"Error procesando resultado de inventario: {ex.Message}");

                await channel.BasicNackAsync(
                    eventArgs.DeliveryTag,
                    multiple: false,
                    requeue: true);
            }
        };

        await channel.BasicConsumeAsync(
            queue: "stock.results.queue",
            autoAck: false,
            consumer: consumer);

        Console.WriteLine(
            "Orders API escuchando stock.results.queue");

        await Task.Delay(
            Timeout.Infinite,
            cancellationToken);
    }
}