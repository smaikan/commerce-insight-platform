using FluentValidation;

namespace ECommerce.Application.Brands.Commands.CreateBrand;

public sealed class CreateBrandCommandValidator : AbstractValidator<CreateBrandCommand>
{
    public CreateBrandCommandValidator()
    {
        RuleFor(command => command.Name)
            .NotEmpty()
            .MaximumLength(150);

        RuleFor(command => command.Url)
            .MaximumLength(200);

        RuleFor(command => command.Description)
            .MaximumLength(1000);
    }
}
