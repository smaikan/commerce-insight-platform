using FluentValidation;

namespace ECommerce.Application.Brands.Commands.SetBrandActivation;

public sealed class SetBrandActivationCommandValidator : AbstractValidator<SetBrandActivationCommand>
{
    public SetBrandActivationCommandValidator()
    {
        RuleFor(command => command.Id)
            .NotEmpty();
    }
}
