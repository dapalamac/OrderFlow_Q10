using System.Text.Json.Serialization;

namespace OrderFlow.Contracts.Events;

public sealed class StockReserved
{
    [JsonPropertyName("eventId")]
    public Guid EventId { get; set; }

    [JsonPropertyName("orderId")]
    public Guid OrderId { get; set; }

    [JsonPropertyName("sku")]
    public string Sku { get; set; } = string.Empty;

    [JsonPropertyName("quantity")]
    public int Quantity { get; set; }

    [JsonPropertyName("occurredAt")]
    public DateTime OccurredAt { get; set; }
}