using ECommerce.Application.Returns.Dtos;
using MediatR;

namespace ECommerce.Application.Returns.Commands.ApproveReturnRequest;

// Burada yöneticinin bekleyen iade veya değişim talebini onaylama isteğini taşıyorum.
public sealed record ApproveReturnRequestCommand(Guid ReturnRequestId, string? DecisionNote = null) : IRequest<ReturnRequestDto>;
