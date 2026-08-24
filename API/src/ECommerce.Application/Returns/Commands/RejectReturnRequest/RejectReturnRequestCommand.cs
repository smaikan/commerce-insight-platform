using ECommerce.Application.Returns.Dtos;
using MediatR;

namespace ECommerce.Application.Returns.Commands.RejectReturnRequest;

// Burada yöneticinin teslim alınmış ve karar bekleyen iade veya değişim talebini reddetme isteğini taşıyorum.
public sealed record RejectReturnRequestCommand(Guid ReturnRequestId, string? DecisionNote = null) : IRequest<ReturnRequestDto>;
