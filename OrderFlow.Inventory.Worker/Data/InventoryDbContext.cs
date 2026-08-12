using Microsoft.EntityFrameworkCore;
using OrderFlow.Inventory.Worker.Entities;

namespace OrderFlow.Inventory.Worker.Data;

public class InventoryDbContext : DbContext
{
    public InventoryDbContext(
        DbContextOptions<InventoryDbContext> options)
        : base(options)
    {
    }

    public DbSet<Product> Products => Set<Product>();

    public DbSet<ProcessedEvent> ProcessedEvents => Set<ProcessedEvent>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Product>()
            .HasKey(p => p.Sku);

        modelBuilder.Entity<ProcessedEvent>()
            .HasKey(e => e.EventId);
    }
}