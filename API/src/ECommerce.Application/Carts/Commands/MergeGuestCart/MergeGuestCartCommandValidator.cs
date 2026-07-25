using ECommerce.Domain.Entities;
using FluentValidation;

namespace ECommerce.Application.Carts.Commands.MergeGuestCart;

public sealed class MergeGuestCartCommandValidator : AbstractValidator<MergeGuestCartCommand>
{
    // Burada birleştirilecek misafir session değerinin dolu ve desteklenen uzunlukta olduğunu doğruluyorum.
    public MergeGuestCartCommandValidator()
    {
        RuleFor(command => command.SessionId)
            .NotEmpty()
            .MaximumLength(Cart.MaximumSessionIdLength);
    }
}
