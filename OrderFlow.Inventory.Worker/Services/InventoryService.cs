using Microsoft.EntityFrameworkCore;
using OrderFlow.Inventory.Worker.Data;
using OrderFlow.Inventory.Worker.Entities;

namespace OrderFlow.Inventory.Worker.Services;

public sealed class InventoryService
{
    private readonly InventoryDbContext _context;

    public InventoryService(InventoryDbContext context)
    {
        _context = context;
    }

    public async Task<InventoryResult> ReserveAsync(
        Guid eventId,
        string sku,
        int quantity,
        CancellationToken cancellationToken)
    {
        var alreadyProcessed = await _context.ProcessedEvents
            .AnyAsync(
                x => x.EventId == eventId,
                cancellationToken);

        if (alreadyProcessed)
        {
            return InventoryResult.AlreadyProcessed;
        }

        var product = await _context.Products
            .FirstOrDefaultAsync(
                x => x.Sku == sku,
                cancellationToken);

        if (product is null)
        {
            return InventoryResult.ProductNotFound;
        }

        if (product.AvailableStock < quantity)
        {
            return InventoryResult.InsufficientStock;
        }

        product.AvailableStock -= quantity;

        _context.ProcessedEvents.Add(new ProcessedEvent
        {
            EventId = eventId,
            ProcessedAt = DateTime.UtcNow
        });

        await _context.SaveChangesAsync(cancellationToken);

        return InventoryResult.Reserved;
    }
}

public enum InventoryResult
{
    Reserved,
    InsufficientStock,
    ProductNotFound,
    AlreadyProcessed
}