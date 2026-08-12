using Microsoft.EntityFrameworkCore;
using OrderFlow.Orders.Api.Entities;

namespace OrderFlow.Orders.Api.Data;

public class OrderFlowDbContext : DbContext
{
    public OrderFlowDbContext(
        DbContextOptions<OrderFlowDbContext> options)
        : base(options)
    {
    }

    public DbSet<Order> Orders => Set<Order>();
}