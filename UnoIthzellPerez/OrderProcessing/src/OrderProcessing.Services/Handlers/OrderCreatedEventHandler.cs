using MediatR;
using Microsoft.Extensions.Logging;
using OrderProcessing.Core.Events;
using OrderProcessing.Services.Clients;
using OrderProcessing.Services.Dtos;

namespace OrderProcessing.Services.Handlers;

public class OrderCreatedEventHandler(
    IAnalyticsNotifier analyticsNotifier,
    ILogger<OrderCreatedEventHandler> logger) : INotificationHandler<OrderCreatedEvent>
{
    public async Task Handle(OrderCreatedEvent notification, CancellationToken cancellationToken)
    {
        var request = new OrderCreatedNotificationRequest(
            notification.OrderId,
            notification.CustomerId,
            notification.CreatedAt,
            notification.TotalAmount,
            notification.Items.Select(i => new OrderCreatedNotificationItem(
                i.ProductId, i.ProductName, i.Quantity, i.UnitPrice)).ToList());

        try
        {
            await analyticsNotifier.NotifyOrderCreatedAsync(request, cancellationToken);
        }
        catch (Exception ex)
        {
            // Order is already committed — don't fail the request if analytics is down.
            logger.LogWarning(ex, "Analytics notification failed for order {OrderId}", notification.OrderId);
        }
    }
}
