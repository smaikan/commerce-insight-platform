using FluentValidation;

namespace ECommerce.Application.Returns.Commands.CompleteReturnRequest;

public sealed class CompleteReturnRequestCommandValidator : AbstractValidator<CompleteReturnRequestCommand>
{
    // Burada iade tamamlama isteğinin geçerli iade talebi kimliği taşıdığını doğruluyorum.
    public CompleteReturnRequestCommandValidator()
    {
        RuleFor(command => command.ReturnRequestId).NotEmpty();
    }
}
