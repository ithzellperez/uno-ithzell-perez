using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using OrderProcessing.Services.Dtos;
using OrderProcessing.Services.Inventory;

namespace OrderProcessing.InventoryApi.Controllers;

[ApiController]
[Route("api/inventory")]
public class InventoryController(
    IInventoryService inventoryService,
    IValidator<ReserveInventoryRequest> validator) : ControllerBase
{
    [HttpGet("{productId:int}")]
    public async Task<IActionResult> Get(int productId, CancellationToken ct)
    {
        var item = await inventoryService.GetInventoryAsync(productId, ct);
        return item is null ? NotFound() : Ok(item);
    }

    [HttpPut("reserve")]
    public async Task<IActionResult> Reserve([FromBody] ReserveInventoryRequest request, CancellationToken ct)
    {
        await validator.ValidateAndThrowAsync(request, ct);
        return Ok(await inventoryService.ReserveAsync(request, ct));
    }
}
