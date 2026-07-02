using OrderProcessing.Core.Entities;

namespace OrderProcessing.Core.Interfaces;

public interface IOrderRepository
{
    Task<Order?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Order>> GetAllAsync(string? status, CancellationToken cancellationToken = default);
    Task AddAsync(Order order, CancellationToken cancellationToken = default);
    Task UpdateAsync(Order order, CancellationToken cancellationToken = default);
}

public interface ICustomerRepository
{
    Task<Customer?> GetByIdAsync(string customerId, CancellationToken cancellationToken = default);
}

public interface IInventoryRepository
{
    Task<InventoryItem?> GetByProductIdAsync(int productId, CancellationToken cancellationToken = default);
    Task UpdateAsync(InventoryItem item, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<InventoryReservation>> GetExpiredReservationsAsync(DateTime cutoff, CancellationToken cancellationToken = default);
    Task<InventoryReservation?> GetActiveReservationAsync(Guid orderId, int productId, CancellationToken cancellationToken = default);
    Task AddReservationAsync(InventoryReservation reservation, CancellationToken cancellationToken = default);
    Task UpdateReservationAsync(InventoryReservation reservation, CancellationToken cancellationToken = default);
}
