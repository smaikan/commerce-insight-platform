using FluentValidation;

namespace ECommerce.Application.Collections.Commands.SetCollectionFeatured;

public sealed class SetCollectionFeaturedCommandValidator : AbstractValidator<SetCollectionFeaturedCommand>
{
    public SetCollectionFeaturedCommandValidator()
    {
        RuleFor(command => command.Id)
            .NotEmpty();
    }
}
