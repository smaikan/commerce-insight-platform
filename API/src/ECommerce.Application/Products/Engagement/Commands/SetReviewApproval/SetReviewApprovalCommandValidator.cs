using FluentValidation;

namespace ECommerce.Application.Products.Engagement.Commands.SetReviewApproval;

public sealed class SetReviewApprovalCommandValidator : AbstractValidator<SetReviewApprovalCommand>
{
    public SetReviewApprovalCommandValidator() => RuleFor(command => command.ReviewId).NotEmpty();
}
