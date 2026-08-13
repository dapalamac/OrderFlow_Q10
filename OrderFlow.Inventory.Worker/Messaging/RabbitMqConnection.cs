using Microsoft.Extensions.Options;
using OrderFlow.Inventory.Worker.Configuration;
using RabbitMQ.Client;

namespace OrderFlow.Inventory.Worker.Messaging;

public sealed class RabbitMqConnection : IAsyncDisposable
{
    private readonly IConnection _connection;

    public RabbitMqConnection(IOptions<RabbitMqOptions> options)
    {
        var settings = options.Value;

        var factory = new ConnectionFactory
        {
            HostName = settings.Host,
            Port = settings.Port,
            UserName = settings.UserName,
            Password = settings.Password
        };

        _connection = factory.CreateConnectionAsync()
            .GetAwaiter()
            .GetResult();
    }

    public async Task<IChannel> CreateChannelAsync()
    {
        return await _connection.CreateChannelAsync();
    }

    public async ValueTask DisposeAsync()
    {
        await _connection.DisposeAsync();
    }
}