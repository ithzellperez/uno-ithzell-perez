using Microsoft.AspNetCore.Mvc;
using OrderProcessing.Services.Analytics;
using OrderProcessing.Services.Dtos;

namespace OrderProcessing.AnalyticsApi.Controllers;

[ApiController]
[Route("api/analytics")]
public class AnalyticsController(IAnalyticsService analyticsService) : ControllerBase
{
    [HttpGet("daily-sales")]
    public async Task<IActionResult> GetDailySales([FromQuery] int days = 30, CancellationToken ct = default)
        => Ok(await analyticsService.GetDailySalesAsync(days, ct));

    [HttpGet("top-products")]
    public async Task<IActionResult> GetTopProducts([FromQuery] int limit = 5, CancellationToken ct = default)
        => Ok(await analyticsService.GetTopProductsAsync(limit, ct));

    [HttpPost("events/order-created")]
    public async Task<IActionResult> OrderCreated([FromBody] OrderCreatedNotificationRequest request, CancellationToken ct)
    {
        await analyticsService.ProcessOrderCreatedAsync(request, ct);
        return Accepted();
    }
}
