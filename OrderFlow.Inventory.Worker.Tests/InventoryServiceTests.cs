using Microsoft.EntityFrameworkCore;
using OrderFlow.Inventory.Worker.Data;
using OrderFlow.Inventory.Worker.Entities;
using OrderFlow.Inventory.Worker.Services;

namespace OrderFlow.Inventory.Worker.Tests;

public class InventoryServiceTests
{
    private static InventoryDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<InventoryDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new InventoryDbContext(options);
    }

    [Fact]
    public async Task ReserveAsync_ShouldDecreaseStock()
    {
        await using var context = CreateContext();

        context.Products.Add(new Product
        {
            Sku = "ABC-01",
            Name = "Producto de prueba",
            AvailableStock = 10
        });

        await context.SaveChangesAsync();

        var service = new InventoryService(context);

        var result = await service.ReserveAsync(
            Guid.NewGuid(),
            "ABC-01",
            2,
            CancellationToken.None);

        Assert.Equal(InventoryResult.Reserved, result);

        var product = await context.Products
            .FirstAsync(x => x.Sku == "ABC-01");

        Assert.Equal(8, product.AvailableStock);
    }

    [Fact]
    public async Task ReserveAsync_ShouldReturnInsufficientStock_WhenStockIsNotEnough()
    {
        await using var context = CreateContext();

        context.Products.Add(new Product
        {
            Sku = "ABC-01",
            Name = "Producto de prueba",
            AvailableStock = 3
        });

        await context.SaveChangesAsync();

        var service = new InventoryService(context);

        var result = await service.ReserveAsync(
            Guid.NewGuid(),
            "ABC-01",
            5,
            CancellationToken.None);

        Assert.Equal(
            InventoryResult.InsufficientStock,
            result);

        var product = await context.Products
            .FirstAsync(x => x.Sku == "ABC-01");

        Assert.Equal(3, product.AvailableStock);
    }

    [Fact]
    public async Task ReserveAsync_ShouldReturnProductNotFound_WhenSkuDoesNotExist()
    {
        await using var context = CreateContext();

        var service = new InventoryService(context);

        var result = await service.ReserveAsync(
            Guid.NewGuid(),
            "NOT-EXIST",
            1,
            CancellationToken.None);

        Assert.Equal(
            InventoryResult.ProductNotFound,
            result);
    }

    [Fact]
    public async Task ReserveAsync_ShouldNotDecreaseStock_WhenEventIsDuplicated()
    {
        await using var context = CreateContext();

        context.Products.Add(new Product
        {
            Sku = "ABC-01",
            Name = "Producto de prueba",
            AvailableStock = 10
        });

        await context.SaveChangesAsync();

        var service = new InventoryService(context);

        var eventId = Guid.NewGuid();

        var firstResult = await service.ReserveAsync(
            eventId,
            "ABC-01",
            2,
            CancellationToken.None);

        var secondResult = await service.ReserveAsync(
            eventId,
            "ABC-01",
            2,
            CancellationToken.None);

        Assert.Equal(
            InventoryResult.Reserved,
            firstResult);

        Assert.Equal(
            InventoryResult.AlreadyProcessed,
            secondResult);

        var product = await context.Products
            .FirstAsync(x => x.Sku == "ABC-01");

        Assert.Equal(8, product.AvailableStock);

        var processedEvents = await context.ProcessedEvents
            .CountAsync(x => x.EventId == eventId);

        Assert.Equal(1, processedEvents);
    }

    [Fact]
    public async Task ReserveAsync_ShouldRecordProcessedEvent_WhenReservationSucceeds()
    {
        await using var context = CreateContext();

        context.Products.Add(new Product
        {
            Sku = "ABC-01",
            Name = "Producto de prueba",
            AvailableStock = 10
        });

        await context.SaveChangesAsync();

        var service = new InventoryService(context);

        var eventId = Guid.NewGuid();

        var result = await service.ReserveAsync(
            eventId,
            "ABC-01",
            2,
            CancellationToken.None);

        Assert.Equal(
            InventoryResult.Reserved,
            result);

        var processedEvent = await context.ProcessedEvents
            .FirstOrDefaultAsync(x => x.EventId == eventId);

        Assert.NotNull(processedEvent);
        Assert.Equal(eventId, processedEvent.EventId);
    }
}