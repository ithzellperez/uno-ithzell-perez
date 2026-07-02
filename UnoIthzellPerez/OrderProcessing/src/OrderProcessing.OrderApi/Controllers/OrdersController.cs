using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using OrderProcessing.Services.Dtos;
using OrderProcessing.Services.Orders;

namespace OrderProcessing.OrderApi.Controllers;

[ApiController]
[Route("api/orders")]
public class OrdersController(
    IOrderService orderService,
    IValidator<CreateOrderRequest> createValidator,
    IValidator<UpdateOrderStatusRequest> statusValidator) : ControllerBase
{
    [HttpGet("ping")]
    public IActionResult Ping() => Ok(new { status = "ok" });

    [HttpGet]
    public async Task<IActionResult> List([FromQuery] string? status, CancellationToken ct)
    {
        var orders = await orderService.ListOrdersAsync(status, ct);
        return Ok(orders);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateOrderRequest request, CancellationToken ct)
    {
        await createValidator.ValidateAndThrowAsync(request, ct);
        var order = await orderService.CreateOrderAsync(request, ct);
        return CreatedAtAction(nameof(GetById), new { id = order.Id }, order);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        return Ok(await orderService.GetOrderAsync(id, ct));
    }

    [HttpPut("{id:guid}/status")]
    public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] UpdateOrderStatusRequest request, CancellationToken ct)
    {
        await statusValidator.ValidateAndThrowAsync(request, ct);
        return Ok(await orderService.UpdateStatusAsync(id, request, ct));
    }
}
