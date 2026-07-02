using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;
using OrderProcessing.Services.Clients;
using OrderProcessing.Services.Dtos;
using Testcontainers.MsSql;
using Xunit;

namespace OrderProcessing.IntegrationTests;

public class OrderApiIntegrationTests : IAsyncLifetime
{
    private readonly MsSqlContainer _sqlContainer = new MsSqlBuilder()
        .WithPassword("Password123")
        .Build();

    private WebApplicationFactory<Program> _factory = null!;
    private HttpClient _client = null!;
    private readonly Mock<IInventoryClient> _inventoryMock = new();

    public async Task InitializeAsync()
    {
        await _sqlContainer.StartAsync();

        _inventoryMock.Setup(x => x.GetInventoryAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((int productId, CancellationToken _) => new InventoryResponse(
                productId, "Wireless Mouse", 100, 0, 100));

        _inventoryMock.Setup(x => x.ReserveAsync(It.IsAny<ReserveInventoryRequest>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseSetting("ConnectionStrings:OrderDb", _sqlContainer.GetConnectionString());
                builder.ConfigureServices(services =>
                {
                    services.RemoveAll<IInventoryClient>();
                    services.AddSingleton(_inventoryMock.Object);
                    services.RemoveAll<IAnalyticsNotifier>();
                    services.AddSingleton(Mock.Of<IAnalyticsNotifier>());
                });
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
    public async Task Ping_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/orders/ping");
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadFromJsonAsync<Dictionary<string, string>>();
        Assert.Equal("ok", json!["status"]);
    }

    [Fact]
    public async Task CreateOrder_WithValidData_ReturnsCreated()
    {
        var request = new CreateOrderRequest("CUST-001", [new CreateOrderItemRequest(1, 2)]);

        var response = await _client.PostAsJsonAsync("/api/orders", request);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var order = await response.Content.ReadFromJsonAsync<OrderResponse>();
        Assert.NotNull(order);
        Assert.True(order!.TotalAmount > 0);
        Assert.Equal("Pending", order.Status);

        _inventoryMock.Verify(x => x.ReserveAsync(
            It.Is<ReserveInventoryRequest>(r => r.ProductId == 1 && r.Quantity == 2),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateStatus_FollowsStateMachine()
    {
        var createResponse = await _client.PostAsJsonAsync("/api/orders",
            new CreateOrderRequest("CUST-002", [new CreateOrderItemRequest(2, 1)]));
        var order = await createResponse.Content.ReadFromJsonAsync<OrderResponse>();

        var confirmResponse = await _client.PutAsJsonAsync($"/api/orders/{order!.Id}/status",
            new UpdateOrderStatusRequest("Confirmed"));
        confirmResponse.EnsureSuccessStatusCode();

        var invalidResponse = await _client.PutAsJsonAsync($"/api/orders/{order.Id}/status",
            new UpdateOrderStatusRequest("Delivered"));
        Assert.Equal(HttpStatusCode.BadRequest, invalidResponse.StatusCode);
    }
}
