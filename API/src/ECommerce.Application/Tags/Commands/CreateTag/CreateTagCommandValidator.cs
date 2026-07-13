using FluentValidation;

namespace ECommerce.Application.Tags.Commands.CreateTag;

public sealed class CreateTagCommandValidator : AbstractValidator<CreateTagCommand>
{
    public CreateTagCommandValidator()
    {
        RuleFor(command => command.Name)
            .NotEmpty()
            .MaximumLength(150);

        RuleFor(command => command.Url)
            .MaximumLength(200);
    }
}
