using ECommerce.Domain.Entities;
using FluentValidation;

namespace ECommerce.Application.Returns.Commands.ApproveReturnRequest;

public sealed class ApproveReturnRequestCommandValidator : AbstractValidator<ApproveReturnRequestCommand>
{
    // Burada iade onayı isteğinin kimlik ve isteğe bağlı karar notu sınırlarını doğruluyorum.
    public ApproveReturnRequestCommandValidator()
    {
        RuleFor(command => command.ReturnRequestId).NotEmpty();
        RuleFor(command => command.DecisionNote)
            .MaximumLength(ReturnRequest.MaximumDecisionNoteLength)
            .When(command => !string.IsNullOrWhiteSpace(command.DecisionNote));
    }
}
