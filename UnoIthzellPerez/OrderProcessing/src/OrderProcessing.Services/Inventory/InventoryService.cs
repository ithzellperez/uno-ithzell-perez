using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OrderProcessing.Core.Entities;
using OrderProcessing.Core.Exceptions;
using OrderProcessing.Core.Interfaces;
using OrderProcessing.Services.Dtos;

namespace OrderProcessing.Services.Inventory;

public interface IInventoryService
{
    Task<InventoryResponse?> GetInventoryAsync(int productId, CancellationToken cancellationToken = default);
    Task<ReserveInventoryResponse> ReserveAsync(ReserveInventoryRequest request, CancellationToken cancellationToken = default);
    Task ReleaseExpiredReservationsAsync(CancellationToken cancellationToken = default);
}

public class InventoryService(
    IInventoryRepository inventoryRepository,
    ILogger<InventoryService> logger) : IInventoryService
{
    public async Task<InventoryResponse?> GetInventoryAsync(int productId, CancellationToken cancellationToken = default)
    {
        var item = await inventoryRepository.GetByProductIdAsync(productId, cancellationToken);
        return item is null ? null : MapToResponse(item);
    }

    public async Task<ReserveInventoryResponse> ReserveAsync(ReserveInventoryRequest request, CancellationToken cancellationToken = default)
    {
        var existing = await inventoryRepository.GetActiveReservationAsync(request.OrderId, request.ProductId, cancellationToken);
        if (existing is not null)
            return new ReserveInventoryResponse(existing.Id, request.ProductId, existing.Quantity);

        var item = await inventoryRepository.GetByProductIdAsync(request.ProductId, cancellationToken)
            ?? throw new NotFoundException($"Product {request.ProductId} not found.");

        if (item.Available < request.Quantity)
            throw new ValidationException($"Insufficient stock for product {request.ProductId}.");

        item.Reserve(request.Quantity);
        var reservation = InventoryReservation.Create(request.OrderId, request.ProductId, request.Quantity);

        try
        {
            await inventoryRepository.UpdateAsync(item, cancellationToken);
            await inventoryRepository.AddReservationAsync(reservation, cancellationToken);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            logger.LogWarning(ex, "Concurrency conflict reserving product {ProductId}", request.ProductId);
            throw new ConcurrencyException("Inventory was modified by another process. Please retry.");
        }

        return new ReserveInventoryResponse(reservation.Id, request.ProductId, request.Quantity);
    }

    public async Task ReleaseExpiredReservationsAsync(CancellationToken cancellationToken = default)
    {
        var cutoff = DateTime.UtcNow.AddMinutes(-15);
        var expired = await inventoryRepository.GetExpiredReservationsAsync(cutoff, cancellationToken);

        foreach (var reservation in expired)
        {
            var item = await inventoryRepository.GetByProductIdAsync(reservation.ProductId, cancellationToken);
            if (item is null)
                continue;

            item.Release(reservation.Quantity);
            reservation.Release();

            try
            {
                await inventoryRepository.UpdateAsync(item, cancellationToken);
                await inventoryRepository.UpdateReservationAsync(reservation, cancellationToken);
                logger.LogInformation("Released expired reservation {ReservationId} for product {ProductId}",
                    reservation.Id, reservation.ProductId);
            }
            catch (DbUpdateConcurrencyException ex)
            {
                logger.LogWarning(ex, "Concurrency conflict releasing reservation {ReservationId}", reservation.Id);
            }
        }
    }

    private static InventoryResponse MapToResponse(InventoryItem item) =>
        new(item.ProductId, item.ProductName, item.Quantity, item.Reserved, item.Available);
}
