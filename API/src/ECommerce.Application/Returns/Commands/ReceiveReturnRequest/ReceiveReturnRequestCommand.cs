using ECommerce.Application.Returns.Dtos;
using MediatR;

namespace ECommerce.Application.Returns.Commands.ReceiveReturnRequest;

// Burada yöneticinin Requested iade veya değişim talebinin fiziksel ürünlerini karar öncesinde teslim alma isteğini taşıyorum.
public sealed record ReceiveReturnRequestCommand(Guid ReturnRequestId) : IRequest<ReturnRequestDto>;
