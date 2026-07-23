using MediatR;

namespace ECommerce.Application.Products.Engagement.Commands.RecordProductActivity;

public sealed record RecordProductActivityCommand(
    long ProductId,
    ProductActivityType ActivityType,
    Guid? ProductVariantId = null,
    int Quantity = 1) : IRequest;

public enum ProductActivityType
{
    Click,
    AddToCart,
    Purchase
}
