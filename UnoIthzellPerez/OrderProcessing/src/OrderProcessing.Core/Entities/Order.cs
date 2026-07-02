using OrderProcessing.Core.Enums;

namespace OrderProcessing.Core.Entities;

public class Order
{
    public Guid Id { get; private set; }
    public string CustomerId { get; private set; } = string.Empty;
    public DateTime CreatedAt { get; private set; }
    public OrderStatus Status { get; private set; }
    public decimal SubtotalAmount { get; private set; }
    public decimal DiscountAmount { get; private set; }
    public decimal TotalAmount { get; private set; }
    private readonly List<OrderItem> _items = [];
    public IReadOnlyCollection<OrderItem> Items => _items.AsReadOnly();

    private Order() { }

    public static Order Create(string customerId, IEnumerable<OrderItem> items)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(customerId);

        var itemList = items.ToList();
        if (itemList.Count == 0)
            throw new ArgumentException("Order must contain at least one item.", nameof(items));

        var order = new Order
        {
            Id = Guid.NewGuid(),
            CustomerId = customerId,
            CreatedAt = DateTime.UtcNow,
            Status = OrderStatus.Pending
        };

        foreach (var item in itemList)
            order._items.Add(item);

        order.CalculateTotals();
        return order;
    }

    public void ApplyDiscount(decimal percentage)
    {
        if (percentage < 0 || percentage > 100)
            throw new ArgumentOutOfRangeException(nameof(percentage));

        DiscountAmount = Math.Round(SubtotalAmount * (percentage / 100m), 2);
        TotalAmount = SubtotalAmount - DiscountAmount;
    }

    public bool CanTransitionTo(OrderStatus newStatus)
    {
        return Status switch
        {
            OrderStatus.Pending => newStatus is OrderStatus.Confirmed or OrderStatus.Cancelled,
            OrderStatus.Confirmed => newStatus is OrderStatus.Shipped or OrderStatus.Cancelled,
            OrderStatus.Shipped => newStatus is OrderStatus.Delivered,
            OrderStatus.Delivered => false,
            OrderStatus.Cancelled => false,
            _ => false
        };
    }

    public void TransitionTo(OrderStatus newStatus)
    {
        if (!CanTransitionTo(newStatus))
            throw new InvalidOperationException($"Cannot transition from {Status} to {newStatus}.");

        Status = newStatus;
    }

    private void CalculateTotals()
    {
        SubtotalAmount = _items.Sum(i => i.UnitPrice * i.Quantity);
        var discountPercentage = SubtotalAmount switch
        {
            > 1000m => 20m,
            > 500m => 10m,
            _ => 0m
        };
        ApplyDiscount(discountPercentage);
    }
}
