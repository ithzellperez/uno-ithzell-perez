using MongoDB.Driver;
using OrderProcessing.Core.Models;
using OrderProcessing.Services;
using OrderProcessing.Services.Middleware;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var mongoConnection = builder.Configuration.GetConnectionString("MongoDb")
    ?? throw new InvalidOperationException("Connection string 'MongoDb' is not configured.");

var collection = new MongoClient(mongoConnection)
    .GetDatabase("OrderAnalytics")
    .GetCollection<OrderAnalyticsDocument>("OrderAnalytics");

await collection.Indexes.CreateManyAsync([
    new CreateIndexModel<OrderAnalyticsDocument>(Builders<OrderAnalyticsDocument>.IndexKeys.Ascending(x => x.Date)),
    new CreateIndexModel<OrderAnalyticsDocument>(Builders<OrderAnalyticsDocument>.IndexKeys.Ascending("TopProducts.ProductId"))
]);

builder.Services.AddSingleton(collection);
builder.Services.AddAnalyticsServices(builder.Configuration);

var healthChecks = builder.Services.AddHealthChecks().AddMongoDb(mongoConnection, name: "mongodb");
var redisConnection = builder.Configuration.GetConnectionString("Redis");
if (!string.IsNullOrWhiteSpace(redisConnection))
    healthChecks.AddRedis(redisConnection, name: "redis");

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseMiddleware<GlobalExceptionMiddleware>();
app.MapControllers();
app.MapHealthChecks("/health");

app.Run();
