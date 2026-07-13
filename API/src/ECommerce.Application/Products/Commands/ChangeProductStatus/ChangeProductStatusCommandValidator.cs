using FluentValidation;

namespace ECommerce.Application.Products.Commands.ChangeProductStatus;

public sealed class ChangeProductStatusCommandValidator : AbstractValidator<ChangeProductStatusCommand>
{
    public ChangeProductStatusCommandValidator()
    {
        RuleFor(command => command.Id)
            .NotEmpty();

        RuleFor(command => command.Status)
            .IsInEnum();
    }
}
