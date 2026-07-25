using ECommerce.Domain.Entities;
using FluentValidation;

namespace ECommerce.Application.Carts.Queries.GetCart;

public sealed class GetCartQueryValidator : AbstractValidator<GetCartQuery>
{
    // Burada isteğe bağlı misafir oturumunun veritabanı uzunluk sınırını doğruluyorum.
    public GetCartQueryValidator()
    {
        RuleFor(query => query.SessionId)
            .MaximumLength(Cart.MaximumSessionIdLength);
    }
}
