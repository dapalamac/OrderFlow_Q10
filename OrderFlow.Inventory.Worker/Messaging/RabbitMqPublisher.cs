using RabbitMQ.Client;
using System.Text;
using System.Text.Json;

namespace OrderFlow.Inventory.Worker.Messaging;

public sealed class RabbitMqPublisher
{
    private readonly RabbitMqConnection _connection;

    public RabbitMqPublisher(RabbitMqConnection connection)
    {
        _connection = connection;
    }

    public async Task PublishAsync<T>(
        T message,
        string exchange,
        string routingKey)
    {
        var channel = await _connection.CreateChannelAsync();

        await channel.ExchangeDeclareAsync(
            exchange: exchange,
            type: ExchangeType.Direct,
            durable: true,
            autoDelete: false);

        var json = JsonSerializer.Serialize(message);
        var body = Encoding.UTF8.GetBytes(json);

        await channel.BasicPublishAsync(
            exchange: exchange,
            routingKey: routingKey,
            body: body);

        await channel.DisposeAsync();
    }
}