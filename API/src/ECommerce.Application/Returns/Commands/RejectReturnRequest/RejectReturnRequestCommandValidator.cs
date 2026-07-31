using ECommerce.Domain.Entities;
using FluentValidation;

namespace ECommerce.Application.Returns.Commands.RejectReturnRequest;

public sealed class RejectReturnRequestCommandValidator : AbstractValidator<RejectReturnRequestCommand>
{
    // Burada iade ret isteğinin kimlik ve isteğe bağlı karar notu sınırlarını doğruluyorum.
    public RejectReturnRequestCommandValidator()
    {
        RuleFor(command => command.ReturnRequestId).NotEmpty();
        RuleFor(command => command.DecisionNote)
            .MaximumLength(ReturnRequest.MaximumDecisionNoteLength)
            .When(command => !string.IsNullOrWhiteSpace(command.DecisionNote));
    }
}
