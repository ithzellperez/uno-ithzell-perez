using FluentValidation;
using OrderProcessing.Services.Dtos;

namespace OrderProcessing.Services.Validators;

public class CreateOrderRequestValidator : AbstractValidator<CreateOrderRequest>
{
    public CreateOrderRequestValidator()
    {
        RuleFor(x => x.CustomerId).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Items).NotEmpty();
        RuleForEach(x => x.Items).SetValidator(new CreateOrderItemRequestValidator());
    }
}

public class CreateOrderItemRequestValidator : AbstractValidator<CreateOrderItemRequest>
{
    public CreateOrderItemRequestValidator()
    {
        RuleFor(x => x.ProductId).GreaterThan(0);
        RuleFor(x => x.Quantity).GreaterThan(0).LessThanOrEqualTo(100);
    }
}

public class UpdateOrderStatusRequestValidator : AbstractValidator<UpdateOrderStatusRequest>
{
    private static readonly string[] ValidStatuses = ["Pending", "Confirmed", "Shipped", "Delivered", "Cancelled"];

    public UpdateOrderStatusRequestValidator()
    {
        RuleFor(x => x.Status).NotEmpty().Must(s => ValidStatuses.Contains(s, StringComparer.OrdinalIgnoreCase))
            .WithMessage("Status must be one of: Pending, Confirmed, Shipped, Delivered, Cancelled.");
    }
}

public class ReserveInventoryRequestValidator : AbstractValidator<ReserveInventoryRequest>
{
    public ReserveInventoryRequestValidator()
    {
        RuleFor(x => x.OrderId).NotEmpty();
        RuleFor(x => x.ProductId).GreaterThan(0);
        RuleFor(x => x.Quantity).GreaterThan(0);
    }
}
