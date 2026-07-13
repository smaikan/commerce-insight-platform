using FluentValidation;

namespace ECommerce.Application.Collections.Commands.CreateCollection;

public sealed class CreateCollectionCommandValidator : AbstractValidator<CreateCollectionCommand>
{
    public CreateCollectionCommandValidator()
    {
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
