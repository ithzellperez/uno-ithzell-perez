using Microsoft.EntityFrameworkCore;
using OrderProcessing.Core.Entities;

namespace OrderProcessing.Data;

public static class DbSeeder
{
    private static readonly (string Name, decimal Price, int Qty)[] Products =
    [
        ("Wireless Mouse", 29.99m, 100),
        ("Mechanical Keyboard", 89.99m, 75),
        ("USB-C Hub", 49.99m, 50),
        ("27\" Monitor", 299.99m, 30),
        ("Webcam HD", 59.99m, 60),
        ("Noise Cancelling Headphones", 199.99m, 40),
        ("External SSD 1TB", 129.99m, 55),
        ("Laptop Stand", 39.99m, 80),
        ("Desk Lamp", 24.99m, 90),
        ("Ergonomic Chair", 449.99m, 15)
    ];

    private static readonly (string Id, string Name, string Email)[] Customers =
    [
        ("CUST-001", "Alice Johnson", "alice@example.com"),
        ("CUST-002", "Bob Smith", "bob@example.com"),
        ("CUST-003", "Carol Williams", "carol@example.com"),
        ("CUST-004", "David Brown", "david@example.com"),
        ("CUST-005", "Eve Davis", "eve@example.com")
    ];

    public static async Task SeedAsync(OrderDbContext context)
    {
        await context.Database.MigrateAsync();

        if (!await context.Customers.AnyAsync())
        {
            foreach (var customer in Customers) 
                await context.Customers.AddAsync(new Customer(customer.Id, customer.Name, customer.Email));
        }

        if (!await context.Inventory.AnyAsync())
        {
            foreach (var product in Products)
                await context.Inventory.AddAsync(new InventoryItem(product.Name, product.Qty));
        }

        await context.SaveChangesAsync();
    }

    public static decimal GetProductPrice(string productName) =>
        Products.First(p => p.Name == productName).Price;
}
