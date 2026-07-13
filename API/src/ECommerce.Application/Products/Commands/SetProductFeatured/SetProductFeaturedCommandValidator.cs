using FluentValidation;

namespace ECommerce.Application.Products.Commands.SetProductFeatured;

public sealed class SetProductFeaturedCommandValidator : AbstractValidator<SetProductFeaturedCommand>
{
    public SetProductFeaturedCommandValidator()
    {
        RuleFor(command => command.Id)
            .NotEmpty();
    }
}
