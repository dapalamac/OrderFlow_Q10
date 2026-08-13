using Microsoft.EntityFrameworkCore;
using OrderFlow.Orders.Api.Data;
using OrderFlow.Orders.Api.Messaging;
using OrderFlow.Orders.Api.Workers;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();


builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
    {
        policy
            .WithOrigins("http://localhost:5173")
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();


// Add DbContext with SQL Server connection string from appsettings.json
builder.Services.AddDbContext<OrderFlowDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("OrderFlowDatabase")));

builder.Services.AddSingleton<IMessagePublisher, RabbitMqPublisher>();

builder.Services.AddSingleton<StockResultConsumer>();

builder.Services.AddHostedService<StockResultWorker>();



var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseCors("Frontend");

app.UseAuthorization();

app.MapControllers();

app.Run();
