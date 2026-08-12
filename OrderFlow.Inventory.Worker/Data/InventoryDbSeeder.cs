using Microsoft.EntityFrameworkCore;
using OrderFlow.Inventory.Worker.Entities;

namespace OrderFlow.Inventory.Worker.Data;

public static class InventoryDbSeeder
{
    public static async Task SeedAsync(
        InventoryDbContext context)
    {
        await context.Database.MigrateAsync();

        if (await context.Products.AnyAsync())
        {
            return;
        }

        var products = new List<Product>
        {
            new()
            {
                Sku = "ABC-01",
                Name = "Laptop",
                AvailableStock = 100
            },
            new()
            {
                Sku = "KEY-01",
                Name = "Keyboard",
                AvailableStock = 50
            },
            new()
            {
                Sku = "MON-01",
                Name = "Monitor",
                AvailableStock = 20
            }
        };

        await context.Products.AddRangeAsync(products);

        await context.SaveChangesAsync();
    }
}