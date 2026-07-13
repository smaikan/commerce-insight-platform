using FluentValidation;

namespace ECommerce.Application.Collections.Commands.UpdateCollection;

public sealed class UpdateCollectionCommandValidator : AbstractValidator<UpdateCollectionCommand>
{
    public UpdateCollectionCommandValidator()
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

        RuleFor(command => command.DisplayOrder)
            .GreaterThanOrEqualTo(0);
    }
}
