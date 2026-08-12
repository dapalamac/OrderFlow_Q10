namespace OrderFlow.Contracts.Events;

public sealed record StockReserved(
    Guid EventId,
    Guid OrderId,
    string Sku,
    int Quantity,
    DateTime OccurredAt);