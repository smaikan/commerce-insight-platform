using FluentValidation;

namespace ECommerce.Application.Orders.Queries.GetOrderByIdForAdmin;

public sealed class GetOrderByIdForAdminQueryValidator : AbstractValidator<GetOrderByIdForAdminQuery>
{
    // Burada yönetim sipariş detay isteğinin boş kimlik taşımadığını doğruluyorum.
    public GetOrderByIdForAdminQueryValidator()
    {
        RuleFor(query => query.OrderId).NotEmpty();
    }
}
