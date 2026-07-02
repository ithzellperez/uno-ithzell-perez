namespace OrderProcessing.Core.Entities;

public class InventoryReservation
{
    public Guid Id { get; private set; }
    public Guid OrderId { get; private set; }
    public int ProductId { get; private set; }
    public int Quantity { get; private set; }
    public DateTime ReservedAt { get; private set; }
    public DateTime? ReleasedAt { get; private set; }
    public bool IsActive => ReleasedAt is null;

    private InventoryReservation() { }

    public static InventoryReservation Create(Guid orderId, int productId, int quantity)
    {
        return new InventoryReservation
        {
            Id = Guid.NewGuid(),
            OrderId = orderId,
            ProductId = productId,
            Quantity = quantity,
            ReservedAt = DateTime.UtcNow
        };
    }

    public void Release()
    {
        if (ReleasedAt is not null)
            return;

        ReleasedAt = DateTime.UtcNow;
    }
}
