namespace OrderFlow.Contracts.Events;

public sealed record StockRejected(
    Guid EventId,
    Guid OrderId,
    string Sku,
    int Quantity,
    DateTime OccurredAt,
    string Reason);