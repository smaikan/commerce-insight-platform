using FluentValidation;

namespace ECommerce.Application.Brands.Commands.UpdateBrand;

public sealed class UpdateBrandCommandValidator : AbstractValidator<UpdateBrandCommand>
{
    public UpdateBrandCommandValidator()
    {
        RuleFor(command => command.Id)
            .NotEmpty();

        RuleFor(command => command.Name)
            .NotEmpty()
            .MaximumLength(150);

        RuleFor(command => command.Url)
            .MaximumLength(200);

        RuleFor(command => command.Description)
            .MaximumLength(1000);
    }
}
