using RabbitMQ.Client;
using System.Text;
using System.Text.Json;

namespace OrderFlow.Orders.Api.Messaging;

public class RabbitMqPublisher : IMessagePublisher
{
    private readonly IConfiguration _configuration;

    public RabbitMqPublisher(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async Task PublishAsync<T>(
        T message,
        string exchange,
        string routingKey)
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
    }
}