using MediatR;
using Microsoft.Extensions.Logging;
using OrderProcessing.Core.Entities;
using OrderProcessing.Core.Enums;
using OrderProcessing.Core.Events;
using OrderProcessing.Core.Exceptions;
using OrderProcessing.Core.Interfaces;
using OrderProcessing.Data;
using OrderProcessing.Services.Clients;
using OrderProcessing.Services.Dtos;

namespace OrderProcessing.Services.Orders;

public interface IOrderService
{
    Task<OrderResponse> CreateOrderAsync(CreateOrderRequest request, CancellationToken ct = default);
    Task<OrderResponse> GetOrderAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<OrderSummaryResponse>> ListOrdersAsync(string? status, CancellationToken ct = default);
    Task<OrderResponse> UpdateStatusAsync(Guid id, UpdateOrderStatusRequest request, CancellationToken ct = default);
}

public class OrderService(
    ICustomerRepository customerRepository,
    IOrderRepository orderRepository,
    IInventoryClient inventoryClient,
    IMediator mediator,
    ILogger<OrderService> logger) : IOrderService
{
    public async Task<OrderResponse> CreateOrderAsync(CreateOrderRequest request, CancellationToken ct = default)
    {
        var customer = await customerRepository.GetByIdAsync(request.CustomerId, ct)
            ?? throw new NotFoundException($"Customer '{request.CustomerId}' not found.");

        var orderItems = new List<OrderItem>();
        foreach (var item in request.Items)
        {
            var inventory = await inventoryClient.GetInventoryAsync(item.ProductId, ct)
                ?? throw new NotFoundException($"Product {item.ProductId} not found.");

            if (inventory.Available < item.Quantity)
                throw new ValidationException($"Insufficient stock for product {item.ProductId}.");

            orderItems.Add(OrderItem.Create(
                item.ProductId,
                inventory.ProductName,
                item.Quantity,
                DbSeeder.GetProductPrice(inventory.ProductName)));
        }

        var order = Order.Create(customer.Id, orderItems);
        await orderRepository.AddAsync(order, ct);

        foreach (var item in request.Items)
            await inventoryClient.ReserveAsync(new ReserveInventoryRequest(order.Id, item.ProductId, item.Quantity), ct);

        logger.LogInformation("Created order {OrderId}, total {Total}", order.Id, order.TotalAmount);

        await mediator.Publish(new OrderCreatedEvent(
            order.Id,
            order.CustomerId,
            order.CreatedAt,
            order.TotalAmount,
            order.Items.Select(i => new OrderCreatedItem(i.ProductId, i.ProductName, i.Quantity, i.UnitPrice)).ToList()),
            ct);

        return ToResponse(order);
    }

    public async Task<OrderResponse> GetOrderAsync(Guid id, CancellationToken ct = default)
    {
        var order = await orderRepository.GetByIdAsync(id, ct)
            ?? throw new NotFoundException($"Order '{id}' not found.");
        return ToResponse(order);
    }

    public async Task<IReadOnlyList<OrderSummaryResponse>> ListOrdersAsync(string? status, CancellationToken ct = default)
    {
        var orders = await orderRepository.GetAllAsync(status, ct);
        return orders.Select(o => new OrderSummaryResponse(
            o.Id, o.CustomerId, o.Status.ToString(), o.TotalAmount, o.CreatedAt, o.Items.Count)).ToList();
    }

    public async Task<OrderResponse> UpdateStatusAsync(Guid id, UpdateOrderStatusRequest request, CancellationToken ct = default)
    {
        var order = await orderRepository.GetByIdAsync(id, ct)
            ?? throw new NotFoundException($"Order '{id}' not found.");

        if (!Enum.TryParse<OrderStatus>(request.Status, true, out var newStatus))
            throw new ValidationException($"Invalid status '{request.Status}'.");

        order.TransitionTo(newStatus);
        await orderRepository.UpdateAsync(order, ct);
        return ToResponse(order);
    }

    private static OrderResponse ToResponse(Order order) =>
        new(
            order.Id,
            order.CustomerId,
            order.Status.ToString(),
            order.SubtotalAmount,
            order.DiscountAmount,
            order.TotalAmount,
            order.CreatedAt,
            order.Items.Select(i => new OrderItemResponse(
                i.ProductId, i.ProductName, i.Quantity, i.UnitPrice, i.UnitPrice * i.Quantity)).ToList());
}
