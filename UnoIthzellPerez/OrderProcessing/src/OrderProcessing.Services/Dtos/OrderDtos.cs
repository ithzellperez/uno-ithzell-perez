namespace OrderProcessing.Services.Dtos;

public record CreateOrderRequest(string CustomerId, IReadOnlyList<CreateOrderItemRequest> Items);

public record CreateOrderItemRequest(int ProductId, int Quantity);

public record OrderResponse(
    Guid Id,
    string CustomerId,
    string Status,
    decimal SubtotalAmount,
    decimal DiscountAmount,
    decimal TotalAmount,
    DateTime CreatedAt,
    IReadOnlyList<OrderItemResponse> Items);

public record OrderItemResponse(int ProductId, string ProductName, int Quantity, decimal UnitPrice, decimal LineTotal);

public record OrderSummaryResponse(
    Guid Id,
    string CustomerId,
    string Status,
    decimal TotalAmount,
    DateTime CreatedAt,
    int ItemCount);

public record UpdateOrderStatusRequest(string Status);

public record InventoryResponse(int ProductId, string ProductName, int Quantity, int Reserved, int Available);

public record ReserveInventoryRequest(Guid OrderId, int ProductId, int Quantity);

public record ReserveInventoryResponse(Guid ReservationId, int ProductId, int QuantityReserved);

public record DailySalesResponse(string Date, decimal TotalSales, int OrderCount);

public record TopProductResponse(int ProductId, string Name, int Quantity);

public record OrderCreatedNotificationRequest(
    Guid OrderId,
    string CustomerId,
    DateTime CreatedAt,
    decimal TotalAmount,
    IReadOnlyList<OrderCreatedNotificationItem> Items);

public record OrderCreatedNotificationItem(int ProductId, string ProductName, int Quantity, decimal UnitPrice);
