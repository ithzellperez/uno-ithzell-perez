namespace OrderProcessing.Core.Entities;

public class OrderItem
{
    public Guid Id { get; private set; }
    public Guid OrderId { get; private set; }
    public int ProductId { get; private set; }
    public string ProductName { get; private set; } = string.Empty;
    public int Quantity { get; private set; }
    public decimal UnitPrice { get; private set; }
    public Order Order { get; private set; } = null!;

    private OrderItem() { }

    public static OrderItem Create(int productId, string productName, int quantity, decimal unitPrice)
    {
        if (productId <= 0)
            throw new ArgumentOutOfRangeException(nameof(productId));
        if (quantity <= 0)
            throw new ArgumentOutOfRangeException(nameof(quantity));
        if (unitPrice < 0)
            throw new ArgumentOutOfRangeException(nameof(unitPrice));

        return new OrderItem
        {
            Id = Guid.NewGuid(),
            ProductId = productId,
            ProductName = productName,
            Quantity = quantity,
            UnitPrice = unitPrice
        };
    }

    public void AssignToOrder(Guid orderId) => OrderId = orderId;
}
