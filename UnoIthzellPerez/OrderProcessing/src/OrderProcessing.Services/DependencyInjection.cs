using FluentValidation;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OrderProcessing.Services.Analytics;
using OrderProcessing.Services.BackgroundJobs;
using OrderProcessing.Services.Clients;
using OrderProcessing.Services.Inventory;
using OrderProcessing.Services.Orders;
using OrderProcessing.Services.Validators;

namespace OrderProcessing.Services;

public static class DependencyInjection
{
    public static IServiceCollection AddOrderServices(this IServiceCollection services)
    {
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly));
        services.AddValidatorsFromAssemblyContaining<CreateOrderRequestValidator>();
        services.AddScoped<IOrderService, OrderService>();
        return services;
    }

    public static IServiceCollection AddInventoryServices(this IServiceCollection services)
    {
        services.AddScoped<IInventoryService, InventoryService>();
        services.AddHostedService<ReservationCleanupService>();
        services.AddValidatorsFromAssemblyContaining<ReserveInventoryRequestValidator>();
        return services;
    }

    public static IServiceCollection AddAnalyticsServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<IAnalyticsService, AnalyticsService>();

        var redis = configuration.GetConnectionString("Redis");
        if (string.IsNullOrWhiteSpace(redis))
        {
            services.AddSingleton<IAnalyticsCache, InMemoryAnalyticsCache>();
            return services;
        }

        services.AddSingleton<StackExchange.Redis.IConnectionMultiplexer>(
            _ => StackExchange.Redis.ConnectionMultiplexer.Connect(redis));
        services.AddSingleton<IAnalyticsCache, RedisAnalyticsCache>();
        return services;
    }

    public static IServiceCollection AddInventoryClient(this IServiceCollection services, IConfiguration configuration)
    {
        var baseUrl = configuration["Services:InventoryApi"] ?? "http://localhost:5002";
        services.AddHttpClient<IInventoryClient, InventoryClient>(c => c.BaseAddress = new Uri(baseUrl));
        return services;
    }

    public static IServiceCollection AddAnalyticsNotifier(this IServiceCollection services, IConfiguration configuration)
    {
        var baseUrl = configuration["Services:AnalyticsApi"] ?? "http://localhost:5003";
        services.AddHttpClient<IAnalyticsNotifier, AnalyticsNotifier>(c => c.BaseAddress = new Uri(baseUrl));
        return services;
    }
}
