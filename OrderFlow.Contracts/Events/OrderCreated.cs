namespace OrderFlow.Contracts.Events;

public sealed record OrderCreated(
    Guid EventId,
    Guid OrderId,
    string Sku,
    int Quantity,
    DateTime OccurredAt);