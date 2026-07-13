using FluentValidation;

namespace ECommerce.Application.Auth.Commands.CreateEmailConfirmationToken;

public sealed class CreateEmailConfirmationTokenCommandValidator : AbstractValidator<CreateEmailConfirmationTokenCommand>
{
    public CreateEmailConfirmationTokenCommandValidator()
    {
        RuleFor(command => command.UserId)
            .NotEmpty();
    }
}
