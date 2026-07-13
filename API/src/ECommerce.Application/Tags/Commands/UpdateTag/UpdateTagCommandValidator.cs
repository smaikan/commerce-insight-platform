using FluentValidation;

namespace ECommerce.Application.Tags.Commands.UpdateTag;

public sealed class UpdateTagCommandValidator : AbstractValidator<UpdateTagCommand>
{
    public UpdateTagCommandValidator()
    {
        RuleFor(command => command.Id)
            .NotEmpty();

        RuleFor(command => command.Name)
            .NotEmpty()
            .MaximumLength(150);

        RuleFor(command => command.Url)
            .MaximumLength(200);
    }
}
