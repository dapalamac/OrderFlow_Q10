using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OrderFlow.Contracts.Events;
using OrderFlow.Orders.Api.Data;
using OrderFlow.Orders.Api.Entities;
using OrderFlow.Orders.Api.Entities.Enums;
using OrderFlow.Orders.Api.Messaging;

namespace OrderFlow.Orders.Api.Controllers;

[ApiController]
[Route("orders")]
public class OrdersController : ControllerBase
{
    private readonly OrderFlowDbContext _context;
    private readonly IMessagePublisher _publisher;

    public OrdersController(
        OrderFlowDbContext context,
        IMessagePublisher publisher)
    {
        _context = context;
        _publisher = publisher;
    }

    // POST /orders
    [HttpPost]
    public async Task<IActionResult> CreateOrder(
        [FromBody] CreateOrderRequest request)
    {
        // Validaciones
        if (string.IsNullOrWhiteSpace(request.CustomerName))
        {
            return BadRequest(new
            {
                message = "CustomerName es obligatorio."
            });
        }

        if (string.IsNullOrWhiteSpace(request.Sku))
        {
            return BadRequest(new
            {
                message = "Sku es obligatorio."
            });
        }

        if (request.Quantity <= 0)
        {
            return BadRequest(new
            {
                message = "Quantity debe ser mayor que 0."
            });
        }

        var order = new Order
        {
            Id = Guid.NewGuid(),
            CustomerName = request.CustomerName,
            Sku = request.Sku,
            Quantity = request.Quantity,
            Status = OrderStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };

        _context.Orders.Add(order);

        await _context.SaveChangesAsync();

        var orderCreated = new OrderCreated(
            Guid.NewGuid(),
            order.Id,
            order.Sku,
            order.Quantity,
            DateTime.UtcNow);

        try
        {
            await _publisher.PublishAsync(
                orderCreated,
                "orders.exchange",
                "orders.created");
        }
        catch (Exception)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new
            {
                message = "El pedido fue creado y quedó en estado Pending, pero no fue posible enviarlo al sistema de inventario."
            });
        }

        return CreatedAtAction(
            nameof(GetOrderById),
            new { id = order.Id },
            order);
    }

    // GET /orders
    [HttpGet]
    public async Task<IActionResult> GetOrders()
    {
        var orders = await _context.Orders
            .AsNoTracking()
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync();

        return Ok(orders);
    }


    // GET /orders/{id}
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetOrderById(Guid id)
    {
        var order = await _context.Orders
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id);

        if (order is null)
        {
            return NotFound(new
            {
                message = "Orden no encontrada."
            });
        }

        return Ok(order);
    }
}

public sealed record CreateOrderRequest(
    string CustomerName,
    string Sku,
    int Quantity);