using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using OrderProcessing.Services.Dtos;
using Testcontainers.MsSql;
using Xunit;

namespace OrderProcessing.InventoryApi.Tests;

public class InventoryConcurrencyIntegrationTests : IAsyncLifetime
{
    private readonly MsSqlContainer _sqlContainer = new MsSqlBuilder()
        .WithPassword("Password123")
        .Build();

    private WebApplicationFactory<Program> _factory = null!;
    private HttpClient _client = null!;

    public async Task InitializeAsync()
    {
        await _sqlContainer.StartAsync();

        _factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseSetting("ConnectionStrings:OrderDb", _sqlContainer.GetConnectionString());
            });

        _client = _factory.CreateClient();
    }

    public async Task DisposeAsync()
    {
        _client.Dispose();
        await _factory.DisposeAsync();
        await _sqlContainer.DisposeAsync();
    }

    [Fact]
    public async Task ReserveInventory_ConcurrentRequests_OneSucceedsOneConflicts()
    {
        var orderId1 = Guid.NewGuid();
        var orderId2 = Guid.NewGuid();
        const int productId = 10;
        const int quantity = 14;

        var request1 = new ReserveInventoryRequest(orderId1, productId, quantity);
        var request2 = new ReserveInventoryRequest(orderId2, productId, quantity);

        var task1 = _client.PutAsJsonAsync("/api/inventory/reserve", request1);
        var task2 = _client.PutAsJsonAsync("/api/inventory/reserve", request2);

        await Task.WhenAll(task1, task2);

        var statuses = new[] { task1.Result.StatusCode, task2.Result.StatusCode };
        Assert.Contains(HttpStatusCode.OK, statuses);
        Assert.Contains(HttpStatusCode.Conflict, statuses);
    }
}
