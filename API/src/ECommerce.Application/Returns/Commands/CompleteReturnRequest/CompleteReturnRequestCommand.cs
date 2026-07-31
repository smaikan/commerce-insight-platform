using ECommerce.Application.Returns.Dtos;
using MediatR;

namespace ECommerce.Application.Returns.Commands.CompleteReturnRequest;

// Burada teslim alınmış iade veya değişim talebinin mali ya da lojistik kapanış isteğini taşıyorum.
public sealed record CompleteReturnRequestCommand(Guid ReturnRequestId) : IRequest<ReturnRequestDto>;
