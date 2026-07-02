using Microsoft.EntityFrameworkCore;
using OrderProcessing.Core.Entities;
using OrderProcessing.Core.Enums;
using OrderProcessing.Core.Interfaces;

namespace OrderProcessing.Data.Repositories;

public class OrderRepository(OrderDbContext context) : IOrderRepository
{
    public async Task<Order?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        await context.Orders
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Order>> GetAllAsync(string? status, CancellationToken cancellationToken = default)
    {
        var query = context.Orders.Include(o => o.Items).AsQueryable();

        if (!string.IsNullOrWhiteSpace(status)
            && Enum.TryParse<OrderStatus>(status, true, out var orderStatus))
        {
            query = query.Where(o => o.Status == orderStatus);
        }

        return await query
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(Order order, CancellationToken cancellationToken = default)
    {
        foreach (var item in order.Items)
            item.AssignToOrder(order.Id);

        await context.Orders.AddAsync(order, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(Order order, CancellationToken cancellationToken = default)
    {
        context.Orders.Update(order);
        await context.SaveChangesAsync(cancellationToken);
    }
}

public class CustomerRepository(OrderDbContext context) : ICustomerRepository
{
    public async Task<Customer?> GetByIdAsync(string customerId, CancellationToken cancellationToken = default) =>
        await context.Customers.FirstOrDefaultAsync(c => c.Id == customerId, cancellationToken);
}

public class InventoryRepository(OrderDbContext context) : IInventoryRepository
{
    public async Task<InventoryItem?> GetByProductIdAsync(int productId, CancellationToken cancellationToken = default) =>
        await context.Inventory.FirstOrDefaultAsync(i => i.ProductId == productId, cancellationToken);

    public async Task UpdateAsync(InventoryItem item, CancellationToken cancellationToken = default)
    {
        context.Inventory.Update(item);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<InventoryReservation>> GetExpiredReservationsAsync(
        DateTime cutoff, CancellationToken cancellationToken = default) =>
        await context.InventoryReservations
            .Where(r => r.ReleasedAt == null && r.ReservedAt < cutoff)
            .ToListAsync(cancellationToken);

    public async Task<InventoryReservation?> GetActiveReservationAsync(
        Guid orderId, int productId, CancellationToken cancellationToken = default) =>
        await context.InventoryReservations
            .FirstOrDefaultAsync(r => r.OrderId == orderId && r.ProductId == productId && r.ReleasedAt == null, cancellationToken);

    public async Task AddReservationAsync(InventoryReservation reservation, CancellationToken cancellationToken = default)
    {
        await context.InventoryReservations.AddAsync(reservation, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateReservationAsync(InventoryReservation reservation, CancellationToken cancellationToken = default)
    {
        context.InventoryReservations.Update(reservation);
        await context.SaveChangesAsync(cancellationToken);
    }
}
