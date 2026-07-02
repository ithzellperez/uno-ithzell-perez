using System.Net.Http.Json;
using OrderProcessing.Services.Dtos;

namespace OrderProcessing.Services.Clients;

public interface IInventoryClient
{
    Task<InventoryResponse?> GetInventoryAsync(int productId, CancellationToken ct = default);
    Task ReserveAsync(ReserveInventoryRequest request, CancellationToken ct = default);
}

public class InventoryClient(HttpClient http) : IInventoryClient
{
    public async Task<InventoryResponse?> GetInventoryAsync(int productId, CancellationToken ct = default)
    {
        var response = await http.GetAsync($"/api/inventory/{productId}", ct);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            return null;

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<InventoryResponse>(ct);
    }

    public async Task ReserveAsync(ReserveInventoryRequest request, CancellationToken ct = default)
    {
        var response = await http.PutAsJsonAsync("/api/inventory/reserve", request, ct);
        response.EnsureSuccessStatusCode();
    }
}

public interface IAnalyticsNotifier
{
    Task NotifyOrderCreatedAsync(OrderCreatedNotificationRequest request, CancellationToken ct = default);
}

public class AnalyticsNotifier(HttpClient http) : IAnalyticsNotifier
{
    public async Task NotifyOrderCreatedAsync(OrderCreatedNotificationRequest request, CancellationToken ct = default)
    {
        var response = await http.PostAsJsonAsync("/api/analytics/events/order-created", request, ct);
        response.EnsureSuccessStatusCode();
    }
}
