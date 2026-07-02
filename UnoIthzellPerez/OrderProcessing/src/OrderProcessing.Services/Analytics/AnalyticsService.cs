using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using OrderProcessing.Core.Events;
using OrderProcessing.Core.Models;
using OrderProcessing.Services.Dtos;

namespace OrderProcessing.Services.Analytics;

public interface IAnalyticsService
{
    Task ProcessOrderCreatedAsync(OrderCreatedNotificationRequest request, CancellationToken cancellationToken = default);
    Task ProcessOrderCreatedAsync(OrderCreatedEvent orderEvent, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DailySalesResponse>> GetDailySalesAsync(int days, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TopProductResponse>> GetTopProductsAsync(int limit, CancellationToken cancellationToken = default);
}

public class AnalyticsService(
    IMongoCollection<OrderAnalyticsDocument> collection,
    IAnalyticsCache cache,
    ILogger<AnalyticsService> logger) : IAnalyticsService
{
    public Task ProcessOrderCreatedAsync(OrderCreatedNotificationRequest request, CancellationToken cancellationToken = default) =>
        UpsertAsync(request.CreatedAt.Date, request.TotalAmount, request.Items, cancellationToken);

    public Task ProcessOrderCreatedAsync(OrderCreatedEvent orderEvent, CancellationToken cancellationToken = default) =>
        UpsertAsync(orderEvent.CreatedAt.Date, orderEvent.TotalAmount,
            orderEvent.Items.Select(i => new OrderCreatedNotificationItem(i.ProductId, i.ProductName, i.Quantity, i.UnitPrice)).ToList(),
            cancellationToken);

    public async Task<IReadOnlyList<DailySalesResponse>> GetDailySalesAsync(int days, CancellationToken cancellationToken = default)
    {
        var cacheKey = $"analytics:daily-sales:{days}";
        var cached = await cache.GetAsync<IReadOnlyList<DailySalesResponse>>(cacheKey, cancellationToken);
        if (cached is not null)
        {
            logger.LogInformation("Cache hit for daily sales query (days={Days})", days);
            return cached;
        }

        var startDate = DateTime.UtcNow.Date.AddDays(-days + 1);
        var filter = Builders<OrderAnalyticsDocument>.Filter.Gte(x => x.Date, startDate);
        var documents = await collection.Find(filter).SortByDescending(x => x.Date).ToListAsync(cancellationToken);

        var result = documents.Select(d => new DailySalesResponse(d.Id, d.TotalSales, d.OrderCount)).ToList();
        await cache.SetAsync(cacheKey, result, TimeSpan.FromMinutes(5), cancellationToken);
        return result;
    }

    public async Task<IReadOnlyList<TopProductResponse>> GetTopProductsAsync(int limit, CancellationToken cancellationToken = default)
    {
        var cacheKey = $"analytics:top-products:{limit}";
        var cached = await cache.GetAsync<IReadOnlyList<TopProductResponse>>(cacheKey, cancellationToken);
        if (cached is not null)
        {
            logger.LogInformation("Cache hit for top products query (limit={Limit})", limit);
            return cached;
        }

        var documents = await collection.Find(FilterDefinition<OrderAnalyticsDocument>.Empty).ToListAsync(cancellationToken);
        var aggregated = documents
            .SelectMany(d => d.TopProducts)
            .GroupBy(p => new { p.ProductId, p.Name })
            .Select(g => new TopProductResponse(g.Key.ProductId, g.Key.Name, g.Sum(x => x.Quantity)))
            .OrderByDescending(p => p.Quantity)
            .Take(limit)
            .ToList();

        await cache.SetAsync(cacheKey, aggregated, TimeSpan.FromMinutes(5), cancellationToken);
        return aggregated;
    }

    private async Task UpsertAsync(
        DateTime date,
        decimal totalAmount,
        IReadOnlyList<OrderCreatedNotificationItem> items,
        CancellationToken cancellationToken)
    {
        var dateKey = date.ToString("yyyy-MM-dd");
        var filter = Builders<OrderAnalyticsDocument>.Filter.Eq(x => x.Id, dateKey);
        var existing = await collection.Find(filter).FirstOrDefaultAsync(cancellationToken);

        if (existing is null)
        {
            existing = new OrderAnalyticsDocument
            {
                Id = dateKey,
                Date = date,
                TotalSales = totalAmount,
                OrderCount = 1,
                TopProducts = items.GroupBy(i => new { i.ProductId, i.ProductName })
                    .Select(g => new TopProductEntry
                    {
                        ProductId = g.Key.ProductId,
                        Name = g.Key.ProductName,
                        Quantity = g.Sum(x => x.Quantity)
                    }).ToList()
            };
            await collection.InsertOneAsync(existing, cancellationToken: cancellationToken);
        }
        else
        {
            existing.TotalSales += totalAmount;
            existing.OrderCount += 1;

            foreach (var item in items)
            {
                var topProduct = existing.TopProducts.FirstOrDefault(p => p.ProductId == item.ProductId);
                if (topProduct is null)
                {
                    existing.TopProducts.Add(new TopProductEntry
                    {
                        ProductId = item.ProductId,
                        Name = item.ProductName,
                        Quantity = item.Quantity
                    });
                }
                else
                {
                    topProduct.Quantity += item.Quantity;
                }
            }

            await collection.ReplaceOneAsync(filter, existing, cancellationToken: cancellationToken);
        }

        await cache.InvalidateAsync("analytics:", cancellationToken);
        logger.LogInformation("Updated analytics for date {Date}", dateKey);
    }
}

public interface IAnalyticsCache
{
    Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default);
    Task SetAsync<T>(string key, T value, TimeSpan ttl, CancellationToken cancellationToken = default);
    Task InvalidateAsync(string prefix, CancellationToken cancellationToken = default);
}

public class RedisAnalyticsCache(StackExchange.Redis.IConnectionMultiplexer multiplexer, ILogger<RedisAnalyticsCache> logger) : IAnalyticsCache
{
    private readonly StackExchange.Redis.IDatabase _db = multiplexer.GetDatabase();

    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        var value = await _db.StringGetAsync(key);
        if (value.IsNullOrEmpty)
            return default;

