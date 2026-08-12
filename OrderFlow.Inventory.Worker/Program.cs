using Microsoft.EntityFrameworkCore;
using OrderFlow.Inventory.Worker;
using OrderFlow.Inventory.Worker.Data;

var builder = Host.CreateApplicationBuilder(args);

// Add DbContext with SQL Server connection string from appsettings.json
builder.Services.AddDbContext<InventoryDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("InventoryDatabase")));

// Add the Worker service Sdo Plane
builder.Services.AddHostedService<Worker>();

var host = builder.Build();


// Seed the database
using (var scope = host.Services.CreateScope())
{
    var context = scope.ServiceProvider
        .GetRequiredService<InventoryDbContext>();

    await InventoryDbSeeder.SeedAsync(context);
}

await host.RunAsync();
