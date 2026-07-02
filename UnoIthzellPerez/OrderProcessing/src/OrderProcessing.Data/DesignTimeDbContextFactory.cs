using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace OrderProcessing.Data;

public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<OrderDbContext>
{
    public OrderDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<OrderDbContext>();
        optionsBuilder.UseSqlServer(
            "Server=localhost,1433;Database=OrderProcessing;User Id=sa;Password=Password123;TrustServerCertificate=True");

        return new OrderDbContext(optionsBuilder.Options);
    }
}
