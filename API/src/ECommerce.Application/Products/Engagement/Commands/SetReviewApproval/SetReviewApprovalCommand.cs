using MediatR;

namespace ECommerce.Application.Products.Engagement.Commands.SetReviewApproval;

public sealed record SetReviewApprovalCommand(Guid ReviewId, bool IsApproved) : IRequest;
