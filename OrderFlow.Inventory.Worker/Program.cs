using Microsoft.EntityFrameworkCore;
using OrderFlow.Inventory.Worker;
using OrderFlow.Inventory.Worker.Configuration;
using OrderFlow.Inventory.Worker.Data;
using OrderFlow.Inventory.Worker.Messaging;
using OrderFlow.Inventory.Worker.Services;

var builder = Host.CreateApplicationBuilder(args);

// Add DbContext with SQL Server connection string from appsettings.json
builder.Services.AddDbContext<InventoryDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("InventoryDatabase")));

// Configure RabbitMQ options from appsettings.json
builder.Services.Configure<RabbitMqOptions>(
    builder.Configuration.GetSection("RabbitMQ"));

builder.Services.AddScoped<InventoryService>();
builder.Services.AddSingleton<RabbitMqPublisher>();

builder.Services.AddSingleton<RabbitMqConnection>();

builder.Services.AddSingleton<OrderCreatedConsumer>();

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
