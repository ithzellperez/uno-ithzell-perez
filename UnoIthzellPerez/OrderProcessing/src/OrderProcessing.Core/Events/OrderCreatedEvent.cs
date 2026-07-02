using MediatR;
using OrderProcessing.Core.Enums;

namespace OrderProcessing.Core.Events;

public record OrderCreatedEvent(
    Guid OrderId,
    string CustomerId,
    DateTime CreatedAt,
    decimal TotalAmount,
    IReadOnlyList<OrderCreatedItem> Items) : INotification;

public record OrderCreatedItem(int ProductId, string ProductName, int Quantity, decimal UnitPrice);
