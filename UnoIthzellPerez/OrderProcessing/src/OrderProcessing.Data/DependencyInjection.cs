using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OrderProcessing.Core.Interfaces;
using OrderProcessing.Data.Repositories;

namespace OrderProcessing.Data;

public static class DependencyInjection
{
    public static IServiceCollection AddOrderData(this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<OrderDbContext>(options =>
            options.UseSqlServer(connectionString));

        services.AddScoped<IOrderRepository, OrderRepository>();
        services.AddScoped<ICustomerRepository, CustomerRepository>();
        services.AddScoped<IInventoryRepository, InventoryRepository>();

        return services;
    }
}