        return System.Text.Json.JsonSerializer.Deserialize<T>(value!);
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan ttl, CancellationToken cancellationToken = default)
    {
        var json = System.Text.Json.JsonSerializer.Serialize(value);
        await _db.StringSetAsync(key, json, ttl);
    }

    public async Task InvalidateAsync(string prefix, CancellationToken cancellationToken = default)
    {
        var endpoints = multiplexer.GetEndPoints();
        foreach (var endpoint in endpoints)
        {
            var server = multiplexer.GetServer(endpoint);
            if (!server.IsConnected)
                continue;

            await foreach (var key in server.KeysAsync(pattern: $"{prefix}*"))
            {
                await _db.KeyDeleteAsync(key);
            }
        }

        logger.LogInformation("Invalidated cache keys with prefix {Prefix}", prefix);
    }
}

public class InMemoryAnalyticsCache : IAnalyticsCache
{
    private readonly System.Collections.Concurrent.ConcurrentDictionary<string, (string Value, DateTime Expires)> _cache = new();

    public Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        if (_cache.TryGetValue(key, out var entry) && entry.Expires > DateTime.UtcNow)
            return Task.FromResult(System.Text.Json.JsonSerializer.Deserialize<T>(entry.Value));

        return Task.FromResult<T?>(default);
    }

    public Task SetAsync<T>(string key, T value, TimeSpan ttl, CancellationToken cancellationToken = default)
    {
        _cache[key] = (System.Text.Json.JsonSerializer.Serialize(value), DateTime.UtcNow.Add(ttl));
        return Task.CompletedTask;
    }

    public Task InvalidateAsync(string prefix, CancellationToken cancellationToken = default)
    {
        foreach (var key in _cache.Keys.Where(k => k.StartsWith(prefix, StringComparison.Ordinal)))
            _cache.TryRemove(key, out _);

        return Task.CompletedTask;
    }
}
