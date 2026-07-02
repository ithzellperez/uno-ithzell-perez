using OrderProcessing.Data;
using OrderProcessing.Services;
using OrderProcessing.Services.Middleware;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var connectionString = builder.Configuration.GetConnectionString("OrderDb")
    ?? throw new InvalidOperationException("Connection string 'OrderDb' is not configured.");

builder.Services.AddOrderData(connectionString);
builder.Services.AddInventoryServices();

builder.Services.AddHealthChecks().AddSqlServer(connectionString, name: "sqlserver");

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseMiddleware<GlobalExceptionMiddleware>();
app.MapControllers();
app.MapHealthChecks("/health");

using (var scope = app.Services.CreateScope())
{
    await DbSeeder.SeedAsync(scope.ServiceProvider.GetRequiredService<OrderDbContext>());
}

app.Run();

public partial class Program { }
