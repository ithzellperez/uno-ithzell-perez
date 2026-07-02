using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OrderProcessing.Services.Inventory;

namespace OrderProcessing.Services.BackgroundJobs;

public class ReservationCleanupService(IServiceScopeFactory scopeFactory, ILogger<ReservationCleanupService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var inventoryService = scope.ServiceProvider.GetRequiredService<IInventoryService>();
                await inventoryService.ReleaseExpiredReservationsAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Error releasing expired inventory reservations");
            }

            await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
        }
    }
}
