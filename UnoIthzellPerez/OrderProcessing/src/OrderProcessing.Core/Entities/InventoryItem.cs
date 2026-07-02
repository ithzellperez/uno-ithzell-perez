namespace OrderProcessing.Core.Entities;

public class InventoryItem
{
    public int ProductId { get; private set; }
    public string ProductName { get; private set; } = string.Empty;
    public int Quantity { get; private set; }
    public int Reserved { get; private set; }
    public byte[] RowVersion { get; private set; } = [];

    private InventoryItem() { }

    public InventoryItem(string productName, int quantity)
    {
        ProductName = productName;
        Quantity = quantity;
    }

    public int Available => Quantity - Reserved;

    public void Reserve(int amount)
    {
        if (amount <= 0)
            throw new ArgumentOutOfRangeException(nameof(amount));
        if (amount > Available)
            throw new InvalidOperationException("Insufficient available inventory.");

        Reserved += amount;
    }

    public void Release(int amount)
    {
        if (amount <= 0)
            throw new ArgumentOutOfRangeException(nameof(amount));
        if (amount > Reserved)
            throw new InvalidOperationException("Cannot release more than reserved.");

        Reserved -= amount;
    }
}
