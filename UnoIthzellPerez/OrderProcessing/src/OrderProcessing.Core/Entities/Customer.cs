namespace OrderProcessing.Core.Entities;

public class Customer
{
    public string Id { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;

    private Customer() { }

    public Customer(string id, string name, string email)
    {
        Id = id;
        Name = name;
        Email = email;
    }
}
