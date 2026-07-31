using FluentValidation;

namespace ECommerce.Application.Orders.Queries.GetOrderById;

public sealed class GetOrderByIdQueryValidator : AbstractValidator<GetOrderByIdQuery>
{
    // Burada sipariş detay isteğinin boş kimlik taşımadığını doğruluyorum.
    public GetOrderByIdQueryValidator()
    {
        RuleFor(query => query.OrderId).NotEmpty();
    }
}
