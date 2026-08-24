using ECommerce.Application.Returns.Dtos;
using MediatR;

namespace ECommerce.Application.Returns.Commands.CompleteReturnRequest;

// Burada eski yaşam döngüsünden kalan teslim alınmış kaydın uyumlu completion isteğini taşıyorum.
public sealed record CompleteReturnRequestCommand(Guid ReturnRequestId) : IRequest<ReturnRequestDto>;
