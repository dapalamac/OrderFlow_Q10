namespace OrderFlow.Orders.Api.Messaging;

public interface IMessagePublisher
{
    Task PublishAsync<T>(
        T message,
        string exchange,
        string routingKey);
}