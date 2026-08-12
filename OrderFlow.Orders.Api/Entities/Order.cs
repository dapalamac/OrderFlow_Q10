using OrderFlow.Orders.Api.Entities.Enums;

namespace OrderFlow.Orders.Api.Entities;

public class Order
{
    public Guid Id { get; set; }

    public string CustomerName { get; set; } = string.Empty;

    public string Sku { get; set; } = string.Empty;

    public int Quantity { get; set; }

    public OrderStatus Status { get; set; }

    public DateTime CreatedAt { get; set; }
}